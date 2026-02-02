using Godot;
using System;
using ThreeTile.Core.ExtensionTools;

public partial class Test : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var levelStr =
			"000,2,4,6.22,4.40,2,4,6.62,4.80,2,4,6.A0,2,4,6.C0,2,4,6;112,4.32,4.52,4.72,4.90,2,4,6.B0,2,4,6;222,4.42,4.62,4.82,4.A1,5:a55JEZaaMaSN7CZGGC7GCJGSACOOAH2EXMVIVNXH6AQA6IQL2L";
		var level = levelStr.Deserialize(pairCount: 2, slotCapacity: 4);
		GD.Print("Main node ready");
		GD.Print(level);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
