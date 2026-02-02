using Godot;

namespace Tile3.Scenes.RightBox;

public partial class RightBox : VBoxContainer
{
	/// <summary>
	/// 关卡字符串组件
	/// </summary>
	[Export]
	private LevelStringComponent LevelStringComponent { get; set; }

	/// <summary>
	/// 解题组件
	/// </summary>
	[Export]
	private SolveComponent SolveComponent { get; set; }

	/// <summary>
	/// 填色组件
	/// </summary>
	[Export]
	private FillColorComponent FillColorComponent { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}