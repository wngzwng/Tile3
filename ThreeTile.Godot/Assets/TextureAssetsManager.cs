using Godot;
using Tile3.AutoLoads;

namespace Tile3.Assets;

public static class TextureAssetsManager
{
	private static readonly ColorTexture ColorTextureResources = GD.Load<ColorTexture>("res://Assets/ColorTextures/Colors.tres");

	public static Texture2D[] GetColorTextures()
	{
		return ColorTextureResources.Simple;
	}

	// 麻将的牌块模型图片
	public static readonly Texture2D ModelTexture = GD.Load<Texture2D>("res://Assets/ModelTexture/Model.png");
}
