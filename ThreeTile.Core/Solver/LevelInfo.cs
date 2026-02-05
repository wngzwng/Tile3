namespace ThreeTile.Core.Solver;

/// <summary>
/// 解题器会输出的关卡信息
/// </summary>
/// <param name="DifficultyScore">关卡难度分</param>
/// <param name="FailRate">失败率</param>
/// <param name="FailPosition">失败位置</param>
/// <param name="MinFailPosition">最小失败位置</param>
/// <param name="MaxFailPosition">最大失败位置</param>
/// <param name="AverageLegalMovesCountAtSuccessPath">成功路径中，平均每一步的可用移动数量的平均值</param>
/// <param name="AverageLegalMovesCountAtFailurePath">失败路径中，平均每一步可用移动数量的平均值</param>
/// <param name="LegalMovesCountAtInitial">初始可用移动数量</param>
/// <param name="UnlockingMahjongsCountAtInitial">初始解锁麻将的数量</param>
/// <param name="AverageMoveZDistance">平均每一步消除的 Z 坐标差值</param>
/// <param name="UsefulMoveRateAtSuccessPath">成功路径中，产生新消除的移动的数量占比的平均值</param>
/// <param name="UsefulMoveRateAtFailurePath">失败路径中，产生新消除的移动的数量占比的平均值</param>
public sealed record LevelInfo
(
    double DifficultyScore,
    double FailRate,
    double FailPosition,
    double MinFailPosition,
    double MaxFailPosition,
    double AverageLegalMovesCountAtSuccessPath,
    double AverageLegalMovesCountAtFailurePath,
    double StaggerFactorAtInitial,
    int LegalMovesCountAtInitial,
    int UnlockingMahjongsCountAtInitial,
    double AverageMoveZDistance,
    double AverageMaxStepDifficultyAtSuccessPath,
    double UsefulMoveRateAtSuccessPath,
    double UsefulMoveRateAtFailurePath
);