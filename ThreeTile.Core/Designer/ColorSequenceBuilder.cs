using System.Diagnostics;
using System.Text;

namespace ThreeTile.Core.Designer;

/// <summary>
/// 花色数量的偏好模式（概率层）
/// </summary>
public enum ColorMode
{
    Random = 0,    // 随机
    Max = 1,       // 偏好 slots 中数量多的
    Min = 2,       // 偏好 slots 中数量少的
    Specified = 3  // 指定颜色
}


public enum SlotMode
{
    MaxConcurrent, // 最大颜色并发数
    DangerLine     // 容量死线
}

// ─────────────────────────────────────────────
// 结构层策略接口
// ─────────────────────────────────────────────
public interface IColorStructurePolicy
{
    IReadOnlyList<char> GetCandidates(
        IReadOnlyDictionary<char, int> remaining,
        IReadOnlyDictionary<char, int> slots,
        int used,
        int capacity,
        int k
    );

    int GetRequireCount(
        char selectColor,
        IReadOnlyDictionary<char, int> slots,
        int k
    );
}

    /// <summary>
    /// 结构策略 1：最大颜色并发数（maxConcurrentTypes）
    /// </summary>
    public sealed class MaxConcurrentTypesPolicy : IColorStructurePolicy
    {
        public IReadOnlyList<char> GetCandidates(
            IReadOnlyDictionary<char, int> remaining,
            IReadOnlyDictionary<char, int> slots,
            int used,
            int capacity,
            int k)
        {
            int maxConcurrentTypes = (capacity - 1) / (k - 1);

            // 还没达到并发上限：允许引入新颜色
            if (slots.Count < maxConcurrentTypes)
            {
                return remaining.Keys.ToList();
            }

            // 否则：只能从已有颜色中选
            return slots.Keys.ToList();
        }

        public int GetRequireCount(
            char selectColor,
            IReadOnlyDictionary<char, int> slots,
            int k)
        {
            return 1;
        }
    }

    /// <summary>
    /// 结构策略 2：容量死线（dangerCapacityLine）
    /// </summary>
    public sealed class DangerCapacityPolicy : IColorStructurePolicy
    {
        public IReadOnlyList<char> GetCandidates(
            IReadOnlyDictionary<char, int> remaining,
            IReadOnlyDictionary<char, int> slots,
            int used,
            int capacity,
            int k)
        {
            int dangerCapacityLine = capacity - (k - 1);

            // 还在安全区：允许新颜色
            if (used < dangerCapacityLine)
            {
                return remaining.Keys.ToList();
            }

            // 危险区：只能推进已有颜色
            return slots.Keys.ToList();
        }
        
        public int GetRequireCount(
            char selectColor,
            IReadOnlyDictionary<char, int> slots,
            int k)
        {
            if (slots.TryGetValue(selectColor, out var count))
                return k - count;
            return 1;
        }
    }

    // ─────────────────────────────────────────────
    // 概率 / 偏好策略接口
    // ─────────────────────────────────────────────
    public interface IColorPreferencePolicy
    {
        char Pick(
            IReadOnlyList<char> candidates,
            IReadOnlyDictionary<char, int> slots,
            IReadOnlyDictionary<char, int> remaining,
            Random rng
        );
    }

    /// <summary>
    /// 基于 ColorMode 的概率策略
    /// </summary>
    public sealed class ColorModePreferencePolicy : IColorPreferencePolicy
    {
        private readonly ColorMode _mode;

        public ColorModePreferencePolicy(ColorMode mode)
        {
            _mode = mode;
        }
        
       

        public char Pick(
            IReadOnlyList<char> candidates,
            IReadOnlyDictionary<char, int> slots,
            IReadOnlyDictionary<char, int> remaining,
            Random rng)
        {
            return _mode switch
            {
                ColorMode.Random =>
                    candidates[rng.Next(candidates.Count)],

                ColorMode.Max =>
                    WeightedRandom(
                        candidates,
                        c => remaining.GetValueOrDefault(c) + 1.0 + slots.GetValueOrDefault(c)*1.2,
                        rng),

                ColorMode.Min =>
                    WeightedRandom(
                        candidates,
                        c => 1.0 / (slots.GetValueOrDefault(c) + 1),
                        rng),
                _ =>
                    candidates[rng.Next(candidates.Count)]
            };
            
            
            static char WeightedRandom(
                IReadOnlyList<char> candidates,
                Func<char, double> weightFn,
                Random rng)
            {
                double total = 0;
                foreach (var c in candidates)
                    total += weightFn(c);

                double r = rng.NextDouble() * total;

                foreach (var c in candidates)
                {
                    r -= weightFn(c);
                    if (r <= 0)
                        return c;
                }

                // 理论上不会走到
                return candidates[^1];
            }
        }
    }

    // ─────────────────────────────────────────────
    // 主生成器
    // ─────────────────────────────────────────────
    public static class ColorBuilder
    {
        public static string Build(
            IReadOnlyDictionary<char, int> colorCounts,
            int capacity,
            int k,
            ColorMode colorMode,
            SlotMode  slotMode,
            int? seed = null)
        {
            // ─────────────────────────
            // 结构层策略选择
            // ─────────────────────────
            IColorStructurePolicy structurePolicy = slotMode switch
            {
                SlotMode.MaxConcurrent => new MaxConcurrentTypesPolicy(),
                SlotMode.DangerLine    => new DangerCapacityPolicy(),
                _ => throw new ArgumentOutOfRangeException(nameof(slotMode))
            };

            // ─────────────────────────
            // 概率 / 偏好策略选择
            // ─────────────────────────
            IColorPreferencePolicy preferencePolicy = colorMode switch
            {
                ColorMode.Random    => new ColorModePreferencePolicy(ColorMode.Random),
                ColorMode.Max       => new ColorModePreferencePolicy(ColorMode.Max),
                ColorMode.Min       => new ColorModePreferencePolicy(ColorMode.Min),
                ColorMode.Specified => new ColorModePreferencePolicy(ColorMode.Specified),
                _ => throw new ArgumentOutOfRangeException(nameof(colorMode))
            };

            // ─────────────────────────
            // 委托给核心生成器
            // ─────────────────────────
            return ColorBuilder.BuildCore(
                colorCounts: colorCounts,
                capacity: capacity,
                k: k,
                structurePolicy: structurePolicy,
                preferencePolicy: preferencePolicy,
                seed: seed
            );
        }
        
        
        public static string BuildCore(
            IReadOnlyDictionary<char, int> colorCounts,
            int capacity,
            int k,
            IColorStructurePolicy structurePolicy,
            IColorPreferencePolicy preferencePolicy,
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
            var remaining = new Dictionary<char, int>(colorCounts);
            var slots     = new Dictionary<char, int>();

            int used = 0;
            int left = remaining.Values.Sum();

            var rng = seed.HasValue ? new Random(seed.Value) : new Random();
            var sb  = new StringBuilder();

            // ─────────────────────────
            // 主循环
            // ─────────────────────────
            while (left > 0)
            {
                var candidates = structurePolicy.GetCandidates(
                    remaining, slots, used, capacity, k);

                if (candidates.Count == 0)
                    throw new InvalidOperationException("结构策略返回了空候选集");

                char c = preferencePolicy.Pick(
                    candidates, slots, remaining, rng);
                int count = structurePolicy.GetRequireCount(c, slots, k);
                Emit(c, count);
            }

            return sb.ToString();

            // ─────────────────────────
            // 核心动作
            // ─────────────────────────
            void Emit(char c, int count)
            {
                for(var i = 0; i < count; i++)
                    sb.Append(c);

                remaining[c] -= count;
                if (remaining[c] == 0)
                    remaining.Remove(c);

                slots[c] = slots.GetValueOrDefault(c) + count;
                used += count;
                left -= count;

                // 消除
                if (slots[c] == k)
                {
                    slots.Remove(c);
                    used -= k;
                }

                if (used >= capacity)
                    throw new InvalidOperationException("进入死锁态（理论上不应发生）");
            }
        }
    }