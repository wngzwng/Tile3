using System.Diagnostics;
using System.Text;

namespace ThreeTile.Core.Designer;

public static class Tile3SequenceBuilder
{
   public static string Build(
    IReadOnlyDictionary<char, int> colorCounts,
    int capacity,
    int k)
    {
        if (k < 2) throw new ArgumentException("K 必须 >= 2");
        if (capacity <= 0) throw new ArgumentException("capacity 必须 > 0");

        int maxActive = capacity / (k - 1);
        if (maxActive <= 0)
            throw new InvalidOperationException("capacity 太小，无法形成合法序列");

        var remaining = new Dictionary<char, int>();
        var slot = new Dictionary<char, int>();

        foreach (var (c, n) in colorCounts)
        {
            if (n < 0)
                throw new ArgumentException($"颜色 {c} 的数量非法");

            if (n % k != 0)
                throw new InvalidOperationException(
                    $"颜色 {c} 的数量 {n} 不是 K={k} 的倍数，无法完全消除");

            if (n == 0) continue;

            remaining[c] = n;
            slot[c] = 0;
        }

        var active = new Stack<char>();
        var result = new StringBuilder();
        int used = 0;

        bool HasRemaining() => remaining.Values.Any(v => v > 0);

        while (HasRemaining() || active.Count > 0)
        {
            // ① 完成栈顶
            if (active.Count > 0)
            {
                char top = active.Peek();
                if (slot[top] == k - 1 && remaining[top] > 0)
                {
                    Emit(top);
                    continue;
                }
            }

            // ② 激活新颜色（修复点在这里）
            if (active.Count < maxActive)
            {
                bool found = false;
                char next = '\0';

                foreach (var kv in remaining)
                {
                    if (kv.Value > 0 && slot[kv.Key] == 0)
                    {
                        next = kv.Key;
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    Emit(next);
                    continue;
                }
            }

            // ③ 推进已有 active
            if (active.Count > 0)
            {
                char c = active.Peek();
                if (remaining[c] > 0)
                {
                    Emit(c);
                    continue;
                }
            }

            throw new InvalidOperationException("进入不安全状态：无合法推进");
        }

        return result.ToString();

        // --------------------------

        void Emit(char c)
        {
            result.Append(c);
            remaining[c]--;
            used++;
            slot[c]++;

            if (slot[c] == 1)
                active.Push(c);

            if (slot[c] == k)
            {
                used -= k;
                slot[c] = 0;

                if (active.Peek() != c)
                    throw new InvalidOperationException("active 栈状态异常");

                active.Pop();
            }

            if (used > capacity)
                throw new InvalidOperationException("容量溢出（理论上不可能）");
        }
    }
   
   
   public static string BuildRandom(
    IReadOnlyDictionary<char, int> colorCounts,
    int capacity,
    int k,
    int? seed = null)
    {
        if (k < 2) throw new ArgumentException("K 必须 >= 2");
        if (capacity <= 0) throw new ArgumentException("capacity 必须 > 0");

        int maxActive = capacity / (k - 1);
        if (maxActive <= 0)
            throw new InvalidOperationException("capacity 太小，无法形成合法序列");

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        var remaining = new Dictionary<char, int>();
        var slot = new Dictionary<char, int>();

        foreach (var (c, n) in colorCounts)
        {
            if (n < 0)
                throw new ArgumentException($"颜色 {c} 的数量非法");

            if (n % k != 0)
                throw new InvalidOperationException(
                    $"颜色 {c} 的数量 {n} 不是 K={k} 的倍数");

            if (n == 0) continue;

            remaining[c] = n;
            slot[c] = 0;
        }

        var active = new List<char>();   // ⚠️ 为了随机，Stack → List
        var result = new StringBuilder();
        int used = 0;

        bool HasRemaining() => remaining.Values.Any(v => v > 0);

        while (HasRemaining() || active.Count > 0)
        {
            // ① 随机完成一个可完成颜色
            var finishables = active
                .Where(c => slot[c] == k - 1 && remaining[c] > 0)
                .ToList();

            if (finishables.Count > 0)
            {
                char c = finishables[rng.Next(finishables.Count)];
                Emit(c);
                continue;
            }

            // ② 随机激活一个新颜色
            if (active.Count < maxActive)
            {
                var candidates = remaining
                    .Where(p => p.Value > 0 && slot[p.Key] == 0)
                    .Select(p => p.Key)
                    .ToList();

                if (candidates.Count > 0)
                {
                    char c = candidates[rng.Next(candidates.Count)];
                    Emit(c);
                    continue;
                }
            }

            // ③ 随机推进一个 active
            var pushables = active
                .Where(c => remaining[c] > 0)
                .ToList();

            if (pushables.Count > 0)
            {
                char c = pushables[rng.Next(pushables.Count)];
                Emit(c);
                continue;
            }

            throw new InvalidOperationException("进入不安全状态（不应发生）");
        }

        return result.ToString();

        // --------------------------

        void Emit(char c)
        {
            result.Append(c);
            remaining[c]--;
            used++;
            slot[c]++;

            if (slot[c] == 1)
                active.Add(c);

            if (slot[c] == k)
            {
                used -= k;
                slot[c] = 0;
                active.Remove(c);
            }

            if (used >= capacity)
                throw new InvalidOperationException("容量溢出（理论上不可能）");
        }
    }
   
   public static string BuildSelf(
    IReadOnlyDictionary<char, int> colorCounts,
    int capacity,
    int k,
    int? seed = null)
    {
        // ─────────────────────────
        // 参数校验
        // ─────────────────────────
        if (k < 2)
            throw new ArgumentException("k 必须 >= 2");
        if (capacity < k)
            throw new ArgumentException("capacity 必须 >= k");
        foreach (var (c, n) in colorCounts)
            if (n % k != 0)
                throw new ArgumentException($"颜色 {c} 的数量 {n} 不是 k 的倍数");

        // ─────────────────────────
        // 核心状态
        // ─────────────────────────
        var remaining = new Dictionary<char, int>(colorCounts); // 尚未投放
        var slots     = new Dictionary<char, int>();            // 卡槽中未消除
        var active    = new HashSet<char>();                     // slots.Keys 的语义别名

        int used = 0;
        int left = remaining.Values.Sum();

        // 系统安全底线（避免死锁）
        int safetyLimit = (capacity - 1) / (k - 1);

        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var sb  = new StringBuilder();

        // ─────────────────────────
        // 主循环
        // ─────────────────────────
        while (left > 0)
        {
            char c = PickColor();
            Emit(c);
        }

        return sb.ToString();

        // ─────────────────────────
        // 核心动作
        // ─────────────────────────
        void Emit(char c)
        {
            sb.Append(c);

            remaining[c]--;
            if (remaining[c] == 0)
                remaining.Remove(c);

            // 入槽
            slots[c] = slots.GetValueOrDefault(c) + 1;
            active.Add(c);
            used++;
            left--;

            // 消除
            if (slots[c] == k)
            {
                slots.Remove(c);
                active.Remove(c);
                used -= k;
            }

            if (used >= capacity)
                throw new InvalidOperationException("进入死锁态（理论上不应发生）");
        }

        // ─────────────────────────
        // 选色策略（唯一策略入口）
        // ─────────────────────────
        char PickColor()
        {
            // ① 若还没触及安全上限：可以引入新颜色
            if (active.Count < safetyLimit)
            {
                var pool = remaining.Keys
                    .Where(c => !active.Contains(c))
                    .ToList();

                if (pool.Count > 0)
                    return RandomPick(pool);
            }

            // ② 否则：只能推进已有颜色
            return RandomPick(active);
        }

        // ─────────────────────────
        // 工具
        // ─────────────────────────
        char RandomPick(IEnumerable<char> source)
        {
            var list = source as IList<char> ?? source.ToList();
            return list[rng.Next(list.Count)];
        }
    }
   
   
    public static string BuildSelfOld(
    IReadOnlyDictionary<char, int> colorCounts,
    int capacity,
    int k)
    {
        /**
         * 核心： 着色统计
         *  容量 + Dict 分类
         *  选择颜色 哪些可选 （卡槽内的颜色 + 其他剩余的颜色)
         */
        
        /**
         * 参数校验
         * 1. k的大小 >=2
         * 2. capacity > k
         * 3. colorCounts 的每个分类数量都是 k的倍数
         */

        var remainingColor = colorCounts.Keys.ToHashSet();
        var slots = new Dictionary<char, int>();
        var usedCapacity = 0;

        var colorDict = colorCounts.ToDictionary();
        var remaingCount = colorCounts.Values.Sum();

        // 在 k = 2, capacity = 4 时失效，此时 maxActive = 4, 但实际上最多存在3类
        // var maxActive = capacity / (k - 1); // 可作为边界 < maxActive(在remainingColor中随机，>= maxActive 只在slotColor中随机
        int maxActive = (capacity - 1) / (k - 1);
        var rng = new Random();
        var sb = new StringBuilder();
        /**
         * 问题1:
         * 如何避免这种死局： aabbcc
         */
        while (remaingCount > 0)
        {
            // 1. 随机一个颜色
            var selectColor = RandomColor(rng);
            sb.Append(selectColor.ToString());
            // 2. 添加到格子中
            colorDict[selectColor] -= 1;
            if (colorDict[selectColor] == 0)
            {
                colorDict.Remove(selectColor);
                remainingColor.Remove(selectColor);
            }
            AppendToSlot(selectColor);
            remaingCount--;
        }
        
        return sb.ToString();


        void AppendToSlot(char color)
        {
            if (slots.TryGetValue(color, out var count))
                slots[color] = count + 1;
            else
                slots[color] = 1;
            usedCapacity++;

            if (slots[color] >= k)
            {
                slots.Remove(color);
                usedCapacity -= k;
            }
            
            if (usedCapacity >= capacity)
                throw new ArgumentException("容量已满，理论上不可能");
        }

        
        char RandomColor(Random rng)
        {
            if (slots.Keys.Count < maxActive)
                return RandomFromHashSet(remainingColor, rng);
            return RandomKey(slots, rng);
        }
        
        
        // 从set中随机选取一个元素
        static T RandomFromHashSet<T>(HashSet<T> set, Random rng)
        {
            int index = rng.Next(set.Count);
            foreach (var item in set)
            {
                if (index-- == 0)
                    return item;
            }
            throw new UnreachableException();
        }
        
        static TKey RandomKey<TKey, TValue>(
            Dictionary<TKey, TValue> dict,
            Random rng)
        {
            int index = rng.Next(dict.Count);
            foreach (var key in dict.Keys)
            {
                if (index-- == 0)
                    return key;
            }
            throw new UnreachableException();
        }

    }
    
    // int maxConcurrentTypes;   // 原 maxActive
    // int dangerCapacityLine;   // 原 maxType
   
}