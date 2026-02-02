namespace ThreeTile.Core.Designer;

/// <summary>
/// 花色分配配置
/// </summary>
public sealed record DistributeConfig
{
    /// <summary> 总 Tile 数量 </summary>
    public int TotalCount { get; init; }

    /// <summary> 可用花色数量 </summary>
    public int AvailableColorCount { get; init; }

    /// <summary> 消除所需 Tile 数（如 3 表示一对） </summary>
    public int MatchRequireCount { get; init; }

    /// <summary> 正常情况下每个花色最大 Pair 数（保底上限） </summary>
    public int NormalMaxColorPairCount { get; init; }

    /// <summary> 分配模式 </summary>
    public ColorDistributor.ColorDistributeMode DistributeMode { get; init; }

    /// <summary>
    /// Specified 模式下的“轮次”（🐰数量调制用）
    /// </summary>
    public int RoundCount { get; init; }

    /// <summary>
    /// 特殊指定的花色 Tile 数（必须是 MatchRequireCount 的倍数）
    /// </summary>
    public int[]? SpecialColorCountArray { get; init; }
}

public class ColorDistributor
{
    public enum ColorDistributeMode
    {
        Random = 0,     // 随机分配
        Max = 1,        // 尽量大花色
        Min = 2,        // 尽量多花色
        Specified = 3,  // 指定“兔子”数量
    }
    
   public static Dictionary<int, int> Distribute(DistributeConfig config)
    {
        ValidateConfig(config);

        int remainingTiles = config.TotalCount;
        int remainingColors = config.AvailableColorCount;

        // 每个元素 = 该花色的 Tile 数
        List<int> colorTileCounts = new();

        #region 处理特殊花色

        if (config.SpecialColorCountArray is not null)
        {
            foreach (int tileCount in config.SpecialColorCountArray)
            {
                colorTileCounts.Add(tileCount);
                remainingTiles -= tileCount;
                remainingColors--;
            }
        }

        #endregion

        int maxPairPerColor = ComputeMaxPairPerColor(
            remainingTiles,
            remainingColors,
            config.MatchRequireCount,
            config.NormalMaxColorPairCount
        );

        #region 主分配逻辑

        switch (config.DistributeMode)
        {
            case ColorDistributeMode.Random:
                DistributeRandom(colorTileCounts, ref remainingTiles, remainingColors, config.AvailableColorCount, maxPairPerColor, config.MatchRequireCount);
                break;

            case ColorDistributeMode.Max:
                DistributeMax(colorTileCounts, ref remainingTiles, remainingColors, config.AvailableColorCount, maxPairPerColor, config.MatchRequireCount);
                break;

            case ColorDistributeMode.Min:
                DistributeMin(colorTileCounts, ref remainingTiles, remainingColors, config.AvailableColorCount, config.MatchRequireCount);
                break;

            case ColorDistributeMode.Specified:
                DistributeSpecified(
                    colorTileCounts,
                    ref remainingTiles,
                    remainingColors,
                    config.AvailableColorCount,
                    maxPairPerColor,
                    config.MatchRequireCount,
                    config.RoundCount
                );
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        #endregion

        #region 补齐剩余 Tile（保证不超过上限）

        FillRemainingTiles(colorTileCounts, ref remainingTiles, maxPairPerColor, config.MatchRequireCount);

        #endregion

        #region 随机映射到具体花色编号

        return AssignColorIndices(colorTileCounts, config.AvailableColorCount);

        #endregion
    }
   
    private static void ValidateConfig(DistributeConfig config)
    {
        if (config.TotalCount <= 0)
            throw new ArgumentException($"TotalCount must be positive: {config.TotalCount}");

        if (config.AvailableColorCount <= 0)
            throw new ArgumentException($"AvailableColorCount must be positive: {config.AvailableColorCount}");

        if (config.MatchRequireCount <= 0)
            throw new ArgumentException($"MatchRequireCount must be positive: {config.MatchRequireCount}");

        if (config.TotalCount % config.MatchRequireCount != 0)
            throw new ArgumentException($"TotalCount must be multiple of MatchRequireCount ({config.TotalCount} % {config.MatchRequireCount} != 0)");

        if (config.SpecialColorCountArray is not null)
        {
            foreach (int c in config.SpecialColorCountArray)
            {
                if (c % config.MatchRequireCount != 0)
                    throw new ArgumentException("SpecialColorCount must be multiple of MatchRequireCount");
            }

            if (config.SpecialColorCountArray.Length >= config.AvailableColorCount)
                throw new ArgumentException("Too many special colors");
        }
    }
    
    private static int ComputeMaxPairPerColor(
        int remainingTiles,
        int remainingColors,
        int matchRequireCount,
        int normalMaxPair)
    {
        int totalPairs = remainingTiles / matchRequireCount;

        // 向上取整：保证容量一定能装下
        int theoreticalMax =
            (totalPairs + remainingColors - 1) / remainingColors;

        return Math.Max(theoreticalMax, normalMaxPair);
    }
    
    private static void DistributeRandom(
        List<int> list,
        ref int remainingTiles,
        int remainingColors,
        int totalColors,
        int maxPair,
        int matchRequireCount)
    {
        while (remainingTiles > 0 && list.Count < totalColors)
        {
            int pair = Math.Min(
                Random.Shared.Next(1, maxPair + 1),
                remainingTiles / matchRequireCount
            );

            list.Add(pair * matchRequireCount);
            remainingTiles -= pair * matchRequireCount;
        }
    }

    private static void DistributeMin(
        List<int> list,
        ref int remainingTiles,
        int remainingColors,
        int totalColors,
        int matchRequireCount)
    {
        while (remainingTiles > 0 && list.Count < totalColors)
        {
            list.Add(1 * matchRequireCount);
            remainingTiles -= matchRequireCount;
        }
    }
    
    private static void DistributeMax(
        List<int> list,
        ref int remainingTiles,
        int remainingColors,
        int totalColors,
        int maxPair,
        int matchRequireCount)
    {
        while (remainingTiles > 0 && list.Count < totalColors)
        {
            int pair = Math.Min(
                maxPair,
                remainingTiles / matchRequireCount
            );

            list.Add(pair * matchRequireCount);
            remainingTiles -= pair * matchRequireCount;
        }
    }
    
    private static void DistributeSpecified(
        List<int> list,
        ref int remainingTiles,
        int remainingColors,
        int totalColors,
        int maxPair,
        int matchRequireCount,
        int round)
    {
        int maxRabbit = remainingTiles / (maxPair * matchRequireCount);
        int minRabbit = Math.Max(
            (remainingTiles - (maxPair - 1) * remainingColors * matchRequireCount) / matchRequireCount,
            0
        );

        int range = maxRabbit - minRabbit + 1;
        int rabbitCount = minRabbit + (round % range);

        // 先放“兔子”
        for (int i = 0; i < rabbitCount; i++)
        {
            list.Add(maxPair * matchRequireCount);
            remainingTiles -= maxPair * matchRequireCount;
        }

        // 剩余用“鸡”填
        while (remainingTiles > 0 && list.Count < totalColors)
        {
            int pair = Random.Shared.Next(1, maxPair);
            list.Add(pair * matchRequireCount);
            remainingTiles -= pair * matchRequireCount;
        }
    }
    
    //剩余补齐（不破坏上限）
    private static void FillRemainingTiles(
        List<int> list,
        ref int remainingTiles,
        int maxPair,
        int matchRequireCount)
    {
        List<int> availableColorIndex = [];
        while (remainingTiles > 0)
        {
            availableColorIndex.Clear();
            int maxColorCount = maxPair * matchRequireCount;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] >= maxColorCount) continue;
                availableColorIndex.Add(i);
            }


