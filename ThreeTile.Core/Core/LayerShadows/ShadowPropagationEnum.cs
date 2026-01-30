namespace ThreeTile.Core.Core.LayerShadows;

/// <summary>
/// 阴影传播方式
/// </summary>
public enum ShadowPropagationEnum
{
    /// <summary>
    /// 仅受直接上层影响
    /// </summary>
    DirectOnly,

    /// <summary>
    /// 受所有上层级联影响
    /// </summary>
    Cascade
}