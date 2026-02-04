using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tile3.Scenes.MidBox;

public partial class SlotContainer: Control
{
	private HBoxContainer _slot;
	
	[Export]
	private float _cellWidth;
	[Export]
	private float _cellHeight;
	[Export]
	private int _slotCapacity;

	private bool _initialized;

	private List<MahjongScene> _slotMahjongs = new();
	public override void _Ready()
	{
		// SlotRoot 只做一件事：准备好结构
		_slot = new HBoxContainer
		{
			Name = "SlotHBox",
			// Alignment = BoxContainer.AlignmentMode.Center
		};

		AddChild(_slot);

		_slot.SetAnchorsPreset(LayoutPreset.FullRect);
	}

	// ⭐ 核心入口：外部“喂数据”
	public void Initialize(
		float cellWidth,
		float cellHeight,
		int slotCapacity)
	{
		_cellWidth = cellWidth;
		_cellHeight = cellHeight;
		_slotCapacity = slotCapacity;

		ApplyLayout();

		_initialized = true;
	}

	private void ApplyLayout()
	{
		float width  = _cellWidth * _slotCapacity;
		float height = _cellHeight;
		
		AnchorLeft   = 0.5f;
		AnchorRight  = 0.5f;
		AnchorTop    = 1.0f;
		AnchorBottom = 1.0f;

		SetOffset(Side.Left,   -width / 2f);
		SetOffset(Side.Right,   width / 2f);
		SetOffset(Side.Top,    -height);
		SetOffset(Side.Bottom,  0);
	}
	
	public void AddMahjongScene(MahjongScene mahjongScene)
	{
		mahjongScene.CustomMinimumSize = new Vector2(_cellWidth, _cellHeight);
		_slotMahjongs.Add(mahjongScene);
		_slot.AddChild(mahjongScene);
	}

	public void RemoveMahjong(int index)
	{
		var mh = _slotMahjongs.Where(m => m.Index == index).ToArray();
		if (mh.Length == 0) return;

		_slotMahjongs.Remove(mh[0]);
		_slot.RemoveChild(mh[0]);
	}

	public void Clear()
	{
		_slotMahjongs.Clear();
		foreach (var child in _slot.GetChildren())
		{
			_slot.RemoveChild(child);
		}
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
