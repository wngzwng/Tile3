using ThreeTile.Core.ExtensionTools;

namespace ThreeTile.Core.Core;

/// <summary>
/// Tile：ThreeTile 中的空间实体
///
/// 设计约定：
/// 1. Pasture 模式下，TilePositionIndex 表示 packed (x,y,z) 的最小坐标
/// 2. Tile 有体积 (dx,dy,dz)，实际占据的是一个长方体
/// 3. 遮挡 / 可见性判定只基于 Tile 的“顶面”（TopZ / TopCoordinates）
/// 4. TilePosition 与 Volume 在放置完成后视为稳定
///    Coordinates / TopCoordinates 为缓存结果
/// </summary>
public sealed class Tile : IEquatable<Tile>
{
    // ─────────────────────────
    // Constants
    // ─────────────────────────

    /// <summary> 颜色未指定 常量 </summary>
    public const int COLOR_UNSPECIFIED = -1;

    /// <summary> 默认体积 (2,2,1) </summary>
    public static readonly int DefaultVolume = (x: 2, y: 2, z: 1).PackXyz();

    // ─────────────────────────
    // Identity / Basic State
    // ─────────────────────────

    /// <summary> 关卡内唯一 ID </summary>
    public int Index { get; private set; }

    /// <summary> Tile 的花色 </summary>
    public int Color { get; private set; }

    /// <summary> Tile 的体积（packed xyz） </summary>
    public int Volume { get; }

    /// <summary> 是否锁定 </summary>
    public bool IsLocked { get; private set; }

    /// <summary> 是否可见（逻辑状态，不等价于遮挡） </summary>
    public bool IsVisible { get; private set; } = true;

    // ─────────────────────────
    // Position
    // ─────────────────────────

    public enum TileZoneEnum
    {
        Pasture,        // 牧场（空间放置）
        StagingArea,    // 集结区（槽位）
        Corral,         // 围栏（槽位）
        Unknown
    }

    /// <summary> Tile 所在区域 </summary>
    public TileZoneEnum TileZone { get; private set; } = TileZoneEnum.Unknown;

    /// <summary>
    /// Tile 的牧场位置索引：
    /// - Pasture      ：packed (x,y,z) 的最小坐标
    /// </summary>
    public int TilePositionIndex { get; private set; }

    // ─────────────────────────
    // Spatial Cache (Pasture only)
    // ─────────────────────────

    /// <summary> Tile 占据的所有空间坐标（体积展开） </summary>
    public int[] Coordinates { get; private set; } = [];

    /// <summary> Tile 顶面占据的空间坐标（用于遮挡） </summary>
    public int[] TopCoordinates { get; private set; } = [];

    /// <summary> Tile 的顶面 Z（遮挡判定层） </summary>
    public int TopZ { get; private set; }

    // ─────────────────────────
    // Construction
    // ─────────────────────────

    public Tile(int index, int position, int color = COLOR_UNSPECIFIED, int? volume = null)
    {
        Index = index;
        Color = color;
        Volume = volume ?? DefaultVolume;

        TilePositionIndex = position;
        Coordinates = BuildCoordinates(position, Volume);
        TopCoordinates = BuildTopCoordinates(position, Volume);
        TopZ = ComputeTopZ(position, Volume);
    }

    // ─────────────────────────
    // State Setters
    // ─────────────────────────

    public void SetIndex(int index) => Index = index;

    public void SetColor(int color) => Color = color;

    public void SetLocked(bool locked) => IsLocked = locked;

    public void SetVisible(bool visible) => IsVisible = visible;

    /// <summary>
    /// 设置 Tile 的位置
    /// </summary>
    public void SetTileZone(TileZoneEnum zoneEnum)
    {
        TileZone = zoneEnum;
    }

    // ─────────────────────────
    // Spatial Helpers
    // ─────────────────────────

    /// <summary>
    /// 计算 Tile 的顶面 Z（遮挡判定用）
    /// </summary>
    public static int ComputeTopZ(int position, int volume)
    {
        var (_, _, z0) = position.UnpackXyz();
        var (_, _, dz) = volume.UnpackXyz();
        return z0 + dz - 1;
    }

    /// <summary>
    /// 构建 Tile 占据的全部空间坐标
    /// </summary>
    private static int[] BuildCoordinates(int position, int volume)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume.UnpackXyz();

        var result = new List<int>(dx * dy * dz);

        for (int x = 0; x < dx; x++)
        for (int y = 0; y < dy; y++)
        for (int z = 0; z < dz; z++)
        {
            result.Add((px + x, py + y, pz + z).PackXyz());
        }

        return result.ToArray();
    }

    /// <summary>
    /// 构建 Tile 顶面占据的空间坐标（用于遮挡）
    /// </summary>
    private static int[] BuildTopCoordinates(int position, int volume)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume.UnpackXyz();
        var topZ = pz + dz - 1;

        var result = new List<int>(dx * dy);

        for (int x = 0; x < dx; x++)
        for (int y = 0; y < dy; y++)
        {
            result.Add((px + x, py + y, topZ).PackXyz());
        }

        return result.ToArray();
    }

    // ─────────────────────────
    // Clone / Equality
    // ─────────────────────────

    public Tile Clone()
    {
        var clone = new Tile(Index, Color, Volume)
        {
            IsLocked = IsLocked,
            IsVisible = IsVisible,
            TileZone = TileZone,
            TilePositionIndex = TilePositionIndex,
            Coordinates = (int[])Coordinates.Clone(),
            TopCoordinates = (int[])TopCoordinates.Clone(),
            TopZ = TopZ
        };

        return clone;
    }

    public bool Equals(Tile? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Index == other.Index;
    }

    public override bool Equals(object? obj)
        => obj is Tile other && Equals(other);

    public override int GetHashCode()
        => Index;

    // ─────────────────────────
    // Debug
    // ─────────────────────────

    public override string ToString()
        => $"Tile #{Index} | " +
           $"Color={Color} | " +
           $"Area={TileZone} | " +
           $"Pasture Pos={ TilePositionIndex.ToXyzString()} | " +
           $"Volume={Volume.ToXyzString()} | " +
           $"Locked={IsLocked} | " +
           $"Visible={IsVisible}";
}