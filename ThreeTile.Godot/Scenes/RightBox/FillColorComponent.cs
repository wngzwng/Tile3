using System;
using Godot;
using ThreeTile.Core.Designer;
using Tile3.AutoLoads;

namespace Tile3.Scenes.RightBox;

public partial class FillColorComponent : HBoxContainer
{
    [Export] private Button FillColorButton { get; set; }
    [Export] private OptionButton OptionButton { get; set; }
	[Export] private TextEdit TextEdit { get; set; }

    /// <summary>
    /// 当染色按钮被按下时的动作
    /// </summary>
    private void OnButtonPressed()
    {
        if (!int.TryParse(TextEdit.Text, out var roundCount))
            EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "请输入数字！");
        
        EventBus.Instance.EmitSignal(EventBus.SignalName.FillColor, OptionButton.Selected, roundCount);
    }
    
    /// <summary>
    /// 设置下拉框按钮的方法
    /// </summary>
    private void InitOptionButton()
    {
        OptionButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        OptionButton.SizeFlagsVertical = SizeFlags.ExpandFill;

        foreach (var mode in Enum.GetValues<ColorDistributor.ColorDistributeMode>())
        {
            OptionButton.AddItem(mode.ToString(), (int)mode);
        }
        
        OptionButton.GetPopup().AddThemeFontSizeOverride("font_size", 35);
    }

    /// <summary>
    /// 初始化染色按钮
    /// </summary>
    private void InitDesignButton()
    {
        FillColorButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        FillColorButton.SizeFlagsVertical = SizeFlags.ExpandFill;
        FillColorButton.SetText("重新染色");

        FillColorButton.Pressed += OnButtonPressed;
    }
    
    public override void _Ready()
    {
        TextEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        TextEdit.SizeFlagsVertical = SizeFlags.ExpandFill;
        
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        InitDesignButton();
        InitOptionButton();
    }
}