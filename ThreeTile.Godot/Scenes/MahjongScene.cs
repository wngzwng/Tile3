using System;
using Godot;
using Tile3.Assets;

namespace Tile3.Scenes;

/// <summary>
/// 显示层中的麻将实例类
/// </summary>
public partial class MahjongScene : Control
{
    [Export] public TextureRect ModelTexture;
    [Export] public TextureRect ColorTexture;

    /// 牌面资源中，宽度上边框的占比
    private const float WidthBorderRatio = 0.0802395210f;
    /// 牌面资源中，高度上边框的占比
    private const float HeightBorderRatio = 0.0851851852f;
    /// 算出牌面尺寸后，花色需要占据的比例 (横向)
    private const float BaseColorTextureScaleFactor = 1.0f;
    /// 麻将牌块模型的图片尺寸
    private static readonly Vector2 ModelTextureSize = TextureAssetsManager.ModelTexture.GetSize();
    /// 所有的花色资源
    private static readonly Texture2D[] ColorTextures = TextureAssetsManager.GetColorTextures();
    /// 当前麻将整体的缩放大小
    private float _modelScaleFactor = 1f;

    /// <summary>
    /// 麻将的最大花色序号
    /// <br/>**题目不能给出比这个序号更大的题目
    /// <br/>**由于现在关卡是从 1 开始的花色序号，所以我们这里拿到之后把序号加个 "1"
    /// </summary>
    private static readonly int MaxColorIndex = ColorTextures.Length + 1;

    /// <summary>
    /// 改变麻将花色的方法，同步改变资源和对应的资源缩放
    /// </summary>
    public void SetColor(int color)
    {
        if (color > MaxColorIndex)
        {
            ColorTexture.Hide();
        }
        else
        {
            ColorTexture.SetTexture(ColorTextures[color - 1]); // 这里 - 1 是因为关卡花色序号从 1 开始，但是牌面花色是从 0 开始
        }
        SetMahjongBehavior();
    }

    /// <summary>
    /// 根据麻将实例当前的大小，更新牌面模型图片和花色图片的位置和缩放
    /// </summary>
    private void SetMahjongBehavior()
    {
        
#if DEBUG
#endif
        
        _modelScaleFactor = Math.Min(Size.X / ModelTextureSize.X, Size.Y / ModelTextureSize.Y);
        ModelTexture.SetSize(ModelTextureSize * _modelScaleFactor);

        var currentModelTextureSize = ModelTexture.GetSize();
        var colorTextureSize = ColorTexture.Texture.GetSize();
        // 去掉边框的实际区域的大小
        var colorZoneSize = new Vector2(currentModelTextureSize.X * (1 - WidthBorderRatio), currentModelTextureSize.Y * (1 - HeightBorderRatio));
        // 根据花色图片的尺寸和用于显示区域的尺寸，计算花色应该缩放多大
        var colorTextureScaleFactor = Math.Min(colorZoneSize.X / colorTextureSize.X,
            colorZoneSize.Y / colorTextureSize.Y) * BaseColorTextureScaleFactor;
        // 更新花色图片的最终尺寸
        colorTextureSize *= colorTextureScaleFactor;
        // 设置花色大小
        ColorTexture.SetSize(colorTextureSize);
        // 设置花色的偏移位置，使得花色在盘面位置居中
        ColorTexture.SetPosition((colorZoneSize - colorTextureSize) / 2);
    }

    public override void _Ready()
    {
        ModelTexture.SetTexture(TextureAssetsManager.ModelTexture);
        Resized += SetMahjongBehavior;
    }

    public override void _Process(double delta)
    {
    }
}