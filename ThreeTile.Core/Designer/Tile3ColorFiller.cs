using ThreeTile.Core.Core;
using ThreeTile.Core.Core.Moves;
using ThreeTile.Core.ExtensionTools;

namespace ThreeTile.Core.Designer;

/// <summary>
/// Tile3 出题器：
///
/// 核心思想：
/// 1. 颜色分配（DistributeConfig + ColorDistributor）
/// 2. 颜色序列生成（ColorBuilder）——不放回
/// 3. 模拟一条“可完成的消除路径”，逐步消费颜色序列
///
/// 注意：
/// - 本类不关心“颜色如何分配”
/// - 本类不关心“颜色序列如何构造”
/// - 只负责：
///   UnlockingTiles + SlotCapacity → 选择 Tile → 消费颜色 → 执行 Move
/// </summary>
public sealed class Tile3ColorFiller
{
    // ─────────────────────────────
    // 基础字段
    // ─────────────────────────────

    private readonly Level _modelLevel;
    private readonly DistributeConfig _distributeConfig;
    private readonly int _slotCapacity;
    private readonly SlotMode _slotMode;
    private readonly ColorMode _colorMode;

    private Level _designingLevel;

    private static readonly Random _rng = new();

    private string _coloredString;

    public string ColoredString => _coloredString;

    // ─────────────────────────────
    // 构造
    // ─────────────────────────────

    public Tile3ColorFiller(
        Level modelLevel,
        DistributeConfig distributeConfig,
        int slotCapacity,
        SlotMode slotMode = SlotMode.MaxConcurrent,
        ColorMode colorMode = ColorMode.Random)
    {
        _modelLevel = modelLevel;
        _designingLevel = modelLevel.Clone();

        _distributeConfig = distributeConfig;
        _slotCapacity = slotCapacity;
        _slotMode = slotMode;
        _colorMode = colorMode;
    }

    // ─────────────────────────────
    // 主出题流程
    // ─────────────────────────────

    /// <summary>
    /// 执行出题流程：
    /// - 构造颜色序列
    /// - 按解锁状态逐步染色并执行消除
    /// </summary>
    public void Design()
    {
        // 1️⃣ 花色 → 数量（不关心顺序）
        var colorCountDict = ColorDistributor.Distribute(_distributeConfig);

        // 2️⃣ 数量 → 颜色时间序列（不放回）
        var colorSeq = ColorBuilder.Build(
            colorCountDict,
            _slotCapacity,
            _distributeConfig.MatchRequireCount,
            _colorMode,
            _slotMode
        );

        int tintIndex = 0;

        // 3️⃣ 模拟一条可完成的消除路径
        while (_designingLevel.Pasture.Tiles.Count > 0)
        {
            var unlockingTiles = _designingLevel.Pasture.UnlockingTiles;
            int unlockingCount = unlockingTiles.Count;

            int availableCapacity = _designingLevel.StagingArea.AvailableCapacity;

            // 死局 / 等待消除
            if (unlockingCount == 0 || availableCapacity == 0)
                break;

            int maxSelectable = Math.Min(unlockingCount, availableCapacity);

            // 每步最多染 3 个，至少 1 个（安全）
            int selectCount = SafeNext(_rng, 1, Math.Min(maxSelectable, 3) + 1);

            // 颜色序列必须够用
            if (tintIndex + selectCount > colorSeq.Count)
                throw new InvalidOperationException(
                    $"Color sequence exhausted early. tintIndex={tintIndex}, selectCount={selectCount}");

            // 4️⃣ 不放回抽样：从解锁 Tile 中选取
            var selectedTiles = SampleWithoutReplacement(unlockingTiles, selectCount, _rng);

            // 5️⃣ 顺序染色（严格消费 colorSeq）
            for (int i = 0; i < selectedTiles.Count; i++)
            {
                selectedTiles[i].SetColor(colorSeq[tintIndex + i]);
            }
            tintIndex += selectCount;

            // 6️⃣ 执行选择 Move
            foreach (var tile in selectedTiles)
            {
                _designingLevel.DoMove(new SelectMove(tile.Index));
            }
        }

        // ─────────────────────────
        // 调试输出（可删）
        // ─────────────────────────
        foreach (var tile in _designingLevel.Corral.Tiles)
        {
            Console.WriteLine(tile);
        }

        _coloredString = _designingLevel.Corral.Tiles.Serialize();
        Console.WriteLine(_coloredString);
    }

    // ─────────────────────────────
    // 工具方法
    // ─────────────────────────────

    /// <summary>
    /// 不放回抽样（Fisher–Yates 前 k 洗牌）
    /// </summary>
    public static List<T> SampleWithoutReplacement<T>(
        IReadOnlyList<T> source,
        int k,
        Random? rng = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (k <= 0)
            return new List<T>();

        if (k > source.Count)
            throw new ArgumentOutOfRangeException(nameof(k));

        rng ??= Random.Shared;

        var buffer = source.ToArray();

        for (int i = 0; i < k; i++)
        {
            int j = rng.Next(i, buffer.Length);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }

        return buffer.Take(k).ToList();
    }

    /// <summary>
    /// 安全版 Random.Next：
    /// - 保证不会出现 min &gt;= max 的异常
    /// </summary>
    private static int SafeNext(Random rng, int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        return rng.Next(minInclusive, maxExclusive);
    }
}