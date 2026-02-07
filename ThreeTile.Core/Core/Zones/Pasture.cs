using System.Text;
using ThreeTile.Core.Core.LayerShadows;
using ThreeTile.Core.ExtensionTools;

namespace ThreeTile.Core.Core.Zones;

using Position = int;

// 层级投影 可以通过

/// <summary>
/// 牧场：Tile 的空间盘面
/// 负责维护 Tile 状态与遮挡结果
/// </summary>
public sealed class Pasture
{
    // ───────── 主状态 ─────────
    public Level Parent;
    public List<Tile> Tiles { get; } = [];

    // ───────── 派生视图 ─────────

    public List<Tile> UnlockingTiles { get; } = [];
    public List<Tile> VisibleTiles { get; } = [];

    // ───────── 索引结构 ─────────

    /// <summary> Index → Tile</summary>
    public Dictionary<int, Tile> IndexToTileDict { get; } = [];

    /// <summary>空间坐标 → Tile</summary>
    public Dictionary<Position, Tile> PosTileDict { get; } = [];

    /// <summary>TopZ → Tiles</summary>
    public Dictionary<int, HashSet<Tile>> TopZTileDict { get; } = [];

    /// <summary>Tile → 自身顶面投影</summary>
    public Dictionary<Tile, LayerShadow> TileShadowDict { get; } = [];

    // ───────── 遮挡系统 ─────────

    public LayerShadowManager ShadowManager { get; }

    private readonly int _maxRow;
    private readonly int _maxCol;
    private readonly int _maxLayer;

    // ───────── 构造 ─────────

    public Pasture(
        int maxRow,
        int maxCol,
        int maxLayer,
        ShadowPropagationEnum mode,
        Level parent)
    {
        _maxRow = maxRow;
        _maxCol = maxCol;
        _maxLayer = maxLayer;
        Parent = parent;
        
        ShadowManager = new LayerShadowManager(mode, maxRow, maxCol, maxLayer);
    }

    // ───────── Tile 状态更新 ─────────

    private void UpdateTileLockedStatus(Tile tile)
    {
        bool covered =
            ShadowManager.IntersectsIncoming(tile.TopZ, TileShadowDict[tile]);
        tile.SetLocked(covered);
    }

    private void UpdateTileVisibleStatus(Tile tile)
    {
        int coverCount =
            ShadowManager.IncomingIntersectCount(tile.TopZ, TileShadowDict[tile]);
        
        // 这个可以自动适配不同大小的Tile
        int total = TileShadowDict[tile].PopCount(); // 通过自身的顶层面获取
        // int total = tile.TopCoordinates.Count(); // 这个也可以
        tile.SetVisible(coverCount < total);
    }

    private void UpdateAllTileStatus()
    {
        foreach (var tile in Tiles)
        {
            UpdateTileLockedStatus(tile);
            UpdateTileVisibleStatus(tile);
        }
    }

    // ───────── 派生列表刷新 ─────────

    private void RefreshDerivedLists()
    {
        UnlockingTiles.Clear();
        VisibleTiles.Clear();

        foreach (var tile in Tiles)
        {
            if (!tile.IsLocked)
                UnlockingTiles.Add(tile);
            if (tile.IsVisible)
                VisibleTiles.Add(tile);
        }
    }

    public void RefreshUnlockingAndVisibleTiles()
    {
        UpdateAllTileStatus();
        RefreshDerivedLists();
    }

    // ───────── Tile 生命周期 ─────────

    public void AddTile(Tile tile)
    {
        // 1. 空间冲突检测
        foreach (var pos in tile.Coordinates)
        {
            if (PosTileDict.ContainsKey(pos))
                return;
        }

        tile.SetTileZone(Tile.TileZoneEnum.Pasture);
        // 2. 加入主列表
        Tiles.Add(tile);

        // 3. 建立空间索引
        foreach (var pos in tile.Coordinates)
            PosTileDict[pos] = tile;

        IndexToTileDict[tile.Index] = tile;

        if (!TopZTileDict.TryGetValue(tile.TopZ, out var set))
        {
            set = new HashSet<Tile>();
            TopZTileDict[tile.TopZ] = set;
        }
        set.Add(tile);

        // 4. 构建 Tile 自身投影
        var tileShadow = new LayerShadow(_maxRow, _maxCol);
        foreach (var pos in tile.TopCoordinates)
            tileShadow.AddPosition(pos);

        TileShadowDict[tile] = tileShadow;

        // 5. 更新遮挡系统
        ShadowManager.AddSelfShadow(tile.TopZ, tile.TopCoordinates);
        ShadowManager.RebuildIncomingAfterChange();

        // 6. 刷新派生状态
        RefreshUnlockingAndVisibleTiles();
    }

