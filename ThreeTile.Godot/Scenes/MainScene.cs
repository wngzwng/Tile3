using Godot;
using Tile3.AutoLoads;

namespace Tile3.Scenes;

public partial class MainScene : Node2D
{
    [Export] private MainContainer MainContainer { get; set; }
    [Export] private Label AlertLabel { get; set; }
    private Vector2 _viewportSize;

    /// <summary>
    /// 显示全局提示的方法
    /// </summary>
    private void Alert(string str)
    {
        AlertLabel.Text = str;
        AlertLabel.Show();
        
        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = 2f;
        timer.Timeout += () => AlertLabel.Hide();
        timer.OneShot = true;
        timer.Start();
    }

    /// <summary>
    /// 初始化提示文本的方法
    /// </summary>
    private void InitAlertLabel()
    {
        AlertLabel.Hide();
        AlertLabel.SetZIndex((int)RenderingServer.CanvasItemZMax);
    }

    private void OnSizeChanged()
    {
        _viewportSize = GetViewportRect().Size;
        AlertLabel.SetSize(_viewportSize);
        MainContainer.SetSize(_viewportSize);
    }

    public override void _Ready()
    {
        InitAlertLabel();
        GetViewport().SizeChanged += OnSizeChanged;
        OnSizeChanged();
        
        EventBus.Instance.Alert += Alert;
    }
}