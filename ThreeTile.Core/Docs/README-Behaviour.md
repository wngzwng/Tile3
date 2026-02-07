#### 行为分类
##### 消除行为
- 简单： 当前盘面下，某花色的可选棋子数量和序列内该花色的棋子数量之和 >= 3, 则这几个可选棋子视为直接可消除的操作
> 这个Ok，都是可选棋子的组成的行为

- 困难：当前盘面下，有同花色的几个棋子
> 可选 + 可见 棋子组成的消除
- 其中有至少一个棋子为可选棋子
- 剩余棋子均为可见棋子，且他们的锁定棋子仅有组（当前消除组）内其他的棋子， 及不会引入其他的成本
- 记录可见棋子的数量

##### 翻牌行为
没有消除行为的花色，玩家将一张这样的牌放入 Sequence，则视为翻牌行为


主要消除行为的逻辑
卡槽颜色分类 color：count   SlotMap[color]
卡槽可用容量 AvailableCapacity
当前盘面的原始可选组：  OriginSelectableGroup
当前盘面可选花色分类 color：count  OriginSelectableMap[color]
当前盘面的原始可见组：  VisibleGroup
颜色匹配消除数：     MatchClearCount

当前花色消除需要数: clearNeedCount[color] = MatchClearCount - SlotMap[color]
剪枝： 过滤 clearNeedCount > AvailableCapacity 的花色
对同一个花色分桶。 color: List<Tile>  selectableMap[color]

1. 简单消除
    - 条件： OriginSelectableMap[color] >= clearNeedCount[color]
    - 处理： C(selectableMap[color], clearNeedCount[color])
2. 困难消除
两个步骤： 展开 和 选取
三个组：
- F(Fixed) 固定组: 已确认纳入行为的棋子
- S(Selectable) 当前可直接选择的棋子（历史可选，包含上一次展开得到的同色可选棋子）
- E(Expanded) 展开一个棋子后得到的同色可选棋子，展开的棋子为固定组的最后一颗棋子

选取规则： 固定组全部，展开组至少一个，其余用可选组补充
- 选取前提： FixedCount + ExpandedCount >= ClearNeedCount
1. 固定组全部 固定组数量。FixedCount  固定组选择数量： FixedPickCount = FixedCount
2. 展开组： 展开组数量：ExpandedCount 展开组选择数量： ExpandedPickCount 1 ~ (ClearNeedCount - FixedPickCount)
3. 可选组： 可选组数量 SelectableCount 可选组选择数量： SelectablePickCount: ClearNeedCount - FixedPickCount - ExpandedPickCount

展开规则：
- 展开前提：FixedCount + 1 < ClearNeedCount  不够才要展开
- 遍历展开组中的每个棋子
  - 筛选展开这个棋子得到的同色展开可选棋子，没有跳过，有下一步
  - 该展开棋子进入固定组，展开组其余棋子进入可选组，同色展开可选棋子为新的展开组
  - 记录这个新得到的三组

看这种情况
    五1      五2 五3
   五4五5
  五6
第一轮展开：
F： 五1   S： 五2 五3    E：五4五5
这时： Fcount + Scount >= ClearNeedCount, 但是还可以拓展，因为 五1 五4 五6 也是满足要求的困难消除行为
第二轮展开：
F: 五1 五4 S：五2 五3 五5 E: 五6
所以可以展开的前提条件是 FixedCount + 1 < ClearNeedCount  这个1其实从展开组至少选一个，不会没有，应为一个都没有的展开组就展不开