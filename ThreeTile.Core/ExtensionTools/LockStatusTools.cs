namespace ThreeTile.Core.ExtensionTools;

public static class LockStatusTools
{
    /// <summary>
    /// 返回一个位置左侧相邻位置的方法，此处默认麻将体积为 (2, 2, 1)
    /// </summary>
    /// <returns>每一个传入位置和体积对应左侧的单元位置坐标的数组</returns>
    public static int[] GetPositionLeftNeighbourPositions(this int position, int? volume = null)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume?.UnpackXyz() ?? (x: 2, y: 2, z: 1);
        List<int> result = [];
        // 对于每一层进行循环
        for (var zOffset = 0; zOffset < dz; zOffset++)
        {
            // 对于每一行进行循环
            for (var yOffset = 0; yOffset < dy; yOffset++)
            {
                result.Add((px - 1, py + yOffset, pz + zOffset).PackXyz());
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 返回一个位置右侧相邻位置的方法，此处默认麻将体积为 (2, 2, 1)
    /// </summary>
    /// <returns>每一个传入位置和体积对应右侧的单元位置坐标的数组</returns>
    public static int[] GetPositionRightNeighbourPositions(this int position, int? volume = null)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume?.UnpackXyz() ?? (x: 2, y: 2, z: 1);
        List<int> result = [];
        // 对于每一层进行循环
        for (var zOffset = 0; zOffset < dz; zOffset++)
        {
            // 对于每一行进行循环
            for (var yOffset = 0; yOffset < dy; yOffset++)
            {
                result.Add((px + dx, py + yOffset, pz + zOffset).PackXyz());
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 返回一个位置前方相邻位置的方法，此处默认麻将体积为 (2, 2, 1)
    /// </summary>
    /// <returns>每一个传入位置和体积对应前方的单元位置坐标的数组</returns>
    public static int[] GetPositionFrontNeighbourPositions(this int position, int? volume = null)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume?.UnpackXyz() ?? (x: 2, y: 2, z: 1);
        List<int> result = [];

        // 对于每一层进行循环
        for (var zOffset = 0; zOffset < dz; zOffset++)
        {
            // 对于每一列进行循环
            for (var xOffset = 0; xOffset < dx; xOffset++)
            {
                result.Add((px + xOffset, py + dy, pz + zOffset).PackXyz());
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 返回一个位置后方相邻位置的方法，此处默认麻将体积为 (2, 2, 1)
    /// </summary>
    /// <returns>每一个传入位置和体积对应后方的单元位置坐标的数组</returns>
    public static int[] GetPositionBehindNeighbourPositions(this int position, int? volume = null)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume?.UnpackXyz() ?? (x: 2, y: 2, z: 1);
        List<int> result = [];

        // 对于每一层进行循环
        for (var zOffset = 0; zOffset < dz; zOffset++)
        {
            // 对于每一列进行循环
            for (var xOffset = 0; xOffset < dx; xOffset++)
            {
                result.Add((px + xOffset, py - 1, pz + zOffset).PackXyz());
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 返回一个位置顶部相邻位置的方法，此处默认麻将体积为 (2, 2, 1)
    /// </summary>
    /// <returns>每一个传入位置和体积对应顶部的单元位置坐标的数组</returns>
    public static int[] GetPositionUpNeighbourPositions(this int position, int? volume = null)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume?.UnpackXyz() ?? (x: 2, y: 2, z: 1);
        List<int> result = [];
        // 对于每一层进行循环
        for (var xOffset = 0; xOffset < dx; xOffset++)
        {
            // 对于每一行进行循环
            for (var yOffset = 0; yOffset < dy; yOffset++)
            {
                result.Add((px + xOffset, py + yOffset, pz + dz).PackXyz());
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 返回一个位置底部相邻位置的方法，此处默认麻将体积为 (2, 2, 1)
    /// </summary>
    /// <returns>每一个传入位置和体积对应底部的单元位置坐标的数组</returns>
    public static int[] GetPositionDownNeighbourPositions(this int position, int? volume = null)
    {
        var (px, py, pz) = position.UnpackXyz();
        var (dx, dy, dz) = volume?.UnpackXyz() ?? (x: 2, y: 2, z: 1);
        List<int> result = [];
        // 对于每一层进行循环
        for (var xOffset = 0; xOffset < dx; xOffset++)
        {
            // 对于每一行进行循环
            for (var yOffset = 0; yOffset < dy; yOffset++)
            {
                result.Add((px + xOffset, py + yOffset, pz - 1).PackXyz());
            }
        }

        return result.ToArray();
    }
}