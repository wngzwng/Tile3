using ThreeTile.Core.Core.LayerShadows;
using ThreeTile.Core.Core.Moves;
using ThreeTile.Core.Core.Zones;
using ThreeTile.Core.ExtensionTools;

namespace ThreeTile.Core.Core;

public class Level
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


    public void DoMove(Move move)
    {
        if (!move.CanDo(this)) throw new InvalidOperationException("操作不合法");
        
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



}