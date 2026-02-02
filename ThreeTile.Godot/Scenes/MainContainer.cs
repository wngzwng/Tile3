using Godot;

namespace Tile3.Scenes;

/// <summary>
/// 最外层的容器，横屏时横着，竖屏时竖着
/// </summary>
public partial class MainContainer : BoxContainer
{
	[Export] private VBoxContainer MidBox { get; set; }

	[Export] private VBoxContainer RightBox { get; set; }
	
	private void ResetDisplay()
	{
		Vertical = Size.X / Size.Y <= .6f;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Resized += ResetDisplay;
		ResetDisplay();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}