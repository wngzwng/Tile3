using System.Numerics;
using ThreeTile.Core.Core.Moves;
using ThreeTile.Core.Core.Zones;

namespace ThreeTile.Core.Core;

public partial class Level
{
    public void GetLogicBehaviours3(int? availableCapacity = null)
    {
        LogicBehaviours.Clear();

        int cap = availableCapacity ?? StagingArea.AvailableCapacity;
        if (cap <= 0) return;

        int matchClearCount = StagingArea.RequiredMatchingElementsCount;

        // --------------------------------------------------
        // SlotMap[color] : 卡槽中已有的颜色数量
        // --------------------------------------------------
        var slotMap = new int[MaxColorIndex + 1];
        foreach (var (color, count) in StagingArea.Counter)
            slotMap[color] = count;

        // --------------------------------------------------
        // OriginSelectableGroup / OriginSelectableMap[color]
        // --------------------------------------------------
        var selectableMap = new List<Tile>[MaxColorIndex + 1];
        foreach (var tile in Pasture.UnlockingTiles)
        {
            selectableMap[tile.Color] ??= new List<Tile>();
            selectableMap[tile.Color].Add(tile);
        }

        // ==================================================
        // per-color processing
        // ==================================================
        for (int color = 0; color <= MaxColorIndex; color++)
        {
            var originSelectable = selectableMap[color];
            if (originSelectable is not { Count: > 0 })
                continue;

            // clearNeedCount[color] = MatchClearCount - SlotMap[color]
            int clearNeedCount = matchClearCount - slotMap[color];

            // 剪枝：容量不够
            if (clearNeedCount <= 0 || clearNeedCount > cap)
                continue;

            // ------------------------------
            // 1. 简单消除
            // ------------------------------
            EmitSimpleClearForColor(
                color,
                originSelectable,
                clearNeedCount
            );

            // ------------------------------
            // 2. 困难消除
            // ------------------------------
            if (clearNeedCount < 2)
                continue;

            var context1 = new HardClearContext(
                fixedGroup: Array.Empty<Tile>(),
                selectableGroup: Array.Empty<Tile>(),
                expandedGroup: originSelectable.ToArray()
            );
            
            EmitHardClearForColor(
                color,
                context1,
                clearNeedCount,
                Pasture,
                (nextCtx) =>
                {
                    
                    if (nextCtx.Fixed.Length + 1 < clearNeedCount)
                    {
                        EmitHardClearForColor(
                            color,
                            nextCtx,
                            clearNeedCount,
                            Pasture,
                            null
                        );
                    }
                }
            );
        }
    }

    
    private void EmitSimpleClearForColor(
        int color,
        List<Tile> originSelectable,
        int clearNeedCount
    )
    {
        ulong selectableMask = FullMask(originSelectable.Count);

        EmitSimpleClears(
            selectableMask,
            clearNeedCount,
            pickMask =>
            {
                var indices = ResolveTileIndexes(originSelectable, pickMask);
                LogicBehaviours.Add(new BehaviourMove(
                    BehaviourKind.EASY_CLEAR,
                    color,
                    indices
                ));
            });
    }

    
    private void EmitHardClearForColor(
        int color,
        HardClearContext context,
        int clearNeedCount,
        Pasture pasture,
        Action<HardClearContext>? then)
    {
        // 初始状态
        
        foreach (var ctx in AdvanceFSE(context, clearNeedCount, pasture))
        {
            EmitHardClearPicks(
                ctx,
                clearNeedCount,
                pick =>
                {
                    var indices = ResolveHardPickIndices(ctx, pick);
                    LogicBehaviours.Add(new BehaviourMove(
                        BehaviourKind.HARD_CLEAR,
                        color,
                        indices
                    ));
                });
            then?.Invoke(ctx);
        }
    }

    
    
