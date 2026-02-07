using System.Text;

namespace ThreeTile.Core.Core.Moves;


public enum BehaviourKind
{
    EASY_CLEAR,  // 简单行为
    HARD_CLEAR,  // 困难行为
    FLIP       // 翻盘行为
}


public sealed class BehaviourMove
{
    /// <summary>
    /// 行为的语义类型（消除 / 翻牌）
    /// </summary>
    public BehaviourKind Kind { get; }
    
    
    public int Color { get; }

    /// <summary>
    /// 本次行为中，被直接选择的 Tile
    /// </summary>
    public IReadOnlyList<int> TileIndexes { get; }

    public BehaviourMove(
        BehaviourKind kind,
        int color,
        IReadOnlyList<int> tileIndexes)
    {
        Kind = kind;
        Color = color;
        TileIndexes = tileIndexes;
    }


    public static IEnumerable<Move> Build(BehaviourMove behaviour)
    {
        foreach (var behaviourTileIndex in behaviour.TileIndexes)
        {
            yield return new SelectMove(behaviourTileIndex);
        }
    }

    public string ToRenderString(Level level)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < TileIndexes.Count; i++)
        {
            var tile = level.Pasture.IndexToTileDict[TileIndexes[i]];
            sb.AppendLine(tile.ToString());
        }

        return sb.ToString();
    }
}