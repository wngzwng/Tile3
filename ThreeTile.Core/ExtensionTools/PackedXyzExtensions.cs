namespace ThreeTile.Core.ExtensionTools;


public static class PackedXyzExtensions
{
    // Layout (LSB → MSB): X | Y | Z | Reserved
    private const int XShift = 0;
    private const int YShift = 8;
    private const int ZShift = 16;

    private const int Mask = 0xFF;

    // ---- Pack ----

    public static int PackXyz(this (byte x, byte y, byte z) v)
        => v.x
           | (v.y << YShift)
           | (v.z << ZShift);

    public static int PackXyz(this (int x, int y, int z) v)
        => (v.x & Mask)
           | ((v.y & Mask) << YShift)
           | ((v.z & Mask) << ZShift);

    // ---- Unpack ----

    public static (byte x, byte y, byte z) UnpackXyz(this int value)
        => ((byte)(value & Mask),
            (byte)((value >> YShift) & Mask),
            (byte)((value >> ZShift) & Mask));

    // ---- Read components ----

    public static byte XByte(this int value) => (byte)(value & Mask);
    public static byte YByte(this int value) => (byte)((value >> YShift) & Mask);
    public static byte ZByte(this int value) => (byte)((value >> ZShift) & Mask);

    public static int X(this int value) => value & Mask;
    public static int Y(this int value) => (value >> YShift) & Mask;
    public static int Z(this int value) => (value >> ZShift) & Mask;
    
    public static string ToXyzString(this int value)
    {
        var (x, y, z) = value.UnpackXyz();
        return $"({x}, {y}, {z})";
    }

    // ---- Write components ----

    public static int WithX(this int value, int x)
        => (value & ~Mask) | (x & Mask);

    public static int WithY(this int value, int y)
        => (value & ~(Mask << YShift)) | ((y & Mask) << YShift);

    public static int WithZ(this int value, int z)
        => (value & ~(Mask << ZShift)) | ((z & Mask) << ZShift);

    public static int WithX(this int value, byte x)
        => (value & ~Mask) | x;

    public static int WithY(this int value, byte y)
        => (value & ~(Mask << YShift)) | (y << YShift);

    public static int WithZ(this int value, byte z)
        => (value & ~(Mask << ZShift)) | (z << ZShift);
}
