// using ThreeTile.Core.Core;
//
// namespace ThreeTile.Core.Designer;
//
// /**
// 1. 染色方式
// 第一种： 颜色序列生成 与 tile 着色过程分离
//
//
// tile 数量， 花色种类分配表（合法配对数)
// 构造一个颜色序列, 是一个固定长度的卡槽可以通过，其中当卡槽中某个颜色达到消除数量时，消除，容量增加
//  */
//
//
// /// <summary>
// /// 出题器，目前的规则是模拟出一条消除路径，并将每次的匹配对染上相同的颜色
// /// </summary>
// /// <param name="modelLevel">传入用于出题的题目 (花色不必要)</param>
// /// <param name="specialColorCountArray">特殊花色序列，默认为一个 8</param>
// public sealed class Tile3ColorFiller(
//     Level modelLevel,
//     Tile3ColorFiller.ColorMode colorMode,
//     int roundCount, // 🐔🐰同笼问题中，🐰的数量
//     int[] specialColorCountArray = null
// )
// {
//     /// 正在操作的关卡
//     private Level _designingLevel = modelLevel.Clone();
//
//     /// 花色数量的模式
//     public enum ColorMode
//     {
//         Random = 0, // 从当前的可用数量中随机选择
//         Max = 1, // 尽量从可用花色数量中，选择更大的花色数量
//         Min = 2, // 尽量从可用花色数量中，选择更小的花色数量
//         Specified = 3
//     }
//
//     private static readonly Random Random = new();
//
//     /// 本次出题当前使用的花色和数量的字典
//     private Dictionary<int, int> _colorIndexCountDict = [];
//
//     /// 特殊花色的字典，如果有什么花色必须要有指定数量的 (例如当前的默认值是排行榜的收集牌为 8 张则设定在这个数组里)
//     private readonly int[] _specialColorCountArray = specialColorCountArray ?? [];
//     
//         /// <summary>
//     /// 正式出题流程，这里正常出题的情况下会将 _newLevel 根据传入的 modelLevel 重新染色为一个同模型的新关卡
//     /// </summary>
//     public void Design()
//     {
//     Retry:
//         // 首先设定这一关的颜色数量
//         InitializeColorCountDict();
//         // 开始循环染色，原理为每一步根据解锁中的麻将来随机选择一对模拟真实路线
//         while (_designingLevel.Pasture.Tiles.Count > 0)
//         {
//             // 1. 随机从解锁牌中选取几个(1, paircount, abaliecount)
//             
//             // 2. 结
//             
//             // // 先选择位置
//             // var modelMahjong = RandomChooseMahjongPairPositions();
//             // // 若位置不是一对，说明没有足够的可用麻将位置，此时直接重新开始出题
//             // if (modelMahjong is not { Length: 2 })
//             // {
//             //     Clear();
//             //     goto Retry;
//             // }
//             //
//             // var move = new MatchMove(modelMahjong[0], modelMahjong[1]);
//             // var color = RandomChooseColor();
//             // 填色
//             FillPositionWithColor(move, color);
//         }
//     }
//
//     /// <summary>
//     /// 重新出题的情形下，需要将所有当前的属性 / 字段初始化的方法
//     /// </summary>
//     private void Clear()
//     {
//         _designingLevel = modelLevel.Clone();
//         _colorIndexCountDict = [];
//         NewLevel.CLear();
//     }
//
//     /// <summary>
//     /// 看起来是染色方法，其实是在原模型上删除，在新模型上添加对应位置和传入花色的麻将，同时将麻将花色数量减一
//     /// </summary>
//     private void FillPositionWithColor(MatchMove matchMove, int color)
//     {
//         var mahjong1 = matchMove.Mahjong1;
//         var mahjong2 = matchMove.Mahjong2;
//         // 如果剩余位置里没有对应麻将直接返回
//         if (!_designingLevel.MakeMove(matchMove, twoDirections: true)) return;
//         // 加入新麻将，并将这个颜色的数量减 1
//         NewLevel.AddMahjong(new Mahjong(mahjong1.Index, mahjong1.Position, color));
//         NewLevel.AddMahjong(new Mahjong(mahjong2.Index, mahjong2.Position, color));
//         _colorIndexCountDict[color] -= 2;
//     }
//
//     /// <summary>
//     /// 找到现在可以进行染色的麻将
//     /// </summary>
//     private Mahjong[] GetAvailableMahjongs()
//         => _designingLevel.UnlockingMahjongs.ToArray();
//
//     /// <summary>
//     /// 选出一对可以用于染色的麻将，目前为随机选择
//     /// </summary>
//     private Mahjong[] RandomChooseMahjongPairPositions()
//     {
//         var availableMahjongs = GetAvailableMahjongs();
//         // 如果可用的麻将不到两个，说明已经不可以构成消除路径，返回 null
//         if (availableMahjongs.Length < 2) return null;
//
//         Random.Shuffle(availableMahjongs);
//         return [availableMahjongs[0], availableMahjongs[1]];
//     }
//
//     /// <summary>
//     /// 对于颜色的序列进行初始化
//     /// </summary>
//     private void InitializeColorCountDict()
//     {
//         _colorIndexCountDict.Clear();
//         var positionCount = _designingLevel.Mahjongs.Count;
//         List<int> colorCountList = [];
//
//         var availableColorCount = LevelCore.MaxLevelColorIndex;
//
//         foreach (var colorCount in _specialColorCountArray)
//         {
//             // 先放入排行榜数量的牌
//             colorCountList.Add(colorCount);
//             positionCount -= colorCount;
//             availableColorCount -= 1;
//         }
//
//         /*
//          * 根据麻将牌的总张数，计算关卡内最大张数的最小对数，至少需要为 2
//          * 注意是对数，因此需要先除以 2
//          */
//         var maxColorPairCount =
//             Math.Max(
//                 (positionCount / 2 + availableColorCount - 1) / availableColorCount, // 这样处理是为了向上取整，这里 -1 是因为上一步使用了一个颜色
//                 LevelCore.NormalMaxColorCount / 2
//                 );
//
//         #region 第二步：选择花色的麻将数量，填充够麻将需求数量为止
//
//         switch (colorMode)
//         {
//             case ColorMode.Random:
//             {
//                 // 先随机分配，此时出循环可能存在 36 个花色已经分配完，但是麻将的数量依然不足
//                 while (positionCount > 0 && colorCountList.Count < LevelCore.MaxLevelColorIndex)
//                 {
//                     var currentColorPairCount = Math.Min(
//                         Random.Next(0, maxColorPairCount) + 1,
//                         positionCount / 2
//                     );
//                     colorCountList.Add(currentColorPairCount * 2);
//                     positionCount -= currentColorPairCount * 2;
//                 }
//                 break;
//             }
//             case ColorMode.Max: 
//             {
//                 // 尽量填充最大花色数量
//                 while (positionCount > 0 && colorCountList.Count < LevelCore.MaxLevelColorIndex)
//                 {
//                     var currentColorPairCount = Math.Min(
//                         maxColorPairCount,
//                         positionCount / 2
//                     );
//                     colorCountList.Add(currentColorPairCount * 2);
//                     positionCount -= currentColorPairCount * 2;
//                 }
//                 break;
//             }
//             case ColorMode.Min: 
//             {
//                 // 尽量填充更多的花色，从 2 开始铺满
//                 while (positionCount > 0 && colorCountList.Count < LevelCore.MaxLevelColorIndex)
//                 {
//                     // 直接只填充一对
//                     colorCountList.Add(1 * 2);
//                     positionCount -= 1 * 2;
//                 }
//                 break;
//             }
//             case ColorMode.Specified: // 指定鸡兔同笼问题的兔子数量，进行填充
//             {
//                 var maxRabbitCount = positionCount / (maxColorPairCount * 2); // 至多有这么多只兔
//                 var minRabbitCount = Math.Max((positionCount -
//                                                (maxColorPairCount - 1) * LevelCore.MaxLevelColorIndex * 2)
//                                               / 2, 0); // 受 MaxColorIndex 限制，至少有这么多只兔
//                 var roundLength = maxRabbitCount - minRabbitCount + 1;
//                 
//                 var currentRound = roundCount % roundLength;
//                 
//                 for (var i = 0; i < currentRound + minRabbitCount; i++)
//                 {
//                     colorCountList.Add(maxColorPairCount * 2);
//                     positionCount -= maxColorPairCount * 2;
//                 }
//
//                 while (positionCount > 0 && colorCountList.Count < LevelCore.MaxLevelColorIndex)
//                 {
//                     var chickFootCount = Random.Next(1, maxColorPairCount);
//                     colorCountList.Add(chickFootCount * 2);
//                     positionCount -= chickFootCount * 2;
//                 }
//                 break;
//             }
//             default:
//                 throw new ArgumentOutOfRangeException(nameof(colorMode), colorMode, null);
//         }
//
//         // 如果出现了上条注释说明的情况，此时继续对于 36 个花色的数量进行补齐
//         while (positionCount > 0)
//         {
//             List<int> availableColorIndex = [];
//             for (var i = 0; i < colorCountList.Count; i++)
//             {
//                 // 如果已经达到了最大数量则跳过，否则将这个颜色序号加入可用序号中
//                 if (colorCountList[i] >= maxColorPairCount * 2) continue;
//                 availableColorIndex.Add(i);
//             }
//             // 随机挑选一个可用的颜色序号再补一对
//             colorCountList[availableColorIndex[Random.Next(availableColorIndex.Count)]] += 2;
//             positionCount -= 2;
//         }
//
//         #endregion
//
//         #region 第三步：进行颜色的打乱
//
//         var wholeColorArray = Enumerable.Range(1, LevelCore.MaxLevelColorIndex).ToArray();
//         Random.Shuffle(wholeColorArray);
//
//         for (var i = 0; i < colorCountList.Count; i++)
//             _colorIndexCountDict.Add(wholeColorArray[i], colorCountList[i]);
//
//         #endregion
//     }
//
//     /// <summary>
//     /// 随机从当前还有剩余数量的一个花色中挑选出一个花色 Index
//     /// </summary>
//     private int RandomChooseColor()
//     {
//         var availableColorArray = _colorIndexCountDict
//             .Where(kvp => kvp.Value > 0)
//             .Select(kvp => kvp.Key)
//             .ToArray();
//         return availableColorArray[Random.Next(availableColorArray.Length)];
//     }
// }