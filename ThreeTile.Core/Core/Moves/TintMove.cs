namespace ThreeTile.Core.Core.Moves;

// 着色移动
public class TintMove(Span<int> tileIndex) : Move
{
    // 是否有实体因本次选择进入了围栏
    private bool _enteredCorral;

    // 本次进入围栏的实体（用于 Undo）
    private (int tileIndex, int slotIndex)[] _partner
        = Array.Empty<(int, int)>();
    
    public override bool CanDo(Level level)
    {
        // foreach (var index in tileIndices)
        // {
        //     if (!level.Pasture.CanSelect(index))
        //         return false;
        // }
        //
        // // 模拟同样要尊重容量
        // if (!level.StagingArea.CanAccept(tileIndices.Length))
        //     return false;
        //
        // return true;
        return true;
    }

    public override void Do(Level level)
    {
        // var staged = new List<(int, int)>();
        //
        // // 1. 模拟 Extract + Stage
        // foreach (var index in tileIndices)
        // {
        //     var tile = level.Pasture.Extract(index, simulate: true);
        //     var slot = level.StagingArea.Stage(tile, simulate: true);
        //     staged.Add((index, slot));
        // }
        //
        // _staged = staged.ToArray();
        //
        // // 2. 尝试 Resolve（规则一致）
        // if (level.StagingArea.TryResolve(out var groups, simulate: true))
        // {
        //     _enteredCorral = groups.Count > 0;
        //
        //     _corralEntries = groups
        //         .SelectMany(g => g.Tiles)
        //         .Select(t => (t.Index, t.SlotIndex))
        //         .ToArray();
        //
        //     foreach (var group in groups)
        //     {
        //         level.Corral.Accept(group, simulate: true);
        //     }
        // }
        // else
        // {
        //     _enteredCorral = false;
        //     _corralEntries = Array.Empty<(int, int)>();
        // }
    }

    public override void Undo(Level level)
    {
        // // 1. 撤销模拟完成态
        // if (_enteredCorral)
        // {
        //     var groups = level.Corral.Retrieve(_corralEntries.Length, simulate: true);
        //     foreach (var group in groups)
        //     {
        //         level.StagingArea.Restore(group, simulate: true);
        //     }
        // }
        //
        // // 2. 撤销模拟 Stage / Extract
        // foreach (var (tileIndex, _) in _staged)
        // {
        //     var tile = level.StagingArea.Unstage(tileIndex, simulate: true);
        //     level.Pasture.Restore(tileIndex, tile, simulate: true);
        // }
        //
        // _enteredCorral = false;
        // _staged = Array.Empty<(int, int)>();
        // _corralEntries = Array.Empty<(int, int)>();
    }
}