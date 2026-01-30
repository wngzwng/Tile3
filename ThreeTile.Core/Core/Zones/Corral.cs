namespace ThreeTile.Core.Core.Zones;

/// <summary>
/// 围栏：已完成（收牧成功）的 Tile 集合
/// </summary>
public sealed class Corral
{
    public Level Parent;

    // 🔒 权威结构：按收牧顺序存放（用于回滚）
    private readonly List<Tile> _orderedTiles = new();

    // 🔒 派生结构：颜色计数
    private readonly Dictionary<int, int> _colorCounter = new();

    public int TotalCount => _orderedTiles.Count;

    public IReadOnlyDictionary<int, int> ColorCounter => _colorCounter;

    public Corral(Level parent)
    {
        Parent = parent;
    }

    // ─────────────────────────
    // 收牧
    // ─────────────────────────

    public void Accept(IEnumerable<Tile> tiles)
    {
        foreach (var tile in tiles)
            Add(tile);
    }

    public void Add(Tile tile)
    {
        tile.SetTileZone(Tile.TileZoneEnum.Corral);

        _orderedTiles.Add(tile);

        if (_colorCounter.TryGetValue(tile.Color, out var count))
            _colorCounter[tile.Color] = count + 1;
        else
            _colorCounter[tile.Color] = 1;
    }

    // ─────────────────────────
    // 退牧（Undo / 回滚）
    // ─────────────────────────

    /// <summary>
    /// 退牧：撤回最近收牧的 count 个 Tile（LIFO）
    /// </summary>
    public List<Tile> Retrieve(int count)
    {
        if (count <= 0)
            return new List<Tile>();

#if DEBUG
        if (count > _orderedTiles.Count)
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"Retrieve count({count}) exceeds TotalCount({_orderedTiles.Count})");
#endif

        var result = new List<Tile>(count);

        for (int i = 0; i < count; i++)
        {
            int lastIndex = _orderedTiles.Count - 1;
            var tile = _orderedTiles[lastIndex];
            _orderedTiles.RemoveAt(lastIndex);

            // 更新计数
            int c = _colorCounter[tile.Color] - 1;
            if (c == 0)
                _colorCounter.Remove(tile.Color);
            else
                _colorCounter[tile.Color] = c;

            result.Add(tile);
        }

        return result;
    }

    // ─────────────────────────
    // 查询
    // ─────────────────────────

    public bool HasCompletedColor(int color, int requiredCount)
    {
        return _colorCounter.TryGetValue(color, out var count)
               && count >= requiredCount;
    }

    public bool IsAllCollected(int totalTileCount)
    {
        return _orderedTiles.Count >= totalTileCount;
    }

    // ⚠️ 只读分组视图（非核心路径使用）
    public IReadOnlyDictionary<int, IReadOnlyList<Tile>> GetTilesByColorSnapshot()
    {
        return _orderedTiles
            .GroupBy(t => t.Color)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Tile>)g.ToList()
            );
    }

    public Corral Clone()
    {
        // parent 先置空，交给 Level.Clone() 统一修
        var clone = new Corral(parent: null);

        // 深拷贝收牧顺序（权威数据）
        foreach (var tile in _orderedTiles)
        {
            var tileClone = tile.Clone();
            clone._orderedTiles.Add(tileClone);

            // 同步颜色计数
            if (clone._colorCounter.TryGetValue(tileClone.Color, out var count))
                clone._colorCounter[tileClone.Color] = count + 1;
            else
                clone._colorCounter[tileClone.Color] = 1;
        }

        return clone;
    }
}
