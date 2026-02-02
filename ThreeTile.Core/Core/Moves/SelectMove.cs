namespace ThreeTile.Core.Core.Moves;

// 普通的选择移动
public class SelectMove(int tileIndex) : Move
{
    // 是否有实体因本次选择进入了围栏
    private bool _enteredCorral;
    

    // 本次进入围栏的实体（用于 Undo）
    private List<int> _partners = new ();
    
    public override bool CanDo(Level level)
    {
        // 1. 牧场：这个 tile 现在是否可选
        if (!level.Pasture.CanSelect(tileIndex))
            return false;
        
        // 2. 集结区：是否还能接纳新的 tile
        if (!level.StagingArea.CanAccept())
            return false;

        return true;
    }

    public override void Do(Level level)
    {
        // // 1. 从牧场取出 tile
        var tile = level.Pasture.Extract(tileIndex);
        //
        // // 2. 放入集结区，记录 slot
        var slotIndex = level.StagingArea.Stage(tile);
        //
        // // 3. 尝试完成（可能一次完成多个）
        if (level.StagingArea.TryResolve(tile.Color, out var group))
        {
            _enteredCorral = true;
        
            // 记录所有参与完成的 partner（不包含自己也可以，看你规则）
            _partners = group
                .Where(t => t.Index != tileIndex)
                .Select(t => t.Index)
                .ToList();
        
            level.StagingArea.Remove(group);
            // 4. 送入围栏（完成态）
            level.Corral.Accept(group);
        }
        else
        {
            _enteredCorral = false;
            _partners.Clear();
        }
    }

    public override void Undo(Level level)
    {
        // 1. 如果这次选择触发了完成，先撤销完成态
        if (_enteredCorral)
        {
            // 取回所有本次产生的完成组
            var group = level.Corral.Retrieve(_partners.Count + 1);
            level.StagingArea.Restore(group);
        }
        //
        // // 2. 从集结区取回本次选中的 tile
        var tile = level.StagingArea.Unstage(tileIndex);
        //
        // // 3. 放回牧场
        level.Pasture.AddTile(tile);
        //
        // 清空本次 Move 的事实记录
        _enteredCorral = false;
        _partners = [];
    }
}