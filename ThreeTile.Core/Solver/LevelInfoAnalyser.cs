// using ThreeTile.Core.Core;
// using ThreeTile.Core.ExtensionTools;
//
// namespace ThreeTile.Core.Solver;
//
// public sealed class LevelInfoAnalyser(Level analysingLevel, int maxSolveTime = 1000, int maxSuccessTime = 200)
// {
//     private readonly int _size = GetLevelSize(analysingLevel);
//     private static readonly Random Random = new();
//
//     public LevelInfo Solve()
//     {
//         analysingLevel.RefreshUnlockingMahjongsAndVisibleMahjongs();
//         analysingLevel.GetLegalMoves(twoDirections: true);
//         
//         var staggerFactor = GetLevelStaggerFactor();
//
//         // 解题计数
//         var solveCount = 0;
//         var failCount = 0;
//         var pairCount = analysingLevel.Pasture.Tiles.Count; // 原关卡中麻将的对数
//
//         #region 解题核心数据的初始化
//
//         var difficultySum = .0; // 难度分总和
//         var failMovesCountSum = 0; // 失败位置统计
//         var minFailPosition = int.MaxValue;
//         var maxFailPosition = int.MinValue;
//         var avgLegalMovesSumAtSuccessPath = .0; // 胜利路径平均可用移动数统计
//         var avgLegalMovesSumAtFailurePath = .0; // 失败路径平均可用移动数统计
//         var initialLegalMovesCount = analysingLevel.LegalMoves.Length / 2; // 初始可用移动数量
//         var initialUnlockingMahjongsCount = analysingLevel.UnlockingMahjongs.Count; // 初始解锁麻将数量
//         var avgMoveZDistanceSum = .0;
//         var usefulMoveRatioAtSuccessPathSum = .0;
//         var usefulMoveRatioAtFailurePathSum = .0;
//
//         #endregion
//
//         // 初始的麻将面积计数，用于难度分的加权计算
//         var mahjongAreaCount = GetInitialMahjongAreaCount();
//
//         while (solveCount < maxSolveTime && solveCount - failCount < maxSuccessTime)
//         {
//             var lastSolveInfo = SolveOneTime(mahjongAreaCount);
//
//             #region 失败的话统计相关信息
//
//             if (lastSolveInfo.Failed)
//             {
//                 failMovesCountSum += lastSolveInfo.MovesCount;
//                 failCount++;
//                 minFailPosition = Math.Min(minFailPosition, lastSolveInfo.MovesCount);
//                 maxFailPosition = Math.Max(maxFailPosition, lastSolveInfo.MovesCount);
//                 avgLegalMovesSumAtFailurePath += lastSolveInfo.AverageLegalMovesCount;
//                 avgMoveZDistanceSum += lastSolveInfo.AverageMovesZDistanceCount;
//                 usefulMoveRatioAtFailurePathSum += lastSolveInfo.UsefulMovesRatio;
//             }
//
//             #endregion
//
//             #region 胜利的话统计相关信息
//
//             else
//             {
//                 difficultySum += lastSolveInfo.DifficultyScore;
//                 avgLegalMovesSumAtSuccessPath += lastSolveInfo.AverageLegalMovesCount;
//                 avgMoveZDistanceSum += lastSolveInfo.AverageMovesZDistanceCount;
//                 usefulMoveRatioAtSuccessPathSum += lastSolveInfo.UsefulMovesRatio;
//             }
//
//             #endregion
//
//             // 更新常规信息
//             solveCount++;
//         }
//
//         var successCount = solveCount - failCount;
//         return new LevelInfo(
//             StaggerFactorAtInitial: staggerFactor,
//             AverageMaxStepDifficultyAtSuccessPath: .0,
//             DifficultyScore: difficultySum / successCount,
//             FailRate: (double)failCount / (solveCount == 0 ? 1 : solveCount),
//             FailPosition: (double)failMovesCountSum / pairCount / failCount,
//             MinFailPosition: failMovesCountSum == 0 ? 0 : (double)minFailPosition / pairCount,
//             MaxFailPosition: failMovesCountSum == 0 ? 0 : (double)maxFailPosition / pairCount,
//             AverageLegalMovesCountAtSuccessPath: avgLegalMovesSumAtSuccessPath / successCount,
//             AverageLegalMovesCountAtFailurePath: avgLegalMovesSumAtFailurePath / failCount,
//             LegalMovesCountAtInitial: initialLegalMovesCount / 2,
//             UnlockingMahjongsCountAtInitial: initialUnlockingMahjongsCount,
//             AverageMoveZDistance: avgMoveZDistanceSum / solveCount,
//             UsefulMoveRateAtSuccessPath: usefulMoveRatioAtSuccessPathSum / successCount,
//             UsefulMoveRateAtFailurePath: usefulMoveRatioAtFailurePathSum / failCount
//             );
//     }
//
//     /// <summary>
//     /// 计算初始关卡中的俯视图下，包含麻将的平面坐标的面积 (例如只有一张麻将这个算出来就是 4)
//     /// </summary>
//     private int GetInitialMahjongAreaCount()
//     {
//         var mahjongAreaCount = 0;
//         
//         // 遍历每一个关卡的俯视平面坐标，如果在位置字典里有对应的坐标说明这个点上有麻将
//         for (var x = 0; x < _size.X + Vector3D.DefaultVolume.X; x++)
//             for (var y = 0; y < _size.Y + Vector3D.DefaultVolume.Y; y++)
//                 if (analysingLevel.PosMahjongDict.Any(kvp => kvp.Key.X == x && kvp.Key.Y == y))
//                     mahjongAreaCount++;
//
//         return mahjongAreaCount;
//     }
//
//     /// <summary>
//     /// 计算关卡错落程度的方法
//     /// <br/> -当前的错落程度是指每个麻将平均压住了几个麻将
//     /// </summary>
//     public double GetLevelStaggerFactor()
//     {
//         // 找到所有不在底层的麻将
//         var bottomCount = 0; // 在底层的计数
//         foreach (var m in analysingLevel.Mahjongs)
//         {
//             // 由于麻将是从下到上排序，所以可以提前剪枝
//             if (m.Position.Z != 0) break;
//             bottomCount++;
//         }
//
//         var mahjongsNotAtBottom = new Mahjong[analysingLevel.Mahjongs.Count - bottomCount];
//         for (var i = 0; i < mahjongsNotAtBottom.Length; i++)
//             mahjongsNotAtBottom[i] = analysingLevel.Mahjongs[i + bottomCount];
//
//         var downCount = 0;
//         foreach (var m in mahjongsNotAtBottom)
//             downCount += analysingLevel.GetAllMahjongsWhichContainsPositionInArray(
//                 m.Position.GetPositionDownNeighbourPositions()).Length;
//
//         return (double)downCount / mahjongsNotAtBottom.Length;
//     }
//
//     /// <summary>
//     /// 解题一次的方法，返回这一次解题路径的一些信息 (难度分、是否失败、可用移动的总和)
//     /// </summary>
//     private SolveInfo SolveOneTime(int mahjongAreaCount)
//     {
//         var level = analysingLevel.Clone();
//         level.RefreshUnlockingMahjongsAndVisibleMahjongs();
//         level.GetLegalMoves(twoDirections: true);
//         
//         // 记录一下这次解题路径的难度值
//         var pathDifficulty = new double[analysingLevel.Mahjongs.Count / 2];
//         
//         // 声明一些必要的参数，目前有难度分、是否失败、可用步数的统计
//         var difficultyScore = .0;
//         var initLegalMoveCount = level.LegalMoves.Length / 2;
//         var moveZIndexDistanceSum = 0;
//         var movesCount = 0;
//         var usefulMovesCount = 0;
//         var isUsefulMovesRatioDone = false; // 这个数字只算一次
//         var usefulMoveRatio = 0d;
//         var maxStepDifficultyScore = double.MinValue;
//         
//         var failed = false;
//
//         while (level.Mahjongs.Count > 0)
//         {
//             var legalMovesCount = level.LegalMoves.Length;
//             
//             var move = ChooseMove(level, mahjongAreaCount, out var score);
//
//             // 如果 move 为 null，那么说明用户失败了
//             if (move == null)
//             {
//                 failed = true;
//                 break;
//             }
//
//             // 计算这个移动执行后，不解锁新牌的情况下，合法移动理论上的减少数量
//             var moveColor = move.Mahjong1.Color;
//             var moveColorUnlockingMahjongCount = 0;
//             foreach (var m in level.UnlockingMahjongs)
//                 if (m.Color == moveColor) moveColorUnlockingMahjongCount++;
//             var expectLegalMoveRemoveCount
//                 = moveColorUnlockingMahjongCount * (moveColorUnlockingMahjongCount - 1) -
//                   (moveColorUnlockingMahjongCount - 2) * (moveColorUnlockingMahjongCount - 3);
//
//             // 执行移动
//             level.MakeMove(move, twoDirections: true);
//
//             #region 更新这次解题的相关信息
//
//             // 更新难度分之和、可用移动之和
//             difficultyScore += score;
//             pathDifficulty[movesCount++] = score;
//             initLegalMoveCount += level.LegalMoves.Length;
//             maxStepDifficultyScore = Math.Max(maxStepDifficultyScore, score);
//             
//             // 检查新的合法移动，是否存在原来的合法移动中不存在的移动，若有则说明刚刚的移动是 UsefulMoves，计数 ++
//             if (!isUsefulMovesRatioDone)
//             {
//                 if (legalMovesCount - level.LegalMoves.Length != expectLegalMoveRemoveCount)
//                     usefulMovesCount++;
//                 if (level.UnlockingMahjongs.Count == level.Mahjongs.Count)
//                 {
//                     usefulMoveRatio = (double)usefulMovesCount / movesCount;
//                     isUsefulMovesRatioDone = true;
//                 }
//             }
//             
//             // 增加移动的 Z 距离差值
//             moveZIndexDistanceSum += Math.Abs(move.Mahjong1.Position.Z - move.Mahjong2.Position.Z);
//
//             #endregion
//
//             #region 看一下麻将的俯视图面积需要如何更新
//
//             // 先合并两个麻将的平面坐标
//             HashSet<(int X, int Y)> checkingPoints = [];
//             // 第一个麻将的平面坐标
//             for (var x = move.Mahjong1.Position.X; x < move.Mahjong1.Position.X + move.Mahjong1.Volume.X; x++)
//                 for (var y = move.Mahjong1.Position.Y; y < move.Mahjong1.Position.Y + move.Mahjong1.Volume.Y; y++)
//                     checkingPoints.Add((x, y));
//             // 第二个麻将的平面坐标
//             for (var x = move.Mahjong2.Position.X; x < move.Mahjong2.Position.X + move.Mahjong2.Volume.X; x++)
//                 for (var y = move.Mahjong2.Position.Y; y < move.Mahjong2.Position.Y + move.Mahjong2.Volume.Y; y++)
//                     checkingPoints.Add((x, y));
//             // 然后检查去重，对于这些点坐标每有一个点在字典里找不到对应的位置坐标，那么就把对应的麻将区域计数去除
//             foreach (var p in checkingPoints)
//                 if (level.PosMahjongDict.All(kvp => kvp.Key.X != p.X || kvp.Key.Y != p.Y))
//                     mahjongAreaCount--;
//
//             #endregion
//         }
//
//         return new SolveInfo(
//             MovesCount: movesCount,
//             DifficultyScore: difficultyScore,
//             MaxStepDifficulty: maxStepDifficultyScore,
//             Failed: failed,
//             AverageLegalMovesCount: (double)initLegalMoveCount / (movesCount == 0 ? 1 : movesCount),
//             AverageMovesZDistanceCount: (double)moveZIndexDistanceSum / (movesCount == 0 ? 1 : movesCount),
//             UsefulMovesRatio: isUsefulMovesRatioDone ? usefulMoveRatio : (double)usefulMovesCount / movesCount
//         );
//     }
//
//     /// <summary>
//     /// 计算关卡的尺寸
//     /// </summary>
//     /// <param name="level"></param>
//     /// <returns></returns>
//     private static int GetLevelSize(Level level)
//     {
//         var maxX = int.MinValue;
//         var maxY = int.MinValue;
//         var maxZ = int.MinValue;
//
//         foreach (var mahjong in level.Pasture.Tiles)
//         {
//             maxX = Math.Max(mahjong.TilePositionIndex.X() + mahjong.Volume.X(), maxX);
//             maxY = Math.Max(mahjong.TilePositionIndex.Y() + mahjong.Volume.Y(), maxY);
//             maxZ = Math.Max(mahjong.TilePositionIndex.Z() + mahjong.Volume.Z(), maxZ);
//         }
//         
//         return (maxX, maxY, maxZ).PackXyz();
//     }
//
//     /// <summary>
//     /// 计算每个移动对应的分数，然后使用 SoftMax 计算对应的权重
//     /// </summary>
//     /// <returns>根据权重命中的移动，如果没有可用移动则返回 null</returns>
//     private MatchMove ChooseMove(LevelCore designingLevel, int mahjongAreaCount, out double difficultyScore)
//     {
//         var legalMoves = designingLevel.LegalMoves;
//         difficultyScore = 0;
//         if (legalMoves.Length == 0) return null;
//
//         // 计算 legalMoves 里面每一个的难度分
//         var scoreArray = new double[legalMoves.Length];
//         for (var i = 0; i < legalMoves.Length; i++)
//             scoreArray[i] = GetMoveDifficultyScore(legalMoves[i], designingLevel);
//         
//         var weightArray = scoreArray.GetSoftMaxWeightArray();
//         var targetWeight = Random.NextDouble() * weightArray[^1];
//
//         // 返回权重数组里面比随机数大的第一个，即为这个随机数命中的移动区间
//         var index = 0;
//         for (var i = 0; i < weightArray.Length; i++)
//         {
//             if (weightArray[i] < targetWeight) continue;
//             index = i;
//             break;
//         }
//
//         difficultyScore = scoreArray.GetMixedDifficulty(
//             (double)mahjongAreaCount / 
//             (Vector3D.DefaultVolume.X * Vector3D.DefaultVolume.Y));
//
//         return legalMoves[index];
//     }
//
//     /// <summary>
//     /// 计算移动的难度分
//     /// </summary>
//     private double GetMoveDifficultyScore(MatchMove matchMove, LevelCore designingLevel) =>
//         .5 * GetMoveMahjongsDistance(matchMove) +
//         .5 * GetMoveActionDistance(matchMove, designingLevel) -
//         .3 * GetMoveZIndexSum(matchMove);
//
//     /// <summary>
//     /// 计算 matchMove 内两个麻将的距离分的方法
//     /// </summary>
//     private static double GetMoveMahjongsDistance(MatchMove matchMove)
//         => Vector3D.GetCoordinateDistance(matchMove.Mahjong1.Position, matchMove.Mahjong2.Position);
//
//     /// <summary>
//     /// 这个函数的出发点是计算这一组移动相对于上一组移动的距离分数
//     /// <br/> **目前的算法是计算上一组移动的终点麻将，距离本次移动的起点麻将的 2 倍和终点麻将的 1 倍
//     /// </summary>
//     private double GetMoveActionDistance(MatchMove matchMove, LevelCore designingLevel)
//     {
//         // 如果移动列表数量为 0，说明这是第一个移动，这个时候就用屏幕的中心点作为起始坐标 (_size / 2)
//         var lastMahjongPosition = designingLevel.MoveList.Count > 0
//             ? designingLevel.MoveList[^1].Mahjong2.Position
//             : new Vector3D(_size.X / 2, _size.Y / 2, _size.Z / 2);
//
//         return 2 * Vector3D.GetCoordinateDistance(lastMahjongPosition, matchMove.Mahjong1.Position) +
//                Vector3D.GetCoordinateDistance(lastMahjongPosition, matchMove.Mahjong2.Position);
//     }
//
//     /// <summary>
//     /// 获取 matchMove 两个麻将层数的和
//     /// </summary>
//     private static double GetMoveZIndexSum(MatchMove matchMove)
//         => matchMove.Mahjong1.Position.Z + matchMove.Mahjong2.Position.Z;
// }