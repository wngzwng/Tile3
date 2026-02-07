### Tile(id, position, volume?)
- id 唯一标识
- color 花色
- zone 当前所在区域
- // 集结区相关
- stagingAreaIndex 集结区逻辑编号
- // 牧场相关
- pasturePostion 牧场位置
- volume 作占空间
- Coordinates 空间所占坐标
- 是否可见
- 是否解锁

### Level
- List[Tile] 所有的Tile
- // 访问方式
- Dict<index, Tile> IndexToDict
- 给出当前可用行为 (参考Pasture 以及 StagingArea) 
### Pasture
- Parent Level
- List[tileIndex] // tileId集合
- Dict[position, tileIndex]  空间坐标 -> tileId
- VisiableTileIndexes   可见
- UnlockingTileIndexes  解锁
- Add(tilezon -> Pasture) 增量更新
- Remove(更新必要信息) // 增量更新
- 更新可见和解锁集合以及tile对应的标志（只提供一个统一的更新方法）全量更新
- 更新可见和解锁集合

### StagingArea
- Parent Level
- List[tileIndex] 集结区的TileIndex
- Dict<color, int> colorCounter
- logicSlotId = 0; 
- ADD (从牧场来的需要logicSlotId++) 从围栏的不用 外部才知晓
- Remove(回到牧场的需要logicSlotId--) 从围栏的不同 外部才知晓

### Corral



### Move