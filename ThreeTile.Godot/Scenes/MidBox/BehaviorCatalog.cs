using Godot;
using System;
using System.Collections.Generic;
using ThreeTile.Core.ExtensionTools;
using Tile3.AutoLoads;

namespace Tile3.Scenes.MidBox;
public struct ItemInfo
{
    public string kind;
    public int Color;
    public int[] TileIndexes;
}

public partial class BehaviorCatalog : Control
{
    // ==============================
    // 外部注入的数据
    // ==============================

    private List<ItemInfo> _items = new();

    // ==============================
    // Node 引用
    // ==============================

    private Control _list;

    // ==============================
    // 生命周期
    // ==============================

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = 1000;
        _list = GetNode<Control>("List");
    }

    // ==============================
    // 对外 API
    // ==============================

    public void SetItems(List<ItemInfo> items)
    {
        _items = items ?? new List<ItemInfo>();
        GD.Print("setItem");
        RefreshList();
    }

    // ==============================
    // 核心：刷新列表（复用 Item）
    // ==============================

    private void RefreshList()
    {
        int targetCount = _items.Count;

        // ---------- 1. 全删掉 ----------
        foreach (var child in _list.GetChildren())
        {
            _list.RemoveChild(child);
            child.QueueFree();
        }

        // ---------- 2. 补充 ----------
        for (int i= 0; i < targetCount; i++)
        {
            var item = CreateItem(_items[i]);
            _list.AddChild(item);
            int index = i;
            item.GuiInput += (InputEvent e) =>
            {
                HandleItemInput(e, index);
            };
        }
        // GD.Print($"RefreshList2: {targetCount}");
        // // ---------- 3. 填充数据 + 绑定事件 ----------
        // for (int i = 0; i < targetCount; i++)
        // {
        //     int index = i; // 👈 绑定时捕获
        //
        //     var item = _list.GetChild<Control>(i);
        //     var label = item.GetNode<Label>("Label");
        //
        //     GD.Print($"RefreshList==3.1 {i} {label == null}");
        //     // 3.1 填充内容
        //     label.Text = BuildLabelText(_items[i]);
        //     GD.Print($"RefreshList==3.2 {i}");
        //     // 3.2 绑定输入
        //     item.GuiInput += (InputEvent e) =>
        //     {
        //         HandleItemInput(e, index);
        //     };
        // }
        // GD.Print($"RefreshList3 finish: {targetCount}");
    }

    // ==============================
    // Item 创建
    // ==============================

    // private Control CreateItem()
    // {
    //     var item = new Control
    //     {
    //         CustomMinimumSize = new Vector2(0, 32),
    //         MouseFilter = MouseFilterEnum.Stop
    //     };
    //
    //     var label = new Label
    //     {
    //         Name = "Label",
    //         AnchorsPreset = (int)LayoutPreset.FullRect,
    //         VerticalAlignment = VerticalAlignment.Center
    //     };
    //
    //     item.AddChild(label);
    //     return item;
    // }
    
    private Control CreateItem(ItemInfo info)
    {
        var item = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Stop
        };

        item.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var label = new Label
        {
            Text = BuildLabelText(info),
            VerticalAlignment = VerticalAlignment.Center,
            // AutowrapMode = TextServer.AutowrapMode.WordSmart
        };

        item.AddChild(label);
        return item;
    }

    // ==============================
    // Label 文本构建
    // ==============================

    private string BuildLabelText(ItemInfo info)
    {
        // 示例格式：
        // 简单：花色（3）：12，23，45
        // 首个位置
        var firstTile = LevelManager.Instance.LevelCore.Pasture.IndexToTileDict[info.TileIndexes[0]];
        return $"{info.kind}, 花色: {info.Color} 首个位置: {firstTile.TilePositionIndex.ToXyzString()}, 组：{string.Join("，", info.TileIndexes)}";
    }

    // ==============================
    // Input 处理
    // ==============================

    private void HandleItemInput(InputEvent e, int index)
    {
        if (e is not InputEventMouseButton btn)
            return;

        if (btn.ButtonIndex != MouseButton.Left || !btn.Pressed)
            return;

        OnItemClicked(index);
    }

    // ==============================
    // 点击响应
    // ==============================

    private void OnItemClicked(int index)
    {
        if (index < 0 || index >= _items.Count)
            return;

        var info = _items[index];
        GD.Print(info);
        HighlightRequested?.Invoke(info);
    }

    // ==============================
    // 对外事件
    // ==============================

    public event Action<ItemInfo>? HighlightRequested;
}
