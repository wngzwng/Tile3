namespace ThreeTile.Core.Solver;

/// <summary>
/// 单次解题的信息
/// </summary>
/// <param name="DifficultyScore">难度分</param>
/// <param name="Failed">是否失败</param>
/// <param name="AverageLegalMovesCount">平均每一步的合法移动数量</param>
/// <param name="AverageMovesZDistanceCount">平均每一步的 Z 距离</param>
/// <param name="UsefulMovesCount">有效移动数量</param>
public sealed record SolveInfo
(
    int MovesCount,
    double DifficultyScore,
    double MaxStepDifficulty,
    bool Failed,
    double AverageLegalMovesCount,
    double AverageMovesZDistanceCount,
    double UsefulMovesRatio
);