using Godot;
namespace Tile3.Scripts.Layout;

// ===========
//  待验证
// ==========


/// <summary>
/// 内容适配到容器的方式
/// 语义对齐 CSS / Unity / 常见引擎
/// </summary>
public enum FitMode
{
    /// <summary>
    /// 等比缩放，完整显示内容（可能留边）
    /// CSS: contain
    /// </summary>
    Contain,

    /// <summary>
    /// 等比缩放，完全覆盖容器（可能裁剪）
    /// CSS: cover
    /// </summary>
    Cover,

    /// <summary>
    /// 非等比缩放，拉伸填满
    /// CSS: stretch
    /// </summary>
    Stretch
}

/// <summary>
/// 一次 Fit 计算的结果：
/// 描述 逻辑空间 → 像素空间 的映射关系
/// </summary>
public readonly struct FitResult
{
    /// <summary>
    /// X 方向：逻辑单位 → 像素
    /// </summary>
    public readonly float UnitToPixelX;

    /// <summary>
    /// Y 方向：逻辑单位 → 像素
    /// Stretch 模式下可能与 X 不同
    /// </summary>
    public readonly float UnitToPixelY;

    /// <summary>
    /// 逻辑原点在像素空间中的偏移
    /// </summary>
    public readonly Vector2 PixelOffset;

    public FitResult(
        float unitToPixelX,
        float unitToPixelY,
        Vector2 pixelOffset)
    {
        UnitToPixelX = unitToPixelX;
        UnitToPixelY = unitToPixelY;
        PixelOffset = pixelOffset;
    }

    /// <summary>
    /// 将逻辑坐标转换为像素坐标
    /// </summary>
    public Vector2 LogicalToPixel(Vector2 logicalPosition)
    {
        return PixelOffset + new Vector2(
            logicalPosition.X * UnitToPixelX,
            logicalPosition.Y * UnitToPixelY
        );
    }

    /// <summary>
    /// 将逻辑尺寸转换为像素尺寸
    /// </summary>
    public Vector2 LogicalSizeToPixel(Vector2 logicalSize)
    {
        return new Vector2(
            logicalSize.X * UnitToPixelX,
            logicalSize.Y * UnitToPixelY
        );
    }
}

/// <summary>
/// 屏幕 / 容器适配工具
/// 纯数学，无业务依赖
/// </summary>
public static class Fit
{
    /// <summary>
    /// 将逻辑内容适配到像素容器中
    /// </summary>
    /// <param name="containerSizePx">容器像素尺寸</param>
    /// <param name="logicalSize">内容逻辑尺寸</param>
    /// <param name="mode">适配策略</param>
    public static FitResult Apply(
        Vector2 containerSizePx,
        Vector2 logicalSize,
        FitMode mode)
    {
        return mode switch
        {
            FitMode.Contain => FitContain(containerSizePx, logicalSize),
            FitMode.Cover   => FitCover(containerSizePx, logicalSize),
            FitMode.Stretch => FitStretch(containerSizePx, logicalSize),
            _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    // -------------------------------------------------
    // Contain：等比缩放，完整显示（可能留边）
    // -------------------------------------------------
    private static FitResult FitContain(
        Vector2 container,
        Vector2 logical)
    {
        float scaleX = container.X / logical.X;
        float scaleY = container.Y / logical.Y;

        float scale = Mathf.Min(scaleX, scaleY);

        Vector2 fittedSizePx = logical * scale;
        Vector2 offsetPx = (container - fittedSizePx) / 2f;

        return new FitResult(
            scale,
            scale,
            offsetPx
        );
    }

    // -------------------------------------------------
    // Cover：等比缩放，完全覆盖（可能裁剪）
    // -------------------------------------------------
    private static FitResult FitCover(
        Vector2 container,
        Vector2 logical)
    {
        float scaleX = container.X / logical.X;
        float scaleY = container.Y / logical.Y;

        float scale = Mathf.Max(scaleX, scaleY);

        Vector2 fittedSizePx = logical * scale;
        Vector2 offsetPx = (container - fittedSizePx) / 2f;

        return new FitResult(
            scale,
            scale,
            offsetPx
        );
    }

    // -------------------------------------------------
    // Stretch：非等比拉伸填满
    // -------------------------------------------------
    private static FitResult FitStretch(
        Vector2 container,
        Vector2 logical)
    {
        float scaleX = container.X / logical.X;
        float scaleY = container.Y / logical.Y;

        return new FitResult(
            scaleX,
            scaleY,
            Vector2.Zero
        );
    }
}
