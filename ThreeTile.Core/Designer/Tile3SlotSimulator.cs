namespace ThreeTile.Core.Designer;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class Tile3SlotSimulator
{
    public sealed record StepLog(
        int StepIndex,
        char Color,
        bool Eliminated,
        int Used,
        IReadOnlyDictionary<char, int> SlotCounts
    );

    /// <summary>
    /// 逐步模拟颜色序列在卡槽中的执行过程。
    /// - 每输入一个颜色：used+1，slot[color]++
    /// - 若 slot[color] == k：立刻消除（used -= k，slot[color]=0）
    /// - 若 used > capacity：抛异常（非法序列）
    /// </summary>
    public static List<StepLog> Simulate(
        string sequence,
        int capacity,
        int k,
        bool printEachStep = true)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        if (k < 2) throw new ArgumentException("k 必须 >= 2", nameof(k));
        if (capacity <= 0) throw new ArgumentException("capacity 必须 > 0", nameof(capacity));

        // 只跟踪序列中出现过的颜色
        var colors = sequence.Distinct().ToArray();
        // var slot = colors.ToDictionary(c => c, _ => 0);
        var slot = new Dictionary<char, int>();

        int used = 0;
        var logs = new List<StepLog>(sequence.Length);

        for (int i = 0; i < sequence.Length; i++)
        {
            char c = sequence[i];
            // if (!slot.ContainsKey(c))
            //     slot[c] = 0; // 理论上不会发生，因为上面 Distinct 初始化了

            used += 1;
            slot[c] = slot.GetValueOrDefault(c, 0) + 1;

            bool eliminated = false;
            if (slot[c] == k)
            {
                eliminated = true;
                used -= k;
                // slot[c] = 0;
                slot.Remove(c);
            }

            if (used > capacity)
            {
                // 打印一下最后状态更好排查
                var snap = Snapshot(slot);
                if (printEachStep)
                {
                    Console.WriteLine(FormatLine(i + 1, c, eliminated, used, capacity, snap));
                }
                throw new InvalidOperationException(
                    $"容量爆炸：step={i + 1}, color='{c}', used={used}, capacity={capacity}");
            }

            var snapshot = Snapshot(slot);
            logs.Add(new StepLog(i + 1, c, eliminated, used, snapshot));

            if (printEachStep)
                Console.WriteLine(FormatLine(i + 1, c, eliminated, used, capacity, snapshot));
        }

        return logs;

        static IReadOnlyDictionary<char, int> Snapshot(Dictionary<char, int> slot)
            => slot.ToDictionary(kv => kv.Key, kv => kv.Value);

        static string FormatLine(
            int step, char c, bool eliminated, int used, int capacity,
            IReadOnlyDictionary<char, int> slotCounts)
        {
            var counts = string.Join(" ",
                slotCounts.OrderBy(kv => kv.Key)
                          .Select(kv => $"'{kv.Key}':{kv.Value}"));

            return $"#{step:00} in={c}  elim={(eliminated ? "Y" : "N")}  used={used}/{capacity}  [{counts}]";
        }
    }
}
