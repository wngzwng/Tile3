using Godot;
using ThreeTile.Core.ExtensionTools;
using Tile3.AutoLoads;

namespace Tile3.Scenes.RightBox;

public partial class DeathReasonAnalyseComponent : HBoxContainer
{
	[Export] private Button Button { get; set; }
	[Export] private Label DeathInfoLabel { get; set; }
	[Export] private ScrollContainer ScrollContainer { get; set; }

	private void ShowDeathReason(double analyseTime)
	{
		var result = "";
		var index = 1;

		// var badMahjongGroupList = LevelManager.Instance.DeathReasonAnalyser.BadMahjongGroupList;
		// result += $"耗时 {analyseTime:0.000} 秒, 共 {badMahjongGroupList.Count} 个致死组，已复制到剪贴板\n \n";
		// foreach (var badMahjongGroup in badMahjongGroupList)
		// {
		// 	/*
		// 	 * 这里的期望就是按颜色分组，输出各个麻将的 index
		// 	 * 例如 [Color11: 01, 03], [Color12: 02, 05], ...
		// 	 */
		// 	result += $"({index++:00}) ";
		//
		// 	var currentColor = -1;
		// 	foreach (var mahjong in badMahjongGroup)
		// 	{
		// 		if (mahjong.Color != currentColor)
		// 		{
		// 			if (!ReferenceEquals(mahjong, badMahjongGroup[0]))
		// 			{
		// 				result += "], ";
		// 			}
		// 			currentColor = mahjong.Color;
		// 			result += $"{ColorNameArray.ColorNames[currentColor]}: [";
		// 		}
		// 		else
		// 		{
		// 			result += ", ";
		// 		}
		// 		result += $"{mahjong.Index:00}";
		// 	}
		// 	result += "]";
		//
		// 	if (badMahjongGroup == LevelManager.Instance.DeathReasonAnalyser.BadMahjongGroupList[^1]) continue;
		// 	result += "\n";
		// }
		//
		// DeathInfoLabel.SetText(result);
		// DisplayServer.ClipboardSet(result);
		EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "解题完成！");
	}

	private static void OnButtonPressed()
	{
		var mahjongDto = LevelManager.Instance.TileDtos;
		var levelDto = LevelManager.Instance.LevelDto;
		
		if (mahjongDto.Count == 0 || 
		    levelDto.Size.X() <= 1 || levelDto.Size.Y() <= 1 || levelDto.Size.Z() <= 1)
			return;
		
		EventBus.Instance.EmitSignal(EventBus.SignalName.AnalyseLevelDeathReason);
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Button.SetText("解析死亡原因");

		Button.Pressed += OnButtonPressed;

		SizeFlagsStretchRatio = 1f;
		
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		
		ScrollContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		ScrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		
		DeathInfoLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		DeathInfoLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
		
		EventBus.Instance.ShowDeathReason += ShowDeathReason;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}