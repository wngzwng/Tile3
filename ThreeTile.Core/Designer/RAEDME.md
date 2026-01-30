### 出题器
#### 模型着色部分
1. 根据 Tile 数量和颜色类型 给出分配表  鸡兔同笼问题
2. 根据分配表得到一条合法的颜色序列      批量调度问题
3. 模拟玩家选择 Tile 行为, 结合颜色序列给选取的 Tile 着色

#### 着色策略
第一种：最大颜色并发数 maxConcurrentTypes, 从颜色种类出发
```C#
int slotMaxCapacity = 7;
int matchAllowCount = 3;
int maxConcurrentTypes = (slotMaxCapacity - 1) / (matchAllowCount - 1);  // 3
```
- 如果 < maxConcurrentTypes
  - 颜色选择范围:为所有剩余的颜色， 数量：1
- \>= maxConcurrentTypes
  - 颜色选择范围: 卡槽内的颜色种类，数量：1

第二种：最大容量deadline， dangerCapacityLine, 从卡槽容量出发
```C#
int slotMaxCapacity = 7;
int matchAllowCount = 3;
int dangerCapacityLine = slotMaxCapacity - (matchAllowCount - 1);  // 5
```
- 如果 < dangerCapacityLine
    - 颜色选择范围:为所有剩余的颜色， 数量：1
- \>= dangerCapacityLine
    - 颜色选择范围: 卡槽内的颜色种类，数量：matchAllowCount - slots[selectColor]
