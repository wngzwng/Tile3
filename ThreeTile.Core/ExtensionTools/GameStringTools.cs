using ThreeTile.Core.Core;
using ThreeTile.Core.Core.LayerShadows;

namespace ThreeTile.Core.ExtensionTools;

public static class GameStringTools
{
    /// <summary>
    /// 解析关卡字符串得到 LevelCore 类型，如果麻将的位置出现冲突则返回 null
    /// </summary>
    public static Level Deserialize(this string str, ShadowPropagationEnum mode = ShadowPropagationEnum.DirectOnly, int pairCount = 3, int slotCapacity = 7)
    {
        // 先把空格、回车和引号去掉
        str = str.Trim('\"', '\n', ' ', '●', '○');

        var s = str.Split(":");

        #region 解析第一段，也就是位置字符串

        // 位置的集合
        List<int> tilePositions = [];
        var boardStr = s[0];
        // 逐层解析
        var layerBoardStr = boardStr.Split(";");
        foreach (var layerStr in layerBoardStr)
        {
            var layerIndex = LetterToIndex(layerStr[0]);
            var tmpStr = layerStr[1..];
            // 逐行解析
            var yStr = tmpStr.Split(".");
            foreach (var tmp in yStr)
            {
                var y = LetterToIndex(tmp[0]);
                // 逐列解析
                var xStr = tmp[1..];
                var trueLength = xStr.Length;

                var tmpXStr = xStr.Replace(",", "");
                var tmpLength = tmpXStr.Length;

                if (tmpLength * 2 - 1 != trueLength) throw new InvalidOperationException("字符串不合法");
                tilePositions.AddRange(tmpXStr.Select(LetterToIndex).Select(x => (x, y, layerIndex).PackXyz()));
            }
        }

        #endregion

        #region 解析第二段，也就是花色字符串

        if (s.Length > 2)
        {
            throw new ArgumentException("关卡字符串不合法，: 分割的段超过2段");
        }

        var colorStr = s.Length == 2 ? s[1] : "";
        var colors = colorStr.Length == tilePositions.Count  //检查花色数是否与麻将数匹配
            ? colorStr.Select(LetterToIndex).ToArray()
            : Enumerable
                .Repeat(Tile.COLOR_UNSPECIFIED, tilePositions.Count)
                .ToArray();

        #endregion

        var level = new Level(
            positions: tilePositions.ToArray(), 
            colors: colors,
            slotCapacity: slotCapacity,
            requiredMatchingElementsCount: pairCount,
            mode: mode
        );

        return level;
    }

    /// <summary>
    /// 根据字符返回数字的方法，对应区间如下
    /// <br/> - 0-9 => 0-9
    /// <br/> - A-Z => 10~35
    /// <br/> - a-z => 36~61
    /// </summary>
    public static int LetterToIndex(char letter)
    {
        return letter switch
        {
            >= '0' and <= '9' => letter - '0',
            >= 'A' and <= 'Z' => letter - 'A' + 10,
            >= 'a' and <= 'z' => letter - 'a' + 36,
            _ => throw new InvalidOperationException("字符内容不正确")
        };
    }

    /// <summary>
    /// 序列化 Level 的方法
    /// </summary>
    /// <param name="level">目标关卡</param>
    /// <returns>目标关卡字符串</returns>
    public static string Serialize(this Level level)
    {
        // 将麻将标准化为以层号、行号、列号排序的方式，以免某些竞品不知道怎么写出来的若至逻辑导致顺序混乱
        return level.Pasture.Tiles.Serialize();
    }
    
    public static string Serialize(this IEnumerable<Tile> tiles)
    {
        // 将麻将标准化为以层号、行号、列号排序的方式，以免某些竞品不知道怎么写出来的若至逻辑导致顺序混乱
        var mahjongs = tiles
                .OrderBy(m => m.TilePositionIndex.Z())
                .ThenBy(m => m.TilePositionIndex.Y())
                .ThenBy(m => m.TilePositionIndex.X())
                .ToArray();
        var result = "";

        #region 先序列化位置信息

        for (var i = 0; i < mahjongs.Length; i++)
        {
            var x = mahjongs[i].TilePositionIndex.X().IndexToLetter();
            var y = mahjongs[i].TilePositionIndex.Y().IndexToLetter();
            var z = mahjongs[i].TilePositionIndex.Z().IndexToLetter();

            if (i == 0) result += $"{z}{y}{x}";
            else
            {
                if (mahjongs[i].TilePositionIndex.Z() != mahjongs[i - 1].TilePositionIndex.Z())
                    result += $";{z}{y}{x}";
                else if (mahjongs[i].TilePositionIndex.Y() != mahjongs[i - 1].TilePositionIndex.Y())
                    result += $".{y}{x}";
                else if (mahjongs[i].TilePositionIndex.X() != mahjongs[i - 1].TilePositionIndex.X())
                    result += $",{x}";
            }
        }

        #endregion

        // 使用 : 分割位置信息和花色信息
        result += ":";

        #region 序列化花色信息

        foreach (var m in mahjongs)
            result += $"{m.Color.IndexToLetter()}";

        #endregion

        return result;
    }

    /// <summary>
    /// 将数字映射为一个单个的字符表示，目前只支持 0-61 之间的数字
    /// </summary>
    /// <param name="index">整数。</param>
    /// <returns>字符。</returns>
    /// <exception cref="InvalidOperationException">整数越界。</exception>
    public static char IndexToLetter(this int index)
        => index switch
        {
            < 10 => (char)(index + '0'),
            < 36 => (char)(index - 10 + 'A'),
            < 62 => (char)(index - 36 + 'a'),
            _ => throw new InvalidOperationException("字符癌了变了")
        };
}