    public void RemoveTile(Tile tile)
    {
        if (!Tiles.Contains(tile))
            return;

        // 1. 从主列表移除
        Tiles.Remove(tile);

        // 2. 移除空间索引
        foreach (var pos in tile.Coordinates)
            PosTileDict.Remove(pos);

        IndexToTileDict.Remove(tile.Index);

        if (TopZTileDict.TryGetValue(tile.TopZ, out var set))
        {
            set.Remove(tile);
            if (set.Count == 0)
                TopZTileDict.Remove(tile.TopZ);
        }

        // 3. 更新遮挡系统
        ShadowManager.RemoveSelfShadow(tile.TopZ, tile.TopCoordinates);
        ShadowManager.RebuildIncomingAfterChange();

        // 4. 移除 Tile Shadow
        TileShadowDict.Remove(tile);

        // 5. 刷新派生状态
        RefreshUnlockingAndVisibleTiles();
    }


    public bool CanSelect(int tileIndex)
    {
        return UnlockingTiles.Any(tile => tile.Index == tileIndex);
    }

    public Tile Extract(int tileIndex)
    {
        var tile = IndexToTileDict[tileIndex];
        RemoveTile(tile);
        return tile;
    }
    
    public Pasture Clone()
    {
        // 1️⃣ 新建一个空的 Pasture
        var clone = new Pasture(
            _maxRow,
            _maxCol,
            _maxLayer,
            ShadowManager.Mode, // 假设你能拿到 mode
            parent: null        // Level.Clone() 里再修
        );

        // 2️⃣ 深拷贝 Tile，并按原顺序放回
        foreach (var tile in Tiles)
        {
            var tileClone = tile.Clone();
            clone.AddTile(tileClone);
        }

        // 3️⃣ 返回
        return clone;
    }

    public void Expand(Tile tile, ref HashSet<Tile> expanders)
    {
        // 先简单处理，只展开它压着的那层
        var downNeighbours = tile.TilePositionIndex.GetPositionDownNeighbourPositions();
        foreach (var downNeighbour in downNeighbours)
        {
            if (PosTileDict.TryGetValue(downNeighbour, out var targetTile))
                expanders.Add(targetTile);
        }
    }

    /// <summary>
    /// 返回所有“直接导致 tile 被锁定”的棋子
    /// 即：只要它们还在，tile 就不可能可选
    /// </summary>
    public void LockersOf(Tile tile, ref HashSet<Tile> blocker, bool all = false)
    {
        // 如果 tile 本身已经可选，说明没有锁定者
        if (!tile.IsLocked)
            return;

        // 获取该棋子的上方邻居
        var upNeighbours = tile.TilePositionIndex.GetPositionUpNeighbourPositions();
        var curblocker = new HashSet<Tile>(); // 这次的blocker
        foreach (var neighbour in upNeighbours)
        {
            if (PosTileDict.TryGetValue(neighbour, out var targetTile))
                curblocker.Add(targetTile);
        }

        if (!all)
        {
            blocker.UnionWith(curblocker);
            return;
        }

        var newBlocker = curblocker.Except(blocker).ToHashSet();
        blocker.UnionWith(curblocker);
        if (newBlocker.Count > 0)
        {
            foreach (var newB in newBlocker)
            {
                LockersOf(newB, ref blocker, all);
            }
        }
    }
    
    public override string ToString()
    {
        return $"Tiles={Tiles.Count}, UnlockingTiles={UnlockingTiles.Count}, Visible={VisibleTiles.Count} \n {DumpLayerShadows()}";
    }
    
    public string DumpLayerShadows()
    {
        var sb = new StringBuilder();

        for (int z = 0; z < _maxLayer; z++)
        {
            sb.AppendLine($"Layer Z={z}:");
            DumpSingleLayer(sb, z);
        }

        return sb.ToString();
    }

    private void DumpSingleLayer(StringBuilder sb, int z)
    {
        var layer = ShadowManager.GetSelfShadow(z);
        layer.DumpGrid(sb);
    }

}

