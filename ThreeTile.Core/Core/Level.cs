using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using ThreeTile.Core.Core.LayerShadows;
using ThreeTile.Core.Core.Moves;
using ThreeTile.Core.Core.Zones;
using ThreeTile.Core.ExtensionTools;

namespace ThreeTile.Core.Core;

public partial class Level
{
    #region Zones

    /// <summary> 盘面：牧场 </summary>
    public Pasture Pasture { get; private set; }

    /// <summary> 集结区 / 卡槽 </summary>
    public StagingArea StagingArea { get; private set;}

    /// <summary> 已完成 Tile 的围栏 </summary>
    public Corral Corral { get; private set;}

    #endregion

    #region History

    private readonly List<Move> _historyMoves = new();
    public IReadOnlyList<Move> HistoryMoves => _historyMoves;

    #endregion
    
    # region MoveGroup

    public readonly List<BehaviourMove> LogicBehaviours= new ();
    
    #endregion
    
    # region 常量

    /// 当前解析逻辑下理论最大花色
    public const int MaxColorIndex = 61;
    /// 当前出题逻辑下可用的最大花色数量，目前是 a
    public const int MaxLevelColorIndex = 36;
    /// 正常情况下每个花色的最大数量，目前是 4 (Classic 设计也是 4)
    public const int NormalMaxColorCount = 4;
    
    # endregion

    public Level
    (
        ReadOnlySpan<int> positions,
        ReadOnlySpan<int> colors,
        int slotCapacity,
        int requiredMatchingElementsCount,
        ShadowPropagationEnum mode
    )
    {
        // ─────────────────────────
        // 0) 参数校验
        // ─────────────────────────
        if (positions.Length == 0)
            throw new ArgumentException("positions 不能为空");

        if (positions.Length != colors.Length)
            throw new ArgumentException(
                $"positions.Length({positions.Length}) != colors.Length({colors.Length})");

        if (slotCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(slotCapacity));

        if (requiredMatchingElementsCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredMatchingElementsCount));

        // ─────────────────────────
        // 1) 扫描尺寸 & 构建 Tile
        // ─────────────────────────
        int maxX = 0, maxY = 0, maxZ = 0;
        var tiles = new List<Tile>(positions.Length);

        for (int index = 0; index < positions.Length; index++)
        {
            var (x, y, z) = positions[index].UnpackXyz();

            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            maxZ = Math.Max(maxZ, z);

            var tile = new Tile(index, positions[index],colors[index]);
            tile.SetTileZone(Tile.TileZoneEnum.Pasture);
            tiles.Add(tile);
        }

        // ⚠️ 坐标是 0-based → 尺寸必须 +1, 同时麻将这里是两个单位为一组，所以最大还要 + 1
        const int tileUnitWidth = 2;
        int cols = maxX + tileUnitWidth;
        int rows = maxY + tileUnitWidth;
        int layers = maxZ + 1;

        // ─────────────────────────
        // 2) 创建牧场并放置 Tile
        // ─────────────────────────
        Pasture = new Pasture(rows, cols, layers, mode, this);

        foreach (var tile in tiles)
        {
            Pasture.AddTile(tile);
        }

        // ─────────────────────────
        // 3) 其他区域
        // ─────────────────────────
        StagingArea = new StagingArea(
            slotCapacity,
            requiredMatchingElementsCount,
            this
        );

