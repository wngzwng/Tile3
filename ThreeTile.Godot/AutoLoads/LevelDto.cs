using System;
using ThreeTile.Core.Core;
using ThreeTile.Core.ExtensionTools;

namespace Tile3.AutoLoads;
public readonly struct LevelDto(int size)
{
    public int Size { get; } = size;

    public static LevelDto GetLevelDtoFromLevel(Level level)
    {
        var maxX = int.MinValue;
        var maxY = int.MinValue;
        var maxZ = int.MinValue;

        foreach (var tile in level.Pasture.Tiles)
        {
            maxX = Math.Max(maxX, tile.TilePositionIndex.X() + tile.Volume.X());
            maxY = Math.Max(maxY, tile.TilePositionIndex.Y() + tile.Volume.Y());
            maxZ = Math.Max(maxZ, tile.TilePositionIndex.Z() + tile.Volume.Z());
        }
        
        return new LevelDto
        (
            (
                maxX,
                maxY,
                maxZ
            ).PackXyz()
        );
    }

    public override string ToString()
    {
        var (x, y, z) = size.UnpackXyz();
        return $"{x},{y},{z}";
    }
}