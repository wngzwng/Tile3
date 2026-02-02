using Godot;

namespace Tile3.Scenes.MidBox;

public partial class MidBox : VBoxContainer
{
	[Export] private MahjongContainer MahjongContainer { get; set; }
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.Fill;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}