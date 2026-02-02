using Godot;
using ThreeTile.Core.ExtensionTools;
using Tile3.AutoLoads;

namespace Tile3.Scenes.MidBox;

public partial class MahjongContainer : Control
{
	[Export] public PackedScene MahjongScene { get; set; }
	
	#region 麻将在容器中相关的一些参数

	/// 一个麻将单元格的宽度权重
	/// <br/> **这里可以理解为一个 "标准麻将" 占据 2x2 的 "麻将单元格"
	/// <br/> 必须常为 1！！不允许修改！！
	private const float MahjongCellXRatio = 1f;
	/// 麻将单元格的高度权重
	/// <br/> **这里可以理解为一个 "标准麻将" 占据 2x2 的 "麻将单元格"
	private const float MahjongCellYRatio = 1.22f;
	/// 不同层麻将的视觉偏移距离权重
	private const float MahjongLayerXOffset = 0.12f;
	/// 不同层麻将的视觉偏移距离权重
	private const float MahjongLayerYOffset = 0.18f;
	/// 麻将本身相对于其麻将 cell 所占位置的横向缩放
	/// <br/>** 这个值越大，意味着图片超框的位置越多，说明图片的边框占图片的比例越大
	private const float MahjongSizeXScaleFactor = 1.05f;
	/// 麻将本身相对于其麻将 cell 所占位置的纵向缩放
	/// <br/>** 这个值越大，意味着图片超框的位置越多，说明图片的边框占图片的比例越大
	private const float MahjongSizeYScaleFactor = 1.16f;

	#endregion

	#region 关于麻将容器显示的一些参数

	/// 麻将单元格的横向长度
	private float _mahjongCellSizeX;
	/// 关卡宽度相对于麻将 Cell 宽度的比例
	private float _levelSizeXRatio;
	/// 关卡宽度相对于麻将 Cell 宽度的比例
	private float _levelSizeYRatio;
	/// 关卡内所有麻将的基础偏移
	private Vector2 _mahjongGlobalOffset;
	/// 关卡容器的宽度和麻将 cell 宽度的比值
	private float _containerSizeXRatio;

	#endregion
	
	/// <summary>
	/// 根据目前数据层的麻将集合实例，计算显示层麻将容器的显示比例的方法 (高 / 宽)
	/// </summary>
	private void SetContainerDisplayParameter(LevelDto level)
	{
		/*
		 * 长度宽度就是单层的长宽加上长宽对应的层偏移
		 * - 有 n 层就需要 (n - 1) 个层偏移
		 */
		_levelSizeXRatio = (level.Size.X() + MahjongSizeXScaleFactor - 1) * MahjongCellXRatio + (level.Size.Z() - 1) * MahjongLayerXOffset;
		_levelSizeYRatio = (level.Size.Y() + MahjongSizeYScaleFactor - 1) * MahjongCellYRatio + (level.Size.Z() - 1) * MahjongLayerYOffset;

		var containerRatio = Size.Y / Size.X; // 现在麻将容器的高 / 宽
		var levelOriginalRatio = _levelSizeYRatio / _levelSizeXRatio; // 现在关卡显示比例的高 / 宽
		
		// 为保证适配正常，需要分两种情况计算麻将偏移，以及计算整个容器的宽度和麻将宽度的比例
		if (containerRatio >= levelOriginalRatio)
		{
			_mahjongGlobalOffset = new Vector2(0, (Size.Y - Size.X * levelOriginalRatio) / 2); // 如果容器的高宽比高于原始高宽比，则麻将容器从 0 开始
			_containerSizeXRatio = _levelSizeXRatio;
		}
		else
		{
			_mahjongGlobalOffset = new Vector2((Size.X - Size.Y / levelOriginalRatio) / 2, 0); // 如果容器的高宽比高于原始高宽比，则麻将容器从 0 开始
			_containerSizeXRatio = _levelSizeXRatio * levelOriginalRatio / containerRatio;
		}
		/*
		 * 加上层数的总共偏移，这样不需要在增加麻将实例的时候不需要额外传参
		 * 否则就需要在 AddMahjongScene() 方法里加一个最大层数，如果加上这个总共偏移就在全局偏移的基础上减去
		 */
		_mahjongCellSizeX = Size.X / _containerSizeXRatio * MahjongCellXRatio;
		
		var maxZOffset = _mahjongCellSizeX * new Vector2(MahjongLayerXOffset, MahjongLayerYOffset) * (level.Size.Z() - 1);
		_mahjongGlobalOffset += maxZOffset;
	}
	
	/// <summary>
	/// 根据传入参数 (麻将在数据层的比例和体积)，增加麻将实例的方法
	/// </summary>
	private void AddMahjongScene(int position, int volume, int color)
	{
		var mahjong = MahjongScene.Instantiate<MahjongScene>();
		AddChild(mahjong);
		
		mahjong.SetColor(color);
		
		/*
		 * 根据层数和坐标位置设置位置，分三部分：全局偏移 + 当层偏移 - 层数偏移
		 *  - 当层偏移：由 position 的 X 和 Y 决定
		 *  - 层数偏移：由 position 的 Z 决定，每高一层需要减去对应的层偏移
		 */
		mahjong.SetPosition
		(
			_mahjongGlobalOffset +
			_mahjongCellSizeX *
			(
				new Vector2
				(
					position.X(), 
					position.Y() * MahjongCellYRatio / MahjongCellXRatio
				) - 
				position.Z() * new Vector2(MahjongLayerXOffset, MahjongLayerYOffset)
			)
		);

		// 麻将尺寸这里有一丢丢复杂，其显示的比例事实上应该超过对应的 mahjongCell 占据位置的比例，超出的部分是其模型图片上的边框
		mahjong.SetSize
		(
			new Vector2
			(
				_mahjongCellSizeX * volume.X() * MahjongSizeXScaleFactor, 
				_mahjongCellSizeX * volume.Y() * MahjongCellYRatio / MahjongCellXRatio * MahjongSizeYScaleFactor
			)
		);

		mahjong.SetZIndex(GetPositionIndex(position));
	}

	private static int GetPositionIndex(int position)
	{
		return position.Y() + position.X() + 4 * position.Z();
	}

	/// <summary>
	/// 从零开始调用 LevelManager 单例，并进行重新渲染的方法
	/// </summary>
	private void InitDisplay()
	{
		if (Size.X * Size.Y <= 1)
			return;
		
		foreach (var c in GetChildren())
			c.QueueFree();

		var mjDto = LevelManager.Instance.TileDtos;
		var lvDto = LevelManager.Instance.LevelDto;
		SetContainerDisplayParameter(lvDto);
		
		foreach (var m in mjDto)
		{
			AddMahjongScene(m.Position, m.Volume, m.Color);
		}
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		EventBus.Instance.InitMahjongContainerDisplay += InitDisplay;
		
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;

		SizeFlagsStretchRatio = 2f;
		
		Resized += InitDisplay;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}