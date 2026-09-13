public static class PerlinNoise
{
    private static readonly int[] permutation =
    {
        151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
        140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
        247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32,
        57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
        74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
        60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
        65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169,
        200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
        52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
        207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
        119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
        129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
        218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
        81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
        184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
        222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
    };

    private static readonly int[] p = CreatePermutationTable();

    public static float Perlin3D(float x, float y, float z, int repeat)
    {
        if (repeat > 0)
        {
            x %= repeat;
            y %= repeat;
            z %= repeat;
        }

        var xi = (int)x & 255;
        var yi = (int)y & 255;
        var zi = (int)z & 255;
        var xf = x - (int)x;
        var yf = y - (int)y;
        var zf = z - (int)z;
        var u = Fade(xf);
        var v = Fade(yf);
        var w = Fade(zf);

        var aaa = p[p[p[xi] + yi] + zi];
        var aba = p[p[p[xi] + Increment(yi, repeat)] + zi];
        var aab = p[p[p[xi] + yi] + Increment(zi, repeat)];
        var abb = p[p[p[xi] + Increment(yi, repeat)] + Increment(zi, repeat)];
        var baa = p[p[p[Increment(xi, repeat)] + yi] + zi];
        var bba = p[p[p[Increment(xi, repeat)] + Increment(yi, repeat)] + zi];
        var bab = p[p[p[Increment(xi, repeat)] + yi] + Increment(zi, repeat)];
        var bbb = p[p[p[Increment(xi, repeat)] + Increment(yi, repeat)] + Increment(zi, repeat)];

        var x1 = Lerp(Gradient(aaa, xf, yf, zf), Gradient(baa, xf - 1f, yf, zf), u);
        var x2 = Lerp(Gradient(aba, xf, yf - 1f, zf), Gradient(bba, xf - 1f, yf - 1f, zf), u);
        var y1 = Lerp(x1, x2, v);
        x1 = Lerp(Gradient(aab, xf, yf, zf - 1f), Gradient(bab, xf - 1f, yf, zf - 1f), u);
        x2 = Lerp(
            Gradient(abb, xf, yf - 1f, zf - 1f),
            Gradient(bbb, xf - 1f, yf - 1f, zf - 1f),
            u);
        var y2 = Lerp(x1, x2, v);
        return (Lerp(y1, y2, w) + 1f) * 0.5f;
    }

    private static int[] CreatePermutationTable()
    {
        var table = new int[512];
        for (var i = 0; i < table.Length; i++) table[i] = permutation[i % permutation.Length];
        return table;
    }

    private static int Increment(int value, int repeat)
    {
        value++;
        return repeat > 0 ? value % repeat : value;
    }

    private static float Gradient(int hash, float x, float y, float z) => (hash & 15) switch
    {
        0 => x + y,
        1 => -x + y,
        2 => x - y,
        3 => -x - y,
        4 => x + z,
        5 => -x + z,
        6 => x - z,
        7 => -x - z,
        8 => y + z,
        9 => -y + z,
        10 => y - z,
        11 => -y - z,
        12 => y + x,
        13 => -y + z,
        14 => y - x,
        15 => -y - z,
        _ => 0f
    };

    private static float Fade(float value) =>
        value * value * value * (value * (value * 6f - 15f) + 10f);

    private static float Lerp(float a, float b, float value) => a + value * (b - a);
}
