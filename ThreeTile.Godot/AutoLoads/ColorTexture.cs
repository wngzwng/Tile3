using Godot;

namespace Tile3.AutoLoads;

[GlobalClass]
public partial class ColorTexture: Resource
{
    [Export] public Texture2D[] Classic { get; set; }
    [Export] public Texture2D[] Simple { get; set; }
}