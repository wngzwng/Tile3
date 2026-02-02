using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using ThreeTile.Core.Core;
using ThreeTile.Core.Designer;
using ThreeTile.Core.ExtensionTools;

// using 齐天麻将Next.BadMahjongGroupDetector;
// using 齐天麻将Next.Designer;
// using 齐天麻将Next.Solver;

namespace Tile3.AutoLoads;

public sealed partial class LevelManager: Node
{
    public static LevelManager Instance { get; private set; }
    private Level _levelCore = null;
    // private LevelInfoAnalyser _levelInfoAnalyser;
    // public DeathReasonAnalyser DeathReasonAnalyser;
    private static readonly Stopwatch Timer = new();
    public IReadOnlyList<TileDto> TileDtos { get; private set; } = [];
    public LevelDto LevelDto { get; private set; }
    // public LevelInfo LevelInfo { get; private set; }

    private void InitLevel(string str)
    {
        int pairCount = 3;
        int slotCapacity = 7;
        _levelCore = str.Deserialize(pairCount: pairCount, slotCapacity: slotCapacity);
        UpdateMahjongDtos();
        UpdateLevelDto();
        EventBus.Instance.EmitSignal(EventBus.SignalName.InitMahjongContainerDisplay);
    }
    
    /// <summary>
    /// 使用当前字段的 levelCore 实例获取所有麻将 DTO 的方法
    /// </summary>
    /// <returns></returns>
    private void UpdateMahjongDtos()
        => TileDtos = _levelCore.Pasture.Tiles.Select(TileDto.GetTileDtoFromTile).ToList();

    /// <summary>
    /// 使用当前实例下的 levelCore 获取 levelDto 的方法
    /// </summary>
    /// <returns></returns>
    private void UpdateLevelDto() => LevelDto = LevelDto.GetLevelDtoFromLevel(_levelCore);

    // private void SolveLevelInfo()
    // {
    //     var solvingLevel = _levelCore.Clone();
    //     _levelInfoAnalyser = new LevelInfoAnalyser(solvingLevel);
    //     LevelInfo = _levelInfoAnalyser.Solve();
    //     EventBus.Instance.EmitSignal(EventBus.SignalName.ShowLevelInfo);
    // }
    //
    // private void AnalyseLevelDeathReason()
    // {
    //     DeathReasonAnalyser = new DeathReasonAnalyser(_levelCore);
    //     Timer.Restart();
    //     DeathReasonAnalyser.Analyse();
    //     Timer.Stop();
    //     EventBus.Instance.EmitSignal(EventBus.SignalName.ShowDeathReason, Timer.Elapsed.TotalSeconds);
    // }

    /// <summary>
    /// 根据传入的麻将 id 获取麻将 Dto 的方法
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public TileDto GetMahjongDto(int index)
        => TileDtos.First(m => m.Index == index);

    /// <summary>
    /// 染色的逻辑，染完之后直接把新关卡的字符串复制到剪贴板
    /// </summary>
    private void FillColor(int colorMode, int roundCount)
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "染色逻辑启动");
        // var designer = new Tile3ColorFiller(_levelCore, (Tile2ColorFiller.ColorMode)colorMode, roundCount);
        // designer.Design();
        // DisplayServer.ClipboardSet(designer.NewLevel.Serialize());
        // EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "已复制到剪贴板");
    }
    
    public override void _Ready()
    {
        // 防止多个 Manager 实例
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
        
        EventBus.Instance.RefreshLevelManager += InitLevel;
        // EventBus.Instance.SolveLevelInfo += SolveLevelInfo;
        EventBus.Instance.FillColor += FillColor;
        // EventBus.Instance.AnalyseLevelDeathReason += AnalyseLevelDeathReason;
    }
}