        Corral = new Corral(this);
    }


    public void GetLogicBehaviours(int? availableCapacity = null)
    {
        LogicBehaviours.Clear();
        availableCapacity ??= StagingArea.AvailableCapacity;
        if (availableCapacity <= 0)
        {
            return;
        }

        var matchRequireCount = StagingArea.RequiredMatchingElementsCount;
        var slotColorCounter = StagingArea.Counter;
        var unlockTileColorMap = new Dictionary<int, List<Tile>>();
        var unlockTileUsedTile = new HashSet<Tile>();
        foreach (var unlockingTile in Pasture.UnlockingTiles)
        {
            if (!unlockTileColorMap.TryGetValue(unlockingTile.Color, out var group))
            {
                group = new List<Tile>();
                unlockTileColorMap[unlockingTile.Color] = group;
            }
            group.Add(unlockingTile);
        }
        
        // 简单消除行为1 slot + unlockTile >= matchRequireCount
        foreach (var (color, count) in slotColorCounter)
        {
            // 消除这个颜色需要数量大于当前格子的可用容量，直接排除
            if (matchRequireCount - count > availableCapacity)
                continue;
            
            if (!unlockTileColorMap.TryGetValue(color, out var group))
                continue;
            
            if (group.Count + count < matchRequireCount)
                continue;
            
            // 添加到使用的中
            foreach (var tile in group) unlockTileUsedTile.Add(tile);
            
            // C(group.Count, matchRequireCount - count)
            foreach (var selectIndexes in Choose(Enumerable.Range(0, group.Count).ToArray(), matchRequireCount - count))
            {
                LogicBehaviours.Add(new BehaviourMove(
                    BehaviourKind.EASY_CLEAR,
                    color,
                    selectIndexes.Select(index => group[index].Index).ToArray()
                    ));
            }
        }

        // 简单行为消除2 unlockTile >= 3  直接选择三个可消除
        if (matchRequireCount <= availableCapacity) // 卡槽剩余容量支持全新的消除组
        {
            foreach (var (color, group) in unlockTileColorMap)
            {
                if (slotColorCounter.ContainsKey(color))
                    continue;

                if (group.Count < matchRequireCount)
                    continue;

                // 添加到使用的中
                foreach (var tile in group) unlockTileUsedTile.Add(tile);
                // C(group.Count, matchRequireCount) 个移动组
                foreach (var selectIndexes in Choose(Enumerable.Range(0, group.Count).ToArray(), matchRequireCount))
                {
                    LogicBehaviours.Add(new BehaviourMove(
                        BehaviourKind.EASY_CLEAR,
                        color,
                        selectIndexes.Select(index => group[index].Index).ToArray()
                    ));
                }
            }
        }
        
        // 困难消除行为
        // 1. 容量方面 可选 + 可见 至少需要2格
        // 2. 颜色方面 卡槽内的颜色距离消除还差至少两个
        if (availableCapacity >= 2)
        {
            // 对每一个棋子进行展开, 过滤哪些展开后没有相同花色的棋子
            // 获取展开后的可选棋子

            var allowUnlockingTile = Pasture.UnlockingTiles.Where(tile =>
            {
                if (slotColorCounter.TryGetValue(tile.Color, out var count))
                {
                    return matchRequireCount - count >= 2;
                }

                return true;
            }).ToList();
            
            var paddingBehaviours = new List<BehaviourMove>();
            foreach (var unlockingTile in allowUnlockingTile)
            {
                // 展开这个棋子后的棋子结合
                HashSet<Tile> expanders = new();
                Pasture.Expand(unlockingTile, ref expanders);
                if (expanders.Count <= 0) continue;
                
                // 1. 尝试获取展开的棋子中有没有同色的棋子
                var sameColorTile = expanders.Where(tile => tile.Color == unlockingTile.Color).ToHashSet();
                if (sameColorTile.Count <= 0) continue;
                
                // 2. 这些同色棋子只有一个锁定者
                var targetTile = sameColorTile.Where(tile =>
                {
                    var lockers = new HashSet<Tile>();
                    Pasture.LockersOf(tile, ref lockers);
                    return lockers.Count == 1 && lockers.Contains(unlockingTile);
                }).ToList();
                if (targetTile.Count <= 0) continue;
                
                // 选择方案 卡槽内有的color 
                if (slotColorCounter.TryGetValue(unlockingTile.Color, out var count))
                {
                    // 展开的同色可选棋子数量 + 卡槽内的数量 + 自己
                    var totalCount = targetTile.Count + count + 1;
                    if (totalCount < matchRequireCount) // 这个情况几乎不会出现
                    {
                        targetTile.Insert(0, unlockingTile);
                        paddingBehaviours.Add(new BehaviourMove(
                            BehaviourKind.HARD_CLEAR,
                            unlockingTile.Color,
                            targetTile.Select(tile => tile.Index).ToArray()
                            ));
                    }
                    else if (totalCount == matchRequireCount)
                    {
                        targetTile.Insert(0, unlockingTile);
                        LogicBehaviours.Add(new BehaviourMove(
                            BehaviourKind.HARD_CLEAR,
                            unlockingTile.Color,
                            targetTile.Select(tile => tile.Index).ToArray()
                        ));
                    }
                    else
                    {
                        var chooseCount = matchRequireCount - count - 1;
                        if (chooseCount <= 0)
                        {
                            throw new ArgumentException($"choose 计算有误");
                        }
                        
                        foreach (int[] ints in Choose(Enumerable.Range(0, targetTile.Count).ToList(), chooseCount))
                        {
                            var tileIndexes = new List<int>(chooseCount + 1)
                            {
                                unlockingTile.Index
                            };
                            
                            foreach (var i in ints)
                            {
                                tileIndexes.Add(targetTile[i].Index);
                            }

                            LogicBehaviours.Add(
                                new BehaviourMove(
                                    BehaviourKind.HARD_CLEAR,
                                    unlockingTile.Color,
                                    tileIndexes
                                )
                            );
                        }
                    }
                        
                }
                else
                {
                    // 卡槽内没有的颜色 自己 + 自己展开的 + 其他的同色棋子
                    // 几种情况
                    /*
                     * 1.自己 + 展开
                     * 2.自己 + 展开 + 其他同色可选棋子 自己一个，展开至少一个， 其他同色棋子至少一个
                     */
                    
                    // 可以组成匹配的部分。展开 + 自己就可以组成匹配
                    if (targetTile.Count + 1 >= matchRequireCount)
                    {
                        // 自己 + 展开
                        var chooseCount = matchRequireCount - 1;
                        foreach (int[] ints in Choose(Enumerable.Range(0, targetTile.Count).ToList(), chooseCount))
                        {
                            var tileIndexes = new List<int>(chooseCount + 1)
                            {
                                unlockingTile.Index
                            };
                            
                            foreach (var i in ints)
                            {
                                tileIndexes.Add(targetTile[i].Index);
                            }

                            LogicBehaviours.Add(
                                new BehaviourMove(
                                    BehaviourKind.HARD_CLEAR,
                                    unlockingTile.Color,
                                    tileIndexes
                                )
                            );
                        }

                        // 自己 + 展开 + 其他同色可选
                        if (unlockTileColorMap.TryGetValue(unlockingTile.Color, out var group))
                        {
                            // 可选棋子（排除自身的数量）+ 展开的同色可选棋子数量 + 自己 满足匹配消除数量
                            if (group.Count - 1 + targetTile.Count + 1 >= matchRequireCount)
                            {
                                // 自己 + 展开 + 其他同色可选
                                foreach (var tile2 in group)
                                {
                                    if (tile2.Index == unlockingTile.Index) continue;
                                    foreach (var tile3 in targetTile)
                                    {
                                        LogicBehaviours.Add(
                                            new BehaviourMove(
                                                BehaviourKind.HARD_CLEAR,
                                                unlockingTile.Color,
                                                [unlockingTile.Index, tile2.Index, tile3.Index]
                                            )
                                        );
                                    }
                                }
                                
                            }
                        }
                    }
                    else
                    {
                        // 自己 + 展开 不够匹配， 需要配合其同色可选  自己 + 展开全部 + 同色可选补充
                        if (unlockTileColorMap.TryGetValue(unlockingTile.Color, out var group)
                            // 可选棋子（排除自身的数量）+ 展开的同色可选棋子数量 + 自己 满足匹配消除数量
                            && (group.Count - 1 + targetTile.Count + 1 >= matchRequireCount)
                            )
                        {
                            var chooseCount = matchRequireCount - 1 - targetTile.Count;
                            var sameColorGroup = group.Where(tile => tile.Index != unlockingTile.Index).ToArray();
                            // 自己 + 展开 + 其他同色可选
                            foreach (int[] ints in Choose(Enumerable.Range(0, sameColorGroup.Length).ToList(), chooseCount))
                            {
                                // 自己
                                var tileIndexes = new List<int>(chooseCount + 1)
                                {
                                    unlockingTile.Index
                                };
                                // 同色可选部分
                                foreach (var i in ints)
                                {
                                    tileIndexes.Add(sameColorGroup[i].Index);
                                }
                                // 全部的展开
                                foreach (var tile in targetTile)
                                {
                                    tileIndexes.Add(tile.Index);
                                }

                                LogicBehaviours.Add(
                                    new BehaviourMove(
                                        BehaviourKind.HARD_CLEAR,
                                        unlockingTile.Color,
                                        tileIndexes
                                    )
                                );
                            }
                        }
                        else  // 自己 + 展开 同时同色可选也没法补充，待定
                        {
                            var tileIndexes = new List<int>(targetTile.Count + 1)
                            {
                                unlockingTile.Index
                            };
                            foreach (var tile in targetTile)
                            {
                                tileIndexes.Add(tile.Index);
                            }
                            paddingBehaviours.Add(
                                new BehaviourMove(
                                    BehaviourKind.HARD_CLEAR,
                                    unlockingTile.Color,
                                    tileIndexes
                                )
                            );
                        }
                    }
                }

                // 如果有待定的部分， 继续展开
                if (paddingBehaviours.Count > 0)
                {
                    foreach (var paddingBehaviour in paddingBehaviours)
                    {
                        Console.WriteLine(paddingBehaviour.ToRenderString(this));
                    }
                }
            }
        }
        
        // 剩余没有被使用的是可选其一即为翻牌的棋子
        var remainUnlockTiles = Pasture.UnlockingTiles.Where(tile => !unlockTileUsedTile.Contains(tile)).ToArray();
        if (remainUnlockTiles.Length > 0)
        {
            // 构造翻牌行为
            foreach (var remainUnlockTile in remainUnlockTiles)
            {
                LogicBehaviours.Add(new BehaviourMove(BehaviourKind.FLIP, remainUnlockTile.Color, [remainUnlockTile.Index]));
            }
        }
    }

    
    /// <summary>
    /// 从 source 中生成所有大小为 k 的组合（Combination）。
    /// 
    /// 特点：
    /// 1. 不重复
    /// 2. 不关心顺序（不是排列）
    /// 3. 使用非递归的“索引状态机”
    /// 
    /// 示例：
    /// source = [a, b, c, d], k = 2
    /// 结果：
    /// [a,b], [a,c], [a,d], [b,c], [b,d], [c,d]
    /// </summary>
    static IEnumerable<int[]> Choose(
        IReadOnlyList<int> source,
        int k)
    {
        int n = source.Count;

        // ─────────────────────────────────────────────
        // 当前组合的“索引表示”
        // indices 始终保持严格递增：
        //   indices = [i0, i1, ..., ik-1]
        // 表示选择：
        //   source[i0], source[i1], ..., source[ik-1]
        //
        // 初始状态是字典序中的第一个组合：
        //   [0, 1, 2, ..., k-1]
        // ─────────────────────────────────────────────
        var indices = new int[k];
        for (int i = 0; i < k; i++)
            indices[i] = i;

        while (true)
        {
            // ─────────────────────────────────────────
            // 1️⃣ 根据当前 indices 生成一个组合结果
            // ─────────────────────────────────────────
            var result = new int[k];
            for (int i = 0; i < k; i++)
                result[i] = source[indices[i]];

            // 把当前组合交给调用方
            yield return result;

            // ─────────────────────────────────────────
            // 2️⃣ 推进到“下一个组合”（字典序）
            //    从右向左，寻找还能继续“往右移动”的位置
            // ─────────────────────────────────────────
            int t;

            for (t = k - 1; t >= 0; t--)
            {
                // 对于第 t 个位置，它能取到的最大值是：
                //   n - k + t
                //
                // 原因：
                //   右边还剩 (k - 1 - t) 个位置，
                //   必须为它们预留空间，保证严格递增
                if (indices[t] < n - k + t)
                    break; // 找到还能动的位置
            }

            // 如果所有位置都已经到达最大值
            // 说明已经是最后一个组合，如 [n-k, ..., n-1]
            if (t < 0)
                yield break;

            // ─────────────────────────────────────────
            // 3️⃣ 将该位置向右移动一格
            // ─────────────────────────────────────────
            indices[t]++;

            // ─────────────────────────────────────────
            // 4️⃣ 将右侧所有位置重置为“最小递增状态”
            //    保证：
            //      indices[t+1] = indices[t] + 1
            //      indices[t+2] = indices[t+1] + 1
            //      ...
            // ─────────────────────────────────────────
            for (int i = t + 1; i < k; i++)
                indices[i] = indices[i - 1] + 1;
        }
    }

    public void DoMove(Move move)
    {
        if (!move.CanDo(this))
        {
            throw new InvalidOperationException("操作不合法");
        }
        
        move.Do(this);
        _historyMoves.Add(move);
    }

    public void UndoMove()
    {
        if (_historyMoves.Count <= 0) throw new ArgumentOutOfRangeException(nameof(_historyMoves), "No history");
        var move = _historyMoves[^1];
        move.Undo(this);
        _historyMoves.RemoveAt(_historyMoves.Count - 1);
    }

    private Level()
    {
        // 只用于 Clone
    }
    public Level Clone()
    {
        var clone = new Level();

        // 深拷贝 Zones
        clone.Pasture = this.Pasture.Clone();
        clone.StagingArea = this.StagingArea.Clone();
        clone.Corral = this.Corral.Clone();

        // 修正 parent
        clone.Pasture.Parent = clone;
        clone.StagingArea.Parent = clone;
        clone.Corral.Parent = clone;
        
        // History
        clone._historyMoves.Clear();
        foreach (var historyMove in this._historyMoves)
        {
            clone._historyMoves.Add(historyMove);
        }

        return clone;
    }
    
    internal void ClearHistory()
    {
        _historyMoves.Clear();
    }

    public override string ToString()
    {
        return
            $"Level {{ " +
            $"Pasture: {Pasture}, " +
            $"Staging: {FormatStaging()}, " +
            $"Corral: {FormatCorral()}, " +
            $"History: {_historyMoves.Count}" +
            $" }}";
    }
    
    private string FormatStaging()
    {
        if (StagingArea.IsEmpty)
            return "Empty";

        return
            $"[{StagingArea.UsedCapacity}/{StagingArea.Capacity}] " +
            string.Join(
                ", ",
                StagingArea.Counter.Select(kv => $"{kv.Key}×{kv.Value}")
            );
    }
    
    private string FormatCorral()
    {
        if (Corral.TotalCount == 0)
            return "Empty";

        return
            $"{Corral.TotalCount} | " +
            string.Join(
                ", ",
                Corral.ColorCounter.Select(kv => $"{kv.Key}×{kv.Value}")
            );
    }

    
    //   public void GetLogicBehaviours(int? availableCapacity = null)
    // {
    //     LogicBehaviours.Clear();
    //
    //     availableCapacity ??= StagingArea.AvailableCapacity;
    //     if (availableCapacity <= 0) return;
    //
    //     var cap = availableCapacity.Value;
    //     var matchCount = StagingArea.RequiredMatchingElementsCount;
    //
    //     // -----------------------------
    //     // 1) slot 颜色计数（Counter） => 数组化：slotCount[color]
    //     // -----------------------------
    //     var slotCount = new int[MaxColorIndex + 1];
    //     var slotColorBit = 0UL;
    //
    //     foreach (var (color, count) in StagingArea.Counter)
    //     {
    //         if ((uint)color > (uint)MaxColorIndex) continue;
    //         slotCount[color] = count;
    //         slotColorBit |= 1UL << color;
    //     }
    //
    //     // -----------------------------
    //     // 2) unlocking tiles 按颜色分桶：unlockByColor[color] = List<Tile>
    //     // -----------------------------
    //     var unlockByColor = new List<Tile>[MaxColorIndex + 1];
    //     var unlockColorBit = 0UL;
    //
    //     if (Pasture.UnlockingTiles is { Count: > 0 })
    //     {
    //         foreach (var t in Pasture.UnlockingTiles)
    //         {
    //             var c = t.Color;
    //             if ((uint)c > (uint)MaxColorIndex) continue;
    //
    //             unlockByColor[c] ??= new List<Tile>(4);
    //             unlockByColor[c].Add(t);
    //             unlockColorBit |= 1UL << c;
    //         }
    //     }
    //
    //     if (unlockColorBit == 0UL) return;
    //
    //     // 用于调试/去重标记：你原来是 unlockTileUsedTile
    //     // （这里仍保留，但不强依赖它参与逻辑）
    //     var unlockTileUsed = new HashSet<Tile>();
    //
    //     // ==========================================================
    //     // A) 简单消除 1：slot[color] + unlock[color] >= matchCount
    //     //    且补的数量 <= cap
    //     // ==========================================================
    //     // 遍历 “slotColorBit & unlockColorBit” 交集颜色，最省
    //     var bothBit = slotColorBit & unlockColorBit;
    //     var tmp = bothBit;
    //
    //     while (tmp != 0UL)
    //     {
    //         var low = tmp & ~(tmp - 1);
    //         var color = BitOperations.TrailingZeroCount(low);
    //
    //         var countInSlot = slotCount[color];
    //         var group = unlockByColor[color]; // 一定不为 null，因为在 unlockColorBit 里
    //         var groupCount = group!.Count;
    //
    //         // 需要从 unlocking 里选的数量
    //         var need = matchCount - countInSlot;
    //
    //         // 需要补的数量 > 可用容量 => 直接排除
    //         if (need > cap)
    //         {
    //             tmp &= ~low;
    //             continue;
    //         }
    //
    //         // unlocking + slot 仍不足 matchCount
    //         if (groupCount + countInSlot < matchCount)
    //         {
    //             tmp &= ~low;
    //             continue;
    //         }
    //
    //         foreach (var tile in group) unlockTileUsed.Add(tile);
    //
    //         // C(groupCount, need)
    //         // need 可能为 0：代表 slot 里已经够了（一般不会发生或没意义）
    //         if (need > 0)
    //         {
    //             foreach (var picks in ChooseIndex(groupCount, need))
    //             {
    //                 // picks 是 group 下标，转为 tileIndex
    //                 var tileIndexes = new int[need];
    //                 for (int i = 0; i < need; i++)
    //                     tileIndexes[i] = group[picks[i]].Index;
    //
    //                 LogicBehaviours.Add(new BehaviourMove(
    //                     BehaviourKind.EASY_CLEAR,
    //                     color,
    //                     tileIndexes
    //                 ));
    //             }
    //         }
    //
    //         tmp &= ~low;
    //     }
    //
    //     // ==========================================================
    //     // B) 简单消除 2：slot 没这个色，且 cap 支持 “新组”
    //     //    unlock[color] >= matchCount
    //     // ==========================================================
    //     if (matchCount <= cap)
    //     {
    //         // 遍历 unlockColorBit 中 “不在 slotColorBit 的颜色”
    //         var onlyUnlockBit = unlockColorBit & ~slotColorBit;
    //         tmp = onlyUnlockBit;
    //
    //         while (tmp != 0UL)
    //         {
    //             var low = tmp & ~(tmp - 1);
    //             var color = BitOperations.TrailingZeroCount(low);
    //
    //             var group = unlockByColor[color]!;
    //             if (group.Count >= matchCount)
    //             {
    //                 foreach (var tile in group) unlockTileUsed.Add(tile);
    //
    //                 foreach (var picks in ChooseIndex(group.Count, matchCount))
    //                 {
    //                     var tileIndexes = new int[matchCount];
    //                     for (int i = 0; i < matchCount; i++)
    //                         tileIndexes[i] = group[picks[i]].Index;
    //
    //                     LogicBehaviours.Add(new BehaviourMove(
    //                         BehaviourKind.EASY_CLEAR,
    //                         color,
    //                         tileIndexes
    //                     ));
    //                 }
    //             }
    //
    //             tmp &= ~low;
    //         }
    //     }
    //
    //     // ==========================================================
    //     // C) 困难消除：可选 + 可见（展开得到的同色“可见牌”）
    //     //    规则（贴你原始实现）：
    //     //    - cap 至少 2
    //     //    - 若 slot[color] 存在，则必须还差 >=2 才值得做（matchCount - count >=2）
    //     //    - 展开后必须能得到同色牌
    //     //    - 目标同色可见牌：其 lockers 只有 1 且就是 unlockingTile
    //     // ==========================================================
    //     if (cap >= 2 && Pasture.UnlockingTiles is { Count: > 0 })
    //     {
    //         var paddingBehaviours = new List<BehaviourMove>(8);
    //
    //         foreach (var unlockingTile in Pasture.UnlockingTiles)
    //         {
    //             var color = unlockingTile.Color;
    //             if ((uint)color > (uint)MaxColorIndex) continue;
    //
    //             // 过滤：slot 内有该色且距离消除只差 0/1 的，跳过（你原逻辑：必须至少差2）
    //             var countInSlot = slotCount[color];
    //             if (countInSlot > 0 && matchCount - countInSlot < 2)
    //                 continue;
    //
    //             // Expand
    //             HashSet<Tile> expanders = new();
    //             Pasture.Expand(unlockingTile, ref expanders);
    //             if (expanders.Count == 0) continue;
    //
    //             // sameColor in expanders
    //             // 再过滤：目标同色牌必须仅被 unlockingTile 锁住
    //             var targetTiles = new List<Tile>(4);
    //             foreach (var t in expanders)
    //             {
    //                 if (t.Color != color) continue;
    //
    //                 var lockers = new HashSet<Tile>();
    //                 Pasture.LockersOf(t, ref lockers);
    //                 if (lockers.Count == 1 && lockers.Contains(unlockingTile))
    //                     targetTiles.Add(t);
    //             }
    //
    //             if (targetTiles.Count == 0) continue;
    //
    //             // --------
    //             // 拼组逻辑：分 slot 内是否已有该色
    //             // --------
    //             if (countInSlot > 0)
    //             {
    //                 // totalCount = targetTiles + slot + self
    //                 var total = targetTiles.Count + countInSlot + 1;
    //
    //                 if (total < matchCount)
    //                 {
    //                     // 不足：进 padding（你原实现）
    //                     var idx = new int[targetTiles.Count + 1];
    //                     idx[0] = unlockingTile.Index;
    //                     for (int i = 0; i < targetTiles.Count; i++)
    //                         idx[i + 1] = targetTiles[i].Index;
    //
    //                     paddingBehaviours.Add(new BehaviourMove(
    //                         BehaviourKind.HARD_CLEAR,
    //                         color,
    //                         idx
    //                     ));
    //                 }
    //                 else if (total == matchCount)
    //                 {
    //                     // 刚好：self + 全部 targetTiles
    //                     var idx = new int[targetTiles.Count + 1];
    //                     idx[0] = unlockingTile.Index;
    //                     for (int i = 0; i < targetTiles.Count; i++)
    //                         idx[i + 1] = targetTiles[i].Index;
    //
    //                     LogicBehaviours.Add(new BehaviourMove(
    //                         BehaviourKind.HARD_CLEAR,
    //                         color,
    //                         idx
    //                     ));
    //                 }
    //                 else
    //                 {
    //                     // 超出：从 targetTiles 里选 chooseCount = matchCount - slot - self
    //                     var chooseCount = matchCount - countInSlot - 1;
    //                     if (chooseCount <= 0)
    //                         throw new ArgumentException("chooseCount 计算有误");
    //
    //                     foreach (var picks in ChooseIndex(targetTiles.Count, chooseCount))
    //                     {
    //                         var idx = new int[chooseCount + 1];
    //                         idx[0] = unlockingTile.Index;
    //                         for (int i = 0; i < chooseCount; i++)
    //                             idx[i + 1] = targetTiles[picks[i]].Index;
    //
    //                         LogicBehaviours.Add(new BehaviourMove(
    //                             BehaviourKind.HARD_CLEAR,
    //                             color,
    //                             idx
    //                         ));
    //                     }
    //                 }
    //             }
    //             else
    //             {
    //                 // slot 没该色：self + targetTiles 至少要能凑 matchCount
    //                 if (targetTiles.Count + 1 >= matchCount)
    //                 {
    //                     // self + 从 targetTiles 中选 matchCount-1
    //                     var chooseCount = matchCount - 1;
    //                     foreach (var picks in ChooseIndex(targetTiles.Count, chooseCount))
    //                     {
    //                         var idx = new int[chooseCount + 1];
    //                         idx[0] = unlockingTile.Index;
    //                         for (int i = 0; i < chooseCount; i++)
    //                             idx[i + 1] = targetTiles[picks[i]].Index;
    //
    //                         LogicBehaviours.Add(new BehaviourMove(
    //                             BehaviourKind.HARD_CLEAR,
    //                             color,
    //                             idx
    //                         ));
    //                     }
    //
    //                     // self + targetTiles + 其他同色可选（你原来的“额外组”分支）
    //                     // 这里严格照你原来的：只在 unlockByColor[color] 可用时补一张可选 + 一张 target
    //                     var group = unlockByColor[color];
    //                     if (group != null && group.Count >= 2)
    //                     {
    //                         foreach (var t2 in group)
    //                         {
    //                             if (t2.Index == unlockingTile.Index) continue;
    //                             foreach (var t3 in targetTiles)
    //                             {
    //                                 LogicBehaviours.Add(new BehaviourMove(
    //                                     BehaviourKind.HARD_CLEAR,
    //                                     color,
    //                                     new[] { unlockingTile.Index, t2.Index, t3.Index }
    //                                 ));
    //                             }
    //                         }
    //                     }
    //                 }
    //                 else
    //                 {
    //                     // self + targetTiles 不够：尝试用“同色可选”补足
    //                     var group = unlockByColor[color];
    //                     if (group != null)
    //                     {
    //                         // 排除自身
    //                         var sameColorSelectable = new List<Tile>(group.Count);
    //                         foreach (var t in group)
    //                             if (t.Index != unlockingTile.Index)
    //                                 sameColorSelectable.Add(t);
    //
    //                         if (sameColorSelectable.Count + targetTiles.Count + 1 >= matchCount)
    //                         {
    //                             var needSelectable = matchCount - 1 - targetTiles.Count;
    //
    //                             foreach (var picks in ChooseIndex(sameColorSelectable.Count, needSelectable))
    //                             {
    //                                 var idx = new int[1 + needSelectable + targetTiles.Count];
    //
    //                                 var p = 0;
    //                                 idx[p++] = unlockingTile.Index;
    //
    //                                 for (int i = 0; i < needSelectable; i++)
    //                                     idx[p++] = sameColorSelectable[picks[i]].Index;
    //
    //                                 for (int i = 0; i < targetTiles.Count; i++)
    //                                     idx[p++] = targetTiles[i].Index;
    //
    //                                 LogicBehaviours.Add(new BehaviourMove(
    //                                     BehaviourKind.HARD_CLEAR,
    //                                     color,
    //                                     idx
    //                                 ));
    //                             }
    //                         }
    //                         else
    //                         {
    //                             // 同色可选也补不齐：padding
    //                             var idx = new int[targetTiles.Count + 1];
    //                             idx[0] = unlockingTile.Index;
    //                             for (int i = 0; i < targetTiles.Count; i++)
    //                                 idx[i + 1] = targetTiles[i].Index;
    //
    //                             paddingBehaviours.Add(new BehaviourMove(
    //                                 BehaviourKind.HARD_CLEAR,
    //                                 color,
    //                                 idx
    //                             ));
    //                         }
    //                     }
    //                     else
    //                     {
    //                         // 没有同色可选分桶：padding
    //                         var idx = new int[targetTiles.Count + 1];
    //                         idx[0] = unlockingTile.Index;
    //                         for (int i = 0; i < targetTiles.Count; i++)
    //                             idx[i + 1] = targetTiles[i].Index;
    //
    //                         paddingBehaviours.Add(new BehaviourMove(
    //                             BehaviourKind.HARD_CLEAR,
    //                             color,
    //                             idx
    //                         ));
    //                     }
    //                 }
    //             }
    //         }
    //
    //         // 你原来是 Console.WriteLine 打印 padding（待定）
    //         // 这里保留同样动线
    //         if (paddingBehaviours.Count > 0)
    //         {
    //             foreach (var b in paddingBehaviours)
    //                 Console.WriteLine(b.ToRenderString(this));
    //         }
    //     }
    // }
    
