// See https://aka.ms/new-console-template for more information

using System.Text;
using ThreeTile.Core.Designer;
using ThreeTile.Core.ExtensionTools;

Console.WriteLine("Hello, World!");
//
// var levelStr =
//     "000,2,4,6.22,4.40,2,4,6.62,4.80,2,4,6.A0,2,4,6.C0,2,4,6;112,4.32,4.52,4.72,4.90,2,4,6.B0,2,4,6;222,4.42,4.62,4.82,4.A1,5:a55JEZaaMaSN7CZGGC7GCJGSACOOAH2EXMVIVNXH6AQA6IQL2L";
//
// var levelStr1 =
//     "000,2,4,6,8,A.20,2,4,6,8,A.40,2,4,6,8,A.60,2,4,6,8,A.80,2,4,6,8,A.A0,2,4,6,8,A.C0,2,4,6,8,A.E0,2,4,6,8,A.G0,2,4,6,8,A;111,4,6,9.30,2,5,8,A.50,2,4,6,8,A.75.81,3,7,9.A1,5,9.B3,7.D0,5,A.F0,3,5,7,A;215.21,9.40,2,8,A.65.91,9.E0,5,A:TX8CESLZ8FLBSXEJGXPKUJQRSFJS5HQHLFG1KJP3H1MUP1G3FZ5HVRXTQYDLR1MI1BDT1PIQaNGCRUT97Y6WVO9OaV4DV6U4WDN7";
// var level = levelStr1.Deserialize();
// Console.WriteLine(level);


var config = new DistributeConfig()
{
    TotalCount = 3 * 26,
    AvailableColorCount = 8,
    DistributeMode = ColorDistributor.ColorDistributeMode.Specified,
    MatchRequireCount = 3,
    NormalMaxColorPairCount = 2,
    RoundCount = 10,
};
var colors = ColorDistributor.Distribute(config);
Console.WriteLine(ColorDistributor.FormattingColorsByPairLines(colors, config.MatchRequireCount));


var slotCapacity = 4;
var matchRequireCout = config.MatchRequireCount;

var seq = ColorBuilder.Build(colors, capacity: slotCapacity, matchRequireCout, ColorMode.Random, SlotMode.MaxConcurrent);
var s = seq.Select(number => GameStringTools.IndexToLetter(number)).ToArray();
Console.WriteLine(s);
// Tile3SlotSimulator.Simulate(new String(s), slotCapacity, matchRequireCout);