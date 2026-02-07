// See https://aka.ms/new-console-template for more information

using System.Text;
using ThreeTile.Core.Core.Moves;
using ThreeTile.Core.Designer;
using ThreeTile.Core.ExtensionTools;

Console.WriteLine("Hello, World!");
// 022,C.42,C.62,C.82,6,8,C.A2,C.C2,C.E4,A.G6,8;133,B.53,B.93,7,B.B3,B.D3,B.F5,9;224,A.42,C.82,6,8,C.C4,A.E6,8.G4,A;333,B.53,B.77.93,B.B3,B.F5,9;422,C.44,A.62,C.82,6,8,C.A4,A.E6,8.G4,A:22LLNL332D2DNOCL2L77F3274F411CLOA117O314374HC4A437NNHDMAODMNMNOHODH1DHHF
var levelStr =
    "022,C.42,C.62,C.82,6,8,C.A2,C.C2,C.E4,A.G6,8;133,B.53,B.93,7,B.B3,B.D3,B.F5,9;224,A.42,C.82,6,8,C.C4,A.E6,8.G4,A;333,B.53,B.77.93,B.B3,B.F5,9;422,C.44,A.62,C.82,6,8,C.A4,A.E6,8.G4,A:22LLNL332D2DNOCL2L77F3274F411CLOA117O31H374HC4A437NNHDMAODMNMNOHODH1D4HF";
    // "022,C.42,C.62,C.82,6,8,C.A2,C.C2,C.E4,A.G6,8;133,B.53,B.93,7,B.B3,B.D3,B.F5,9;224,A.42,C.82,6,8,C.C4,A.E6,8.G4,A;333,B.53,B.77.93,B.B3,B.F5,9;422,C.44,A.62,C.82,6,8,C.A4,A.E6,8.G4,A:22LLNL332D2DNOCL2L77F3274F411CLOA117O314374HC4A437NNHDMAODMNMNOHODH1DHHF";
    // "066,8.82,C.A2,C.C6,8;155,9.73,B.B3,B.D5,9;246,8.64,A.82,C.A2,C.C4,A.E6,8;337.75,9.93,B.B5,9.F7;446,8.64,A.82,C.A2,C.C4,A.E6,8;537.57.93,B.D7.F7:DDHLII1ND8LH18I188HIP8ALOPHNNOIPHIOLHLNPCOC8PCOAPNLAON";
    // "084,C.A4,C.C4,6,8,A,C.E4,C.G4,C;195,B.B5,7,9,B.D5,7,9,B.F5,B;284,C.A4,C.C4,6,8,A,C.E4,C.G4,C;395,B.B5,7,9,B.D5,7,9,B.F5,B;466,A.A6,8,A.C6,A.E6,8,A;566,A.95,B.B5,9,B.D5,7,B.F5,B;666,A;766,A;866,A\n";
    // "000,2,4,6.22,4.40,2,4,6.62,4.80,2,4,6.A0,2,4,6.C0,2,4,6;112,4.32,4.52,4.72,4.90,2,4,6.B0,2,4,6;222,4.42,4.62,4.82,4.A1,5:a55JEZaaMaSN7CZGGC7GCJGSACOOAH2EXMVIVNXH6AQA6IQL2L";

// var levelStr1 =
//     "000,2,4,6,8,A.20,2,4,6,8,A.40,2,4,6,8,A.60,2,4,6,8,A.80,2,4,6,8,A.A0,2,4,6,8,A.C0,2,4,6,8,A.E0,2,4,6,8,A.G0,2,4,6,8,A;111,4,6,9.30,2,5,8,A.50,2,4,6,8,A.75.81,3,7,9.A1,5,9.B3,7.D0,5,A.F0,3,5,7,A;215.21,9.40,2,8,A.65.91,9.E0,5,A:TX8CESLZ8FLBSXEJGXPKUJQRSFJS5HQHLFG1KJP3H1MUP1G3FZ5HVRXTQYDLR1MI1BDT1PIQaNGCRUT97Y6WVO9OaV4DV6U4WDN7";
var level = levelStr.Deserialize(pairCount: 3, slotCapacity: 7);
// Console.WriteLine(level.Pasture.Tiles[0]);
// Console.WriteLine(level);

level.GetLogicBehaviours3();
foreach (var logicBehaviour in level.LogicBehaviours)
{
    Console.WriteLine(logicBehaviour.ToRenderString(level));
}




// var totalCount = level.Pasture.Tiles.Count;
// Console.WriteLine($"{totalCount}");
// var config = new DistributeConfig()
// {
//     TotalCount = totalCount,
//     AvailableColorCount = 26,
//     DistributeMode = ColorDistributor.ColorDistributeMode.Max,
//     MatchRequireCount = 3,
//     NormalMaxColorPairCount = 2,
//     RoundCount = 10,
// };
// var colors = ColorDistributor.Distribute(config);
// Console.WriteLine(ColorDistributor.FormattingColorsByPairLines(colors, config.MatchRequireCount));
//
//
// var slotCapacity = 7;
// var matchRequireCout = config.MatchRequireCount;
// var filler = new Tile3ColorFiller(level, config, slotCapacity);
// filler.Design();


// var seq = ColorBuilder.Build(colors, capacity: slotCapacity, matchRequireCout, ColorMode.Random, SlotMode.MaxConcurrent);
// var s = seq.Select(number => GameStringTools.IndexToLetter(number)).ToArray();
// Console.WriteLine(s);
// Tile3SlotSimulator.Simulate(new String(s), slotCapacity, matchRequireCout);