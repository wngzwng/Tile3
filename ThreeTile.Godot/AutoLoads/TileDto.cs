using ThreeTile.Core.Core;

namespace Tile3.AutoLoads;

/// <summary>
/// 麻将的数据快照
/// </summary>
public readonly struct TileDto
    (int index, int position, int volume, int color, bool isLocked, bool isVisible)
{
    public int Index { get; } = index;
    public int Position { get; } = position;
    public int Volume { get; } = volume;
    public int Color { get; } = color;
    public bool IsLocked { get; } = isLocked;
    public bool IsVisible { get; } = isVisible;

    public static TileDto GetTileDtoFromTile(Tile tile) 
        => new(tile.Index, tile.TilePositionIndex, tile.Volume, tile.Color, tile.IsLocked, tile.IsVisible);
}