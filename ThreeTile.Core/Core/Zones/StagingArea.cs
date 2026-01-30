namespace ThreeTile.Core.Core.Zones;

/// <summary>
/// 集结区 / 卡槽
/// 负责：
/// - Tile 暂存
/// - 容量约束
/// - 按颜色统计
/// - 匹配判定
/// 支持：
/// - 正向完成
/// - 回滚恢复（Undo / Solver）
/// </summary>
public sealed class StagingArea
{
    // ─────────────────────────
    // 基础状态
    // ─────────────────────────

    public Level Parent;

    private readonly List<Tile> _tiles;
    private readonly Dictionary<int, int> _colorCounter;

    public IReadOnlyList<Tile> Tiles => _tiles;
    public IReadOnlyDictionary<int, int> Counter => _colorCounter;

    // ─────────────────────────
    // 规则参数
    // ─────────────────────────

    /// <summary> 触发匹配所需的最少数量 </summary>
    public int RequiredMatchingElementsCount { get; }

    /// <summary> 最大容量（卡槽总数） </summary>
    public int Capacity { get; }

    // ─────────────────────────
    // 容量派生状态（单一事实源：_tiles.Count）
    // ─────────────────────────

    /// <summary> 已使用的容量 </summary>
    public int UsedCapacity => _tiles.Count;

    /// <summary> 剩余可用容量 </summary>
    public int AvailableCapacity => Capacity - _tiles.Count;

    /// <summary> 是否已满（不能再放） </summary>
    public bool IsFull => UsedCapacity >= Capacity;

    /// <summary> 是否为空 </summary>
    public bool IsEmpty => UsedCapacity == 0;

    // ─────────────────────────
    // 构造
    // ─────────────────────────

    public StagingArea(int capacity, int requiredMatchingElementsCount, Level parent)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (requiredMatchingElementsCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredMatchingElementsCount));

        if (requiredMatchingElementsCount > capacity)
            throw new ArgumentException(
                "requiredMatchingElementsCount cannot exceed capacity");

        Parent = parent;
        Capacity = capacity;
        RequiredMatchingElementsCount = requiredMatchingElementsCount;

        _tiles = new List<Tile>(capacity);
        _colorCounter = new Dictionary<int, int>();
    }

    // ─────────────────────────
    // 入槽（占用容量）
    // ─────────────────────────

    /// <summary>
    /// 尝试加入一个 Tile（失败不修改状态）
    /// </summary>
    public bool TryAdd(Tile tile)
    {
        if (IsFull)
            return false;

        tile.SetTileZone(Tile.TileZoneEnum.StagingArea);

        _tiles.Add(tile);

        if (_colorCounter.TryGetValue(tile.Color, out var count))
            _colorCounter[tile.Color] = count + 1;
        else
            _colorCounter[tile.Color] = 1;

        return true;
    }

    /// <summary>
    /// 放入并返回槽位索引（仅在调用方确保未满时使用）
    /// </summary>
    public int Stage(Tile tile)
    {
        int slotIndex = _tiles.Count;
        TryAdd(tile);
        return slotIndex;
    }

    public bool CanAccept() => !IsFull;

    // ─────────────────────────
    // 匹配判定（只读）
    // ─────────────────────────

    /// <summary>
    /// 是否已经满足某种颜色的匹配条件
    /// </summary>
    public bool HasMatchCompleted(int color)
    {
        return _colorCounter.TryGetValue(color, out var count)
               && count >= RequiredMatchingElementsCount;
    }

    /// <summary>
    /// 仅判断并取出匹配组（不修改状态）
    /// </summary>
    public bool TryResolve(int color, out List<Tile> group)
    {
        group = null;

        if (!HasMatchCompleted(color))
            return false;

        group = GetTilesByColor(color);
        return true;
    }

    // ─────────────────────────
    // 正向完成（释放容量）
    // ─────────────────────────

    /// <summary>
    /// 从 StagingArea 中移除一组 Tile（用于正向完成）
    /// </summary>
    public void Remove(IEnumerable<Tile> tiles)
    {
        foreach (var tile in tiles)
        {
            bool removed = _tiles.Remove(tile);
#if DEBUG
            if (!removed)
                throw new InvalidOperationException(
                    $"Tile not found in staging area: index={tile.Index}");
#endif
            int c = _colorCounter[tile.Color] - 1;
            if (c == 0)
                _colorCounter.Remove(tile.Color);
            else
                _colorCounter[tile.Color] = c;
        }
    }

    // ─────────────────────────
    // 回滚 / 退回（恢复容量）
    // ─────────────────────────

    /// <summary>
    /// 将一组 Tile 原样退回 StagingArea（用于回滚）
    /// </summary>
    public void Restore(IEnumerable<Tile> tiles)
    {
        foreach (var tile in tiles)
        {
#if DEBUG
            if (IsFull)
                throw new InvalidOperationException(
                    "StagingArea overflow during restore");
#endif
            tile.SetTileZone(Tile.TileZoneEnum.StagingArea);

            _tiles.Add(tile);

            if (_colorCounter.TryGetValue(tile.Color, out var count))
                _colorCounter[tile.Color] = count + 1;
            else
                _colorCounter[tile.Color] = 1;
        }
    }

    public Tile Unstage(int tileIndex)
    {
        Tile target = null;
        foreach (var tile in _tiles)
        {
            if (tile.Index == tileIndex)
            {
                target = tile;
                break;
            }
        }

        if (target != null)
        {
            _tiles.Remove(target);
            int c = _colorCounter[target.Color] - 1;
            if (c == 0)
                _colorCounter.Remove(target.Color);
            else
                _colorCounter[target.Color] = c;
        }

        return target;
    }

    // ─────────────────────────
    // 查询（快照）
    // ─────────────────────────

    /// <summary>
    /// 获取某种颜色的 Tile 快照（会分配）
    /// </summary>
    public List<Tile> GetTilesByColor(int color)
    {
        if (!_colorCounter.TryGetValue(color, out var count))
            return new List<Tile>();

        var result = new List<Tile>(count);
        foreach (var tile in _tiles)
            if (tile.Color == color)
                result.Add(tile);

        return result;
    }


    public StagingArea Clone()
    {
        // Parent 在 Level.Clone 里再统一修
        var clone = new StagingArea(Capacity, RequiredMatchingElementsCount, parent: null);

        // 深拷贝 tiles
        foreach (var tile in _tiles)
        {
            var tileClone = tile.Clone();
            clone._tiles.Add(tileClone);

            if (clone._colorCounter.TryGetValue(tileClone.Color, out var count))
                clone._colorCounter[tileClone.Color] = count + 1;
            else
                clone._colorCounter[tileClone.Color] = 1;
        }

        return clone;
    }

}
