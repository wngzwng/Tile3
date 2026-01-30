using ThreeTile.Core.ExtensionTools;
namespace ThreeTile.Core.Core.LayerShadows;

/// <summary>
/// 层级遮挡管理器
/// 约定：z = 0 底层，z = maxLayer - 1 顶层
/// 只处理“层 + 平面占位”的遮挡事实，不感知 Tile
/// </summary>
public sealed class LayerShadowManager
{
    // ───────── 不变量（配置）────────

    private readonly ShadowPropagationEnum _mode;
    private readonly int _maxRow;
    private readonly int _maxCol;
    private readonly int _maxLayer;

    // ───────── 状态 ─────────

    public ShadowPropagationEnum Mode => _mode;
    public LayerShadow[] SelfShadow { get; }
    public LayerShadow[] IncomingShadow { get; }

    // ───────── 构造 ─────────

    public LayerShadowManager(
        ShadowPropagationEnum mode,
        int maxRow,
        int maxCol,
        int maxLayer)
    {
        if (maxRow <= 0) throw new ArgumentOutOfRangeException(nameof(maxRow));
        if (maxCol <= 0) throw new ArgumentOutOfRangeException(nameof(maxCol));
        if (maxLayer <= 0) throw new ArgumentOutOfRangeException(nameof(maxLayer));

        _mode = mode;
        _maxRow = maxRow;
        _maxCol = maxCol;
        _maxLayer = maxLayer;

        SelfShadow = new LayerShadow[maxLayer];
        IncomingShadow = new LayerShadow[maxLayer];

        for (int z = 0; z < maxLayer; z++)
        {
            SelfShadow[z] = new LayerShadow(maxRow, maxCol);
            IncomingShadow[z] = new LayerShadow(maxRow, maxCol);
        }
    }

    // ───────── 生命周期入口 ─────────

    /// <summary>
    /// 一次性全量重建（推荐入口）
    /// positionsByLayer[z] 表示第 z 层的所有自身投影位置
    /// </summary>
    public void RebuildAll(IReadOnlyList<int>[] positionsByLayer)
    {
        if (positionsByLayer is null)
            throw new ArgumentNullException(nameof(positionsByLayer));
        if (positionsByLayer.Length != _maxLayer)
            throw new ArgumentException("Layer count mismatch", nameof(positionsByLayer));

        for (int z = 0; z < _maxLayer; z++)
        {
            var self = SelfShadow[z];
            self.Clear();

            var positions = positionsByLayer[z];
            if (positions is null) continue;

            foreach (var pos in positions)
                self.AddPosition(pos);
        }

        BuildIncomingShadows();
    }

    // ───────── 增量修改（仅事实层）────────

    /// <summary>
    /// 增量加入自身投影（不更新 Incoming）
    /// </summary>
    public void AddSelfShadow(int z, IEnumerable<int> positions)
    {
        if ((uint)z >= (uint)_maxLayer)
            throw new ArgumentOutOfRangeException(nameof(z));

        var self = SelfShadow[z];
        foreach (var pos in positions)
            self.AddPosition(pos);
    }

    /// <summary>
    /// 增量移除自身投影（不更新 Incoming）
    /// </summary>
    public void RemoveSelfShadow(int z, IEnumerable<int> positions)
    {
        if ((uint)z >= (uint)_maxLayer)
            throw new ArgumentOutOfRangeException(nameof(z));

        var self = SelfShadow[z];
        foreach (var pos in positions)
            self.RemovePosition(pos);
    }

    /// <summary>
    /// 增量修改后重建传播层
    /// </summary>
    public void RebuildIncomingAfterChange()
        => BuildIncomingShadows();

    /// <summary>
    /// 增量加入并重建传播层
    /// </summary>
    public void AddSelfShadowAndRebuild(int z, IEnumerable<int> positions)
    {
        AddSelfShadow(z, positions);
        BuildIncomingShadows();
    }

    /// <summary>
    /// 增量移除并重建传播层
    /// </summary>
    public void RemoveSelfShadowAndRebuild(int z, IEnumerable<int> positions)
    {
        RemoveSelfShadow(z, positions);
        BuildIncomingShadows();
    }

    // ───────── 构建：传播层 ─────────

    /// <summary>
    /// 自顶向下构建 Incoming 投影
    /// </summary>
    private void BuildIncomingShadows()
    {
        // 顶层之上没有遮挡
        IncomingShadow[^1].Clear();

        for (int z = _maxLayer - 2; z >= 0; z--)
        {
            var curr = IncomingShadow[z];
            var aboveSelf = SelfShadow[z + 1];
            var aboveIncoming = IncomingShadow[z + 1];

            switch (_mode)
            {
                case ShadowPropagationEnum.DirectOnly:
                    curr.CopyFrom(aboveSelf);
                    break;

                case ShadowPropagationEnum.Cascade:
                    curr.CopyFrom(aboveIncoming);
                    curr.OrWith(aboveSelf);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    // ───────── 查询：位置级 ─────────

    /// <summary>
    /// 查询 packed position 是否被上层遮挡
    /// </summary>
    public bool IsPositionCovered(int position)
    {
        var (_, _, z) = position.UnpackXyz();
        if ((uint)z >= (uint)_maxLayer)
            return false;

        return IncomingShadow[z].IsPositionExist(position);
    }

    /// <summary>
    /// 查询 (x,y,z) 是否被上层遮挡
    /// </summary>
    public bool IsPositionCovered(int x, int y, int z)
    {
        if ((uint)z >= (uint)_maxLayer)
            return false;

        return IncomingShadow[z].IsPositionExist((x, y, z).PackXyz());
    }

    // ───────── 查询：区域级 ─────────

    /// <summary>
    /// 查询某层 IncomingShadow 是否与给定 Shadow 有重叠
    /// </summary>
    public bool IntersectsIncoming(int z, LayerShadow area)
    {
        if (area is null) throw new ArgumentNullException(nameof(area));
        if ((uint)z >= (uint)_maxLayer)
            throw new ArgumentOutOfRangeException(nameof(z));

        return IncomingShadow[z].Intersects(area);
    }

    /// <summary>
    /// 查询某层 IncomingShadow 与给定 Shadow 的重叠格子数
    /// </summary>
    public int IncomingIntersectCount(int z, LayerShadow area)
    {
        if (area is null) throw new ArgumentNullException(nameof(area));
        if ((uint)z >= (uint)_maxLayer)
            throw new ArgumentOutOfRangeException(nameof(z));

        return IncomingShadow[z].IntersectCount(area);
    }

    // ───────── 对外只读访问 ─────────

    public LayerShadow GetSelfShadow(int z)
    {
        if ((uint)z >= (uint)_maxLayer)
            throw new ArgumentOutOfRangeException(nameof(z));
        return SelfShadow[z];
    }

    public LayerShadow GetIncomingShadow(int z)
    {
        if ((uint)z >= (uint)_maxLayer)
            throw new ArgumentOutOfRangeException(nameof(z));
        return IncomingShadow[z];
    }

    // ───────── 克隆 ─────────

    public LayerShadowManager Clone()
    {
        var clone = new LayerShadowManager(_mode, _maxRow, _maxCol, _maxLayer);

        for (int z = 0; z < _maxLayer; z++)
        {
            clone.SelfShadow[z].CopyFrom(SelfShadow[z]);
            clone.IncomingShadow[z].CopyFrom(IncomingShadow[z]);
        }

        return clone;
    }

    // ───────── 调试 ─────────

    public override string ToString()
        => $"LayerShadowManager [{_maxRow}x{_maxCol} x {_maxLayer}, Mode={_mode}]";
}
