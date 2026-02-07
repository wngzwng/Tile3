using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using ThreeTile.Core.Core;
using ThreeTile.Core.Core.Moves;
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

    private Tile3ColorFiller _tile3ColorFiller;
    // private LevelInfoAnalyser _levelInfoAnalyser;
    // public DeathReasonAnalyser DeathReasonAnalyser;
    private static readonly Stopwatch Timer = new();
    public IReadOnlyList<TileDto> TileDtos { get; private set; } = [];
    public LevelDto LevelDto { get; private set; }
    // public LevelInfo LevelInfo { get; private set; }

    public Level LevelCore => _levelCore;
    private void InitLevel(string str)
    {
        int pairCount = 3;
        int slotCapacity = 7;
        try
        {
            _levelCore = str.Deserialize(pairCount: pairCount, slotCapacity: slotCapacity);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            GD.Print(e);
            EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "解析错误");
            return;
        }
        GD.Print(str);
        GD.Print(_levelCore.Pasture.Tiles[0]);
        UpdateMahjongDtos();
        UpdateLevelDto();
        EventBus.Instance.EmitSignal(EventBus.SignalName.InitMahjongContainerDisplay);
    }
    
    /// <summary>
    /// 使用当前字段的 levelCore 实例获取所有麻将 DTO 的方法
    /// </summary>
    /// <returns></returns>
    public void UpdateMahjongDtos()
        => TileDtos = _levelCore.Pasture.Tiles.Select(TileDto.GetTileDtoFromTile).ToList();

    /// <summary>
    /// 使用当前实例下的 levelCore 获取 levelDto 的方法
    /// </summary>
    /// <returns></returns>
    public void UpdateLevelDto() => LevelDto = LevelDto.GetLevelDtoFromLevel(_levelCore);

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
        
        var config = new DistributeConfig()
        {
            TotalCount = _levelCore.Pasture.Tiles.Count,
            AvailableColorCount = 26,
            DistributeMode = ColorDistributor.ColorDistributeMode.Random,
            MatchRequireCount = 3,
            NormalMaxColorPairCount = 2,
            RoundCount = 10,
        };
        
        var designer = new Tile3ColorFiller(_levelCore.Clone(), config, 7);
        try
        {
            designer.Design();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            GD.Print(e);
            EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "染色失败");
            return;
        }
        DisplayServer.ClipboardSet(designer.ColoredString);
        EventBus.Instance.EmitSignal(EventBus.SignalName.Alert, "已复制到剪贴板");
    }

    public IReadOnlyList<BehaviourMove> GetBehaviours()
    {
        _levelCore.GetLogicBehaviours3();
        return _levelCore.LogicBehaviours;
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