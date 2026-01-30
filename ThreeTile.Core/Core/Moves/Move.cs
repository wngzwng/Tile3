namespace ThreeTile.Core.Core.Moves;

public abstract class Move
{
    public abstract bool CanDo(Level level);

    public abstract void Do(Level level);
    
    public abstract void Undo(Level level);
}