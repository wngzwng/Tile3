using Godot;
using Tile3.AutoLoads;

namespace Tile3.Scenes.RightBox;

public partial class LevelStringComponent : HBoxContainer
{
	[Export] private TextEdit _textEdit;
	[Export] private Button _button;

	/// <summary>
	/// 默认显示和加载的关卡字符串
	/// </summary>
	private const string DefaultLevelString =
		"000,2,4,6,8,A.20,2,4,6,8,A.40,2,4,6,8,A.60,2,4,6,8,A.80,2,4,6,8,A.A0,2,4,6,8,A.C0,2,4,6,8,A.E0,2,4,6,8,A.G0,2,4,6,8,A;111,4,6,9.30,2,5,8,A.50,2,4,6,8,A.75.81,3,7,9.A1,5,9.B3,7.D0,5,A.F0,3,5,7,A;215.21,9.40,2,8,A.65.91,9.E0,5,A:TX8CESLZ8FLBSXEJGXPKUJQRSFJS5HQHLFG1KJP3H1MUP1G3FZ5HVRXTQYDLR1MI1BDT1PIQaNGCRUT97Y6WVO9OaV4DV6U4WDN7"
		;

	private void OnButtonPressed()
	{
		EventBus.Instance.EmitSignal(EventBus.SignalName.RefreshLevelManager, _textEdit.Text);
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_textEdit.SetText(DefaultLevelString);
		_button.Pressed += OnButtonPressed;

		_textEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_textEdit.SizeFlagsVertical = SizeFlags.ExpandFill;
		_textEdit.SizeFlagsStretchRatio = 5f;
		
		_button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_button.SizeFlagsVertical = SizeFlags.ExpandFill;
		_button.SetText("加载盘面");
		
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SizeFlagsStretchRatio = 1f;

		OnButtonPressed();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}