    readonly struct HardClearContext
    {
        public readonly Tile[] Fixed;      // F
        public readonly Tile[] Selectable; // S
        public readonly Tile[] Expanded;   // E（已展开得到）

        public int FixedCount => Fixed.Length;

        public HardClearContext(
            Tile[] fixedGroup,
            Tile[] selectableGroup,
            Tile[] expandedGroup)
        {
            Fixed = fixedGroup;
            Selectable = selectableGroup;
            Expanded = expandedGroup;
        }
    }

    
    static IEnumerable<HardClearContext> AdvanceFSE(
        HardClearContext ctx,
        int clearNeedCount,
        Pasture pasture
    )
    {
        var F = ctx.Fixed;
        var S = ctx.Selectable;
        var E = ctx.Expanded;

        // 展开前提：FixedCount + 1 < ClearNeedCount
        if (F.Length + 1 >= clearNeedCount)
            yield break;

        if (E.Length == 0)
            yield break;

        foreach (var e in E)
        {
            var targets = new HashSet<Tile>(F);
            // 展开 e 得到同色可选棋子
            var newExpanded = GetExpandedSameColorSelectableTiles(
                e,
                (tile, upstreams) =>
                {
                    targets.Add(e);
                    var isOk = upstreams.SetEquals(targets);
                    targets.Remove(e);
                    return isOk;
                    // upstreams.Count == 1 &&
                    // upstreams.Contains(e),
                },
                pasture
            ).ToArray();

            if (newExpanded.Length == 0)
                continue;

            // F' = F + e
            var newF = Append(F, e);

            // S' = S + (E \ e)
            var newS = AppendRange(S, E, skip: e);

            yield return new HardClearContext(
                fixedGroup: newF,
                selectableGroup: newS,
                expandedGroup: newExpanded
            );
        }
        
        /// <summary>
        /// 追加单个元素：
        /// 用于 Fixed 组推进（F' = F + e）
        /// </summary>
        static Tile[] Append(Tile[] src, Tile item)
        {
            var dst = new Tile[src.Length + 1];
            Array.Copy(src, dst, src.Length);
            dst[^1] = item;
            return dst;
        }
        
        /// <summary>
        /// 追加一组元素（跳过指定元素）
        ///
        /// 用于：
        /// S' = S + (E \ e)
        /// </summary>
        static Tile[] AppendRange(
            Tile[] baseGroup,
            Tile[] fromGroup,
            Tile skip)
        {
            var dst = new Tile[baseGroup.Length + fromGroup.Length - 1];

            // 1. 复制原有 S
            Array.Copy(baseGroup, dst, baseGroup.Length);

            // 2. 追加 E 中除 e 以外的元素
            int p = baseGroup.Length;
            foreach (var t in fromGroup)
            {
                if (!ReferenceEquals(t, skip))
                    dst[p++] = t;
            }

            return dst;
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
            pasture.LockersOf(tile, ref lockers, true);

            // 4. 由外部决定是否“可选”
            if (lockerPredicate(tile, lockers))
                result.Add(tile);
        }

        return result;
    }

    
    
    
    void EmitHardClearPicks(
        HardClearContext ctx,
        int clearNeedCount,
        Action<HardClearPick> emit
    )
    {
        int fixedCount = ctx.Fixed.Length;
        int rest = clearNeedCount - fixedCount;

        if (rest <= 0)
            return;

        int eCount = ctx.Expanded.Length;
        int sCount = ctx.Selectable.Length;

        if (eCount == 0)
            return;

        ulong eMask = FullMask(eCount);
        ulong sMask = FullMask(sCount);

        // ExpandedPickCount: 1 ~ (ClearNeedCount - FixedCount)
        for (int ePickCount = 1;
             ePickCount <= Math.Min(eCount, rest);
             ePickCount++)
        {
            int sPickCount = rest - ePickCount;
            if (sPickCount > sCount)
                continue;

            foreach (var ePick in ChooseBits(eMask, ePickCount))
            foreach (var sPick in ChooseBits(sMask, sPickCount))
            {
                emit(new HardClearPick(
                    f: FullMask(fixedCount),
                    e: ePick,
                    s: sPick
                ));
            }
        }
    }

    
    static ulong FullMask(int n)
        => n >= 64 ? ulong.MaxValue : ((1UL << n) - 1);

    static int[] ResolveTileIndexes(
        List<Tile> tiles,
        ulong mask)
    {
        var result = new int[BitOperations.PopCount(mask)];
        int p = 0;
        for (int i = 0; i < tiles.Count; i++)
            if (((mask >> i) & 1) != 0)
                result[p++] = tiles[i].Index;
        return result;
    }

    static int[] ResolveHardPickIndices(
        HardClearContext ctx,
        HardClearPick pick)
    {
        var list = new List<int>();

        AddByMask(ctx.Fixed, pick.FixedMask, list);
        AddByMask(ctx.Selectable, pick.SelectMask, list);
        AddByMask(ctx.Expanded, pick.ExpandMask, list);

        return list.ToArray();
    }
    
    /// <summary>
    /// 根据 bitmask，将 src 中被选中的 Tile.Index
    /// 追加到 dst 列表中。
    ///
    /// 约定：
    /// - mask 的第 i 位对应 src[i]
    /// - src.Length <= 64
    /// </summary>
    static void AddByMask(
        Tile[] src,
        ulong mask,
        List<int> dst
    )
    {
        // 快速路径：没有选任何一个
        if (mask == 0UL)
            return;

        for (int i = 0; i < src.Length; i++)
        {
            if (((mask >> i) & 1UL) != 0)
                dst.Add(src[i].Index);
        }
    }


}