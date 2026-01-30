using System.Numerics;
using System.Text;
using ThreeTile.Core.ExtensionTools;

namespace ThreeTile.Core.Core.LayerShadows;

public class LayerShadow
{
    private const int BitLength = sizeof(uint) * 8;

    private readonly int _maxRow;
    private readonly int _maxCol;

    // 位集合，表示 (x,y) 平面上的占位情况
    private readonly uint[] _shadow;

    // ─────────────────────────
    // 构造 / 克隆
    // ─────────────────────────

    public LayerShadow(int maxRow, int maxCol)
    {
        _maxRow = maxRow;
        _maxCol = maxCol;
        _shadow = new uint[(maxRow * maxCol + BitLength - 1) / BitLength];
    }

    // 私有复制构造器（仅供 Clone 使用）
    private LayerShadow(int maxRow, int maxCol, uint[] shadowCopy)
    {
        _maxRow = maxRow;
        _maxCol = maxCol;
        _shadow = shadowCopy;
    }

    public LayerShadow Clone()
    {
        var copied = new uint[_shadow.Length];
        Array.Copy(_shadow, copied, _shadow.Length);
        return new LayerShadow(_maxRow, _maxCol, copied);
    }

    // ─────────────────────────
    // 坐标映射
    // ─────────────────────────

    /// <summary>
    /// 将 packed position 映射为位索引
    /// </summary>
    private void GetIndexInArea(int position, out int bitIndex, out int bitOffset)
    {
        var (x, y, _) = position.UnpackXyz();

#if DEBUG
        if ((uint)x >= (uint)_maxCol || (uint)y >= (uint)_maxRow)
            throw new ArgumentOutOfRangeException(
                $"(x,y)=({x},{y}) out of range row=[0,{_maxRow}), col=[0,{_maxCol})");
#endif

        int linearIndex = y * _maxCol + x;

        bitIndex  = linearIndex / BitLength;
        bitOffset = linearIndex % BitLength;
    }

    // ─────────────────────────
    // 单点操作
    // ─────────────────────────

    /// <summary> 添加一个位置 </summary>
    public void AddPosition(int position)
    {
        GetIndexInArea(position, out var bitIndex, out var bitOffset);
        _shadow[bitIndex] |= 1U << bitOffset;
    }

    /// <summary> 移除一个位置 </summary>
    public void RemovePosition(int position)
    {
        GetIndexInArea(position, out var bitIndex, out var bitOffset);
        _shadow[bitIndex] &= ~(1U << bitOffset);
    }

    /// <summary> 判断位置是否存在 </summary>
    public bool IsPositionExist(int position)
    {
        GetIndexInArea(position, out var bitIndex, out var bitOffset);
        return (_shadow[bitIndex] & (1U << bitOffset)) != 0;
    }

    // ─────────────────────────
    // 整体操作
    // ─────────────────────────

    /// <summary> 清空所有占位 </summary>
    public void Clear()
        => Array.Clear(_shadow);

    /// <summary> 从另一个 LayerShadow 复制 </summary>
    public void CopyFrom(LayerShadow other)
    {
        if (other._shadow.Length != _shadow.Length)
            throw new ArgumentException("LayerShadow size mismatch");

        Array.Copy(other._shadow, _shadow, _shadow.Length);
    }

    // ─────────────────────────
    // 位集合运算（原地）
    // ─────────────────────────

    /// <summary> 原地并集（OR） </summary>
    public void OrWith(LayerShadow other)
    {
        if (other._shadow.Length != _shadow.Length)
            throw new ArgumentException("LayerShadow size mismatch");

        for (int i = 0; i < _shadow.Length; i++)
            _shadow[i] |= other._shadow[i];
    }

    /// <summary> 原地交集（AND） </summary>
    public void AndWith(LayerShadow other)
    {
        if (other._shadow.Length != _shadow.Length)
            throw new ArgumentException("LayerShadow size mismatch");

        for (int i = 0; i < _shadow.Length; i++)
            _shadow[i] &= other._shadow[i];
    }

    // ─────────────────────────
    // 位集合运算（返回新对象）
    // ─────────────────────────

    /// <summary> 并集（OR），返回新对象 </summary>
    public LayerShadow Or(LayerShadow other)
    {
        if (other._shadow.Length != _shadow.Length)
            throw new ArgumentException("LayerShadow size mismatch");

        var result = new LayerShadow(_maxRow, _maxCol);
        for (int i = 0; i < _shadow.Length; i++)
            result._shadow[i] = _shadow[i] | other._shadow[i];

        return result;
    }

    /// <summary> 交集（AND），返回新对象 </summary>
    public LayerShadow And(LayerShadow other)
    {
        if (other._shadow.Length != _shadow.Length)
            throw new ArgumentException("LayerShadow size mismatch");

        var result = new LayerShadow(_maxRow, _maxCol);
        for (int i = 0; i < _shadow.Length; i++)
            result._shadow[i] = _shadow[i] & other._shadow[i];

        return result;
    }

    // ─────────────────────────
    // 判定 / 统计
    // ─────────────────────────

    /// <summary>
    /// 是否存在任意重叠位置（最快的相交判定）
    /// </summary>
    public bool Intersects(LayerShadow other)
    {
        if (other._shadow.Length != _shadow.Length)
            throw new ArgumentException("LayerShadow size mismatch");

        for (int i = 0; i < _shadow.Length; i++)
        {
            if ((_shadow[i] & other._shadow[i]) != 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 当前占位总数
    /// </summary>
    public int PopCount()
    {
        int count = 0;
        for (int i = 0; i < _shadow.Length; i++)
            count += BitOperations.PopCount(_shadow[i]);
        return count;
    }

    /// <summary>
    /// 与另一个 LayerShadow 的重叠占位数
    /// </summary>
    public int IntersectCount(LayerShadow other)
    {
        if (other._shadow.Length != _shadow.Length)
            throw new ArgumentException("LayerShadow size mismatch");

        int count = 0;
        for (int i = 0; i < _shadow.Length; i++)
            count += BitOperations.PopCount(_shadow[i] & other._shadow[i]);

        return count;
    }
    
    public string DumpGrid(StringBuilder? sb = null)
    {
        sb = sb ?? new StringBuilder();

        sb.AppendLine($"Grid Layout ({_maxRow} x {_maxCol}):");

        for (int y = 0; y < _maxRow; y++)
        {
            sb.Append($"{y:D2}: ");
            for (int x = 0; x < _maxCol; x++)
            {
                int linearIndex = y * _maxCol + x;
                int bitIndex = linearIndex / BitLength;
                int bitOffset = linearIndex % BitLength;

                bool occupied = (_shadow[bitIndex] & (1U << bitOffset)) != 0;
                sb.Append(occupied ? '█' : '·');
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
    
    public string DumpBits(StringBuilder? sb = null)
    {
        sb = sb ?? new StringBuilder();

        sb.AppendLine($"Bit Layout (uint[{_shadow.Length}], BitLength={BitLength}):");

        for (int i = 0; i < _shadow.Length; i++)
        {
            sb.Append($"[{i:D2}] ");

            uint value = _shadow[i];
            for (int b = 0; b < BitLength; b++)
            {
                sb.Append(((value >> b) & 1U) != 0 ? '1' : '0');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    
}





