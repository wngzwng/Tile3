// See https://aka.ms/new-console-template for more information

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


var colors = new Dictionary<char, int>
{
    ['a'] = 2,
    ['b'] = 4,
    ['c'] = 6,
    ['d'] = 4,
    ['e'] = 2
};
var capacity = 4;
var k = 2;

// var colors = new Dictionary<char, int>
// {
//     ['a'] = 3,
//     ['b'] = 6,
//     ['c'] = 6,
//     ['d'] = 3,
//     ['e'] = 9
// };
// var capacity = 7;
// var k = 3;
// string seq = Tile3SequenceBuilder.BuildSelf(colors, capacity: capacity, k: k);
string seq = ColorBuilder.Build(colors, capacity: capacity, k, ColorMode.Max, SlotMode.MaxConcurrent);
Console.WriteLine(seq);
Tile3SlotSimulator.Simulate(seq, capacity, k);