//     public void GetLogicBehaviours(int? availableCapacity = null)
// {
//     LogicBehaviours.Clear();
//
//     availableCapacity ??= StagingArea.AvailableCapacity;
//     if (availableCapacity <= 0) return;
//
//     int cap = availableCapacity.Value;
//     int matchCount = StagingArea.RequiredMatchingElementsCount;
//
//     // slot[color]
//     var slotCount = new int[MaxColorIndex + 1];
//     foreach (var (color, count) in StagingArea.Counter)
//         slotCount[color] = count;
//
//     // selectable tiles by color
//     var selectableByColor = new List<Tile>[MaxColorIndex + 1];
//     foreach (var t in Pasture.UnlockingTiles)
//     {
//         selectableByColor[t.Color] ??= new List<Tile>();
//         selectableByColor[t.Color].Add(t);
//     }
//
//     // =========================================================
//     // per-color processing
//     // =========================================================
//     for (int color = 0; color <= MaxColorIndex; color++)
//     {
//         var S = selectableByColor[color];
//         if (S == null || S.Count == 0)
//             continue;
//
//         int clearNeedCount = matchCount - slotCount[color];
//
//         // ---- 核心剪枝 ----
//         if (clearNeedCount <= 0)
//             continue;
//
//         if (clearNeedCount > cap)
//             continue;
//
//         // =====================================================
//         // 1. 纯可选消除（EASY）
//         // =====================================================
//         if (S.Count >= clearNeedCount)
//         {
//             foreach (var pick in ChooseIndex(S.Count, clearNeedCount))
//             {
//                 var tiles = new int[clearNeedCount];
//                 for (int i = 0; i < clearNeedCount; i++)
//                     tiles[i] = S[pick[i]].Index;
//
//                 LogicBehaviours.Add(
//                     new BehaviourMove(
//                         BehaviourKind.EASY_CLEAR,
//                         color,
//                         tiles
//                     )
//                 );
//             }
//         }
//
//         // =====================================================
//         // 2. 展开型消除（HARD）
//         // =====================================================
//         foreach (var self in S)
//         {
//             // self 已占 1
//             int restNeed = clearNeedCount - 1;
//             if (restNeed <= 0)
//                 continue;
//
//             // E(self)：展开后的安全同色 tile
//             var expandSet = GetSafeExpandedSameColor(self);
//             if (expandSet.Count == 0)
//                 continue;
//
//             int maxI = Math.Min(expandSet.Count, restNeed);
//
//             // i from expand, j from selectable (excluding self)
//             for (int i = 1; i <= maxI; i++)
//             {
//                 int j = restNeed - i;
//                 if (j > S.Count - 1)
//                     continue;
//
//                 foreach (var ei in ChooseIndex(expandSet.Count, i))
//                 foreach (var sj in ChooseIndex(S.Count - 1, j))
//                 {
//                     var tiles = new List<int>(1 + i + j)
//                     {
//                         self.Index
//                     };
//
//                     // expand part
//                     foreach (var k in ei)
//                         tiles.Add(expandSet[k].Index);
//
//                     // selectable part (skip self)
//                     int sIdx = 0;
//                     for (int p = 0; p < S.Count; p++)
//                     {
//                         if (S[p] == self) continue;
//                         if (Array.IndexOf(sj, sIdx) >= 0)
//                             tiles.Add(S[p].Index);
//                         sIdx++;
//                     }
//
//                     LogicBehaviours.Add(
//                         new BehaviourMove(
//                             BehaviourKind.HARD_CLEAR,
//                             color,
//                             tiles
//                         )
//                     );
//                 }
//             }
//         }
//     }
// }

    public void GetLogicBehaviours2(int? availableCapacity = null)
    {
        LogicBehaviours.Clear();

        availableCapacity ??= StagingArea.AvailableCapacity;
        if (availableCapacity <= 0) return;

        int cap = availableCapacity.Value;
        int matchCount = StagingArea.RequiredMatchingElementsCount;

        // slot[color]
        var slotCount = new int[MaxColorIndex + 1];
        foreach (var (color, count) in StagingArea.Counter)
            slotCount[color] = count;

        // selectable tiles by color
        var selectableByColor = new List<Tile>[MaxColorIndex + 1];
        foreach (var t in Pasture.UnlockingTiles)
        {
            selectableByColor[t.Color] ??= new List<Tile>();
            selectableByColor[t.Color].Add(t);
        }

        // =========================================================
        // per-color processing
        // =========================================================
        for (int color = 0; color <= MaxColorIndex; color++)
        {
            var S = selectableByColor[color];
            if (S == null || S.Count == 0)
                continue;

            int clearNeedCount = matchCount - slotCount[color];

            // ---- 核心剪枝 ----
            if (clearNeedCount <= 0)
                continue;

            if (clearNeedCount > cap)
                continue;
            
            // 简单消除
            int targetColor = color;
            EmitSimpleClears(~(1UL << S.Count), clearNeedCount, union =>
            {   
                var indexes = ResolveTileIndexes(S.ToArray(), union);
                LogicBehaviours.Add(new BehaviourMove(
                    BehaviourKind.EASY_CLEAR,
                    targetColor,
                    indexes
                    ));
            });
            
            if (clearNeedCount < 2) continue;  // 困难消除至少两个起步，可选 + 可见
            
            // 困难消除 构建新的 FSE
            // 困难消除：构建新的 F / S / E
            foreach (var (newF, newS, newE) in BuildNewFSE(
                         F: [],
                         S: [],
                         E: S.ToArray(),
                         expand: (Tile paddingExpandTile) =>
                         {
                             // 业务规则：
                             // 展开 paddingExpandTile 后，
                             // 返回：同色、且仅被 paddingExpandTile 锁定的棋子
                             return GetExpandedSameColorSelectableTiles(
                                 paddingExpandTile,
                                 (lockedTile, upstreams) => // 准确的是看 upstreams 是否为： F + paddingExpandTile 
                                     upstreams.Count == 1 &&
                                     upstreams.Contains(paddingExpandTile),
                                 Pasture
                             ).ToArray();
                         }))
            {
                // newF : 固定组（已推进的）
                // newS : 可选组（历史可选 + 其他展开成员）
                // newE : 新展开组（由 paddingExpandTile 展开得到）
                EmitHardClears(
                    ~(1UL << newF.Length), 
                    ~(1UL << newS.Length), 
                    ~(1UL << newE.Length),
                    clearNeedCount,
                    (pick =>
                    {
                        var fixedIndexes = ResolveTileIndexes(newF.ToArray(), pick.FixedMask);
                        var selectableIndexes = ResolveTileIndexes(newS.ToArray(), pick.FixedMask);
                        var expandableIndexes = ResolveTileIndexes(newE.ToArray(), pick.FixedMask);
                        LogicBehaviours.Add(new BehaviourMove(
                            BehaviourKind.HARD_CLEAR,
                            targetColor,
                            [..fixedIndexes, ..selectableIndexes, ..expandableIndexes]
                            ));
                    })
                    );
                
                if (clearNeedCount < 3) continue;
                
                // 继续迭代一层
                
                // 👉 在这里继续：
                // - 调用 EmitHardClears(newF, newS, newE, clearNeedCount)
                // - 或者决定是否继续推进 BuildNewFSE
            }
            
            
        }
        
        
        
        static int[]  ResolveTileIndexes(
            Tile[] tiles,
            ulong mask
        )
        {
            var resolveIndexes = new int[BitOperations.PopCount(mask)];
            var index = 0;
            for (int i = 0; i < tiles.Length; i++)
                if (((mask >> i) & 1) != 0)
                    resolveIndexes[index++] = tiles[i].Index;
            return resolveIndexes;
        }

        static IEnumerable<(Tile[] newF, Tile[] newS, Tile[] newE)>
            BuildNewFSE(
                Tile[] F,
                Tile[] S,
                Tile[] E,
                Func<Tile, Tile[]> expand
            )
        {
            // 没有展开组，无法推进
            if (E.Length == 0)
                yield break;

            // 对 E 中的每一个 e，生成一个新状态
            foreach (var e in E)
            {
                // 1. 获取展开后同色可选的棋子 
                // 有，继续，没有， 下一个
                var newE = expand(e);
                if (newE.Length == 0) continue;
                
                // 1. 新固定组：F + e
                var newF = new Tile[F.Length + 1];
                Array.Copy(F, newF, F.Length);
                newF[^1] = e;

                // 2. 新可选组：S + (E \ e)
                var newS = new Tile[S.Length + E.Length - 1];
                Array.Copy(S, newS, S.Length);

                int p = S.Length;
                foreach (var other in E)
                {
                    if (!ReferenceEquals(other, e))
                        newS[p++] = other;
                }

                // 3. 新展开组：由 e 展开得到（业务逻辑）
                // var newE = expand(e);

                yield return (newF, newS, newE);
            }
        }
        
        static List<Tile> GetExpandedSameColorSelectableTiles(
            Tile unlockingTile,
            Func<Tile, HashSet<Tile>, bool> lockerPredicate,
            Pasture pasture
        )
        {
            // 1. 展开 unlockingTile 后得到的棋子集合
            HashSet<Tile> expanders = new();
            pasture.Expand(unlockingTile, ref expanders);

            if (expanders.Count == 0)
                return [];

            var result = new List<Tile>();

            foreach (var tile in expanders)
            {
                // 2. 只关心同色
                if (tile.Color != unlockingTile.Color)
                    continue;

                // 3. 查询压着它的上游棋子
                var lockers = new HashSet<Tile>();
                pasture.LockersOf(tile, ref lockers);

                // 4. 由外部决定是否“可选”
                if (lockerPredicate(tile, lockers))
                    result.Add(tile);
            }

            return result;
        }

        
    }

    /// <summary>
    /// 一次困难消除的结构化结果
    /// </summary>
    public readonly struct HardClearPick
    {
        public readonly ulong FixedMask;   // F
        public readonly ulong ExpandMask;  // E'
        public readonly ulong SelectMask;  // S'
    
        public HardClearPick(ulong f, ulong e, ulong s)
        {
            FixedMask  = f;
            ExpandMask = e;
            SelectMask = s;
        }
    }
    // /// <summary>
    /// 简单消除：
    /// 从 selectableMask 中直接选 clearNeedCount 个
    /// </summary>
    public void EmitSimpleClears(
        ulong selectableMask,
        int clearNeedCount,
        Action<ulong> emit   // 直接返回 union mask
    )
    {
        if (BitOperations.PopCount(selectableMask) < clearNeedCount)
            return;

        foreach (var pick in ChooseBits(selectableMask, clearNeedCount))
            emit(pick);
    }

    
    /// <summary>
    /// 困难消除：
    /// 在固定组 / 可选组 / 展开组 下，
    /// 枚举所有满足消除数量的组合。
    ///
    /// 规则：
    /// - |F| + |S'| + |E'| = clearNeedCount
    /// - |E'| >= 1
    /// </summary>
    public void EmitHardClears(
        ulong fixedMask,      // F
        ulong selectableMask, // S
        ulong expandMask,     // E
        int clearNeedCount,
        Action<HardClearPick> emit
    )
    {
        int fCount = BitOperations.PopCount(fixedMask);
        int rest = clearNeedCount - fCount;
        if (rest <= 0) return;

        int eCount = BitOperations.PopCount(expandMask);
        int sCount = BitOperations.PopCount(selectableMask);
        if (eCount == 0) return;

        // i = 从展开组选多少
        // j = 从可选组选多少
        // i >= 1 是“困难消除”的本质
        for (int i = 1; i <= Math.Min(eCount, rest); i++)
        {
            int j = rest - i;
            if (j > sCount) continue;

            foreach (var ePick in ChooseBits(expandMask, i))
            foreach (var sPick in ChooseBits(selectableMask, j))
            {
                emit(new HardClearPick(
                    f: fixedMask,
                    e: ePick,
                    s: sPick
                ));
            }
        }
    }

    public ulong[] ChooseBits(ulong sourceMask, int k)
    {
        return ChooseBitsByGosper(sourceMask, k).ToArray();
    }

    /// <summary>
    /// Gosper's Hack:
    /// 在连续的 n 个位置 (0..n-1) 中，
    /// 枚举所有包含 k 个 1 的 bitmask。
    /// </summary>
    static IEnumerable<ulong> ChooseBitsGosper(int n, int k)
    {
        if (k < 0 || k > n) yield break;
        if (k == 0)
        {
            yield return 0UL;
            yield break;
        }

        // 初始状态：低 k 位为 1
        ulong mask = (1UL << k) - 1;
        ulong limit = 1UL << n;

        while (mask < limit)
        {
            yield return mask;

            // Gosper's Hack（ulong 版）
            ulong c = mask & (~mask + 1);
            ulong r = mask + c;
            mask = (((r ^ mask) >> 2) / c) | r;
        }
    }
    
    static int[] ExtractIndices(ulong mask)
    {
        int count = BitOperations.PopCount(mask);
        var arr = new int[count];

        int p = 0;
        for (int i = 0; i < 64; i++)
            if (((mask >> i) & 1) != 0)
                arr[p++] = i;

        return arr;
    }
    
    /// <summary>
    /// 在一个“稀疏 mask”中，用 Gosper 选 k 个 bit
    /// </summary>
    static IEnumerable<ulong> ChooseBitsByGosper(ulong sourceMask, int k)
    {
        var indices = ExtractIndices(sourceMask);
        int n = indices.Length;

        foreach (var localMask in ChooseBitsGosper(n, k))
        {
            ulong result = 0UL;
            for (int i = 0; i < n; i++)
                if (((localMask >> i) & 1) != 0)
                    result |= 1UL << indices[i];

            yield return result;
        }
    }
}