            int idx = availableColorIndex[Random.Shared.Next(availableColorIndex.Count)];
            list[idx] += matchRequireCount;
            remainingTiles -= matchRequireCount;
        }
    }
    
    private static Dictionary<int, int> AssignColorIndices(
        List<int> tileCounts,
        int totalColorCount)
    {
        int[] colors = Enumerable.Range(1, totalColorCount).ToArray();
        Random.Shared.Shuffle(colors);

        var dict = new Dictionary<int, int>();
        for (int i = 0; i < tileCounts.Count; i++)
            dict[colors[i]] = tileCounts[i];

        return dict;
    }
    
    
    // ========== 辅助打印helper
    
    public static string FormattingColors(Dictionary<int, int> colors)
    {
        if (colors == null || colors.Count == 0)
            return string.Empty;

        return string.Join(
            ", ",
            colors
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}:{kv.Value}")
        );
    }
    
    public static string FormattingColors(
        Dictionary<int, int> colors,
        int matchRequireCount)
    {
        if (colors == null || colors.Count == 0)
            return string.Empty;

        return string.Join(
            ", ",
            colors
                .OrderBy(kv => kv.Key)
                .Select(kv =>
                {
                    int pair = kv.Value / matchRequireCount;
                    return $"{kv.Key}:{pair}p";
                })
        );
    }
    
    public static string FormattingColorsByPairLines(
        Dictionary<int, int> colors,
        int matchRequireCount)
    {
        if (colors == null || colors.Count == 0)
            return string.Empty;

        var lines = colors
            .GroupBy(kv => kv.Value / matchRequireCount) // p
            .OrderByDescending(g => g.Key)               // 大 p 在前
            .Select(g =>
            {
                var colorList = string.Join(
                    ", ",
                    g.Select(x => x.Key).OrderBy(x => x)
                );

                return $"{g.Key}p → [{colorList}]";
            });

        return string.Join(Environment.NewLine, lines);
    }
}