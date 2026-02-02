using Godot;
using ThreeTile.Core.ExtensionTools;
using Tile3.AutoLoads;

namespace Tile3.Scenes.RightBox;

public partial class SolveComponent : HBoxContainer
{
	[Export] private Button Button { get; set; }
	[Export] private Label SolveInfoLabel { get; set; }
	[Export] private ScrollContainer ScrollContainer { get; set; }

	private void ShowLevelInfo()
	{
		// var levelInfo = LevelManager.Instance.LevelInfo;
		// var result = "";
		// result += $"难度分: {levelInfo.DifficultyScore: 00.00}" +
		//           $"\n" +
		//           $"平均最大难度: {levelInfo.AverageMaxStepDifficultyAtSuccessPath}" +
		//           $"\n" +
		//           $"失败率: {levelInfo.FailRate * 100: 00.00}%" + 
		//           $"\n" +
		//           $"平均失败位置: {levelInfo.FailPosition * 100: 00.00}" +
		//           $"\n" +
		//           $"最小失败位置: {levelInfo.MinFailPosition * 100: 00.00}" +
		//           $"\n" +
		//           $"最大失败位置: {levelInfo.MaxFailPosition * 100: 00.00}";
		//
		// SolveInfoLabel.SetText(result);
		EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "解题完成！");
	}

	private void OnButtonPressed()
	{
		var mahjongDto = LevelManager.Instance.TileDtos;
		var levelDto = LevelManager.Instance.LevelDto;
		
		if (mahjongDto.Count == 0 || 
		    levelDto.Size.X() <= 1 || levelDto.Size.Y() <= 1 || levelDto.Size.Z() <= 1)
			return;
		
		EventBus.Instance.EmitSignal(EventBus.SignalName.SolveLevelInfo);
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Button.SetText("解题！");
		SolveInfoLabel.SetText("");

		Button.Pressed += OnButtonPressed;

		SizeFlagsStretchRatio = 1f;
		
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		
		ScrollContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		ScrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		ScrollContainer.SizeFlagsStretchRatio = 2;

		SolveInfoLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SolveInfoLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
		
		EventBus.Instance.ShowLevelInfo += ShowLevelInfo;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
