using Godot;

namespace Tile3.AutoLoads;

public sealed partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    [Signal] // 测试事件的信号
    public delegate void TestEventHandler();
    [Signal] // 刷新中间层 Manager 的 LevelDto 和 MahjongDto
    public delegate void RefreshLevelManagerEventHandler(string levelStr);
    [Signal] // 调用中间层的数据快照，渲染显示层的麻将
    public delegate void InitMahjongContainerDisplayEventHandler();
    [Signal] // 解题事件的信号
    public delegate void SolveLevelInfoEventHandler();
    [Signal] // 显示解题结果的序号
    public delegate void ShowLevelInfoEventHandler();

    [Signal] // 对于题目染色的信号
    public delegate void FillColorEventHandler(int colorMode, int roundColor);

    [Signal]
    public delegate void AlertEventHandler(string alertText);
    [Signal] // 调用死亡解析器解析死亡原因的信号
    public delegate void AnalyseLevelDeathReasonEventHandler();
    [Signal] // UI 显示死亡信息的信号，传递秒数为单位的时间
    public delegate void ShowDeathReasonEventHandler(double solveTime);
    
    public override void _Ready()
    {
        // 防止多个 Manager 实例
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;    
    }
}