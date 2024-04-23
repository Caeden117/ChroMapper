using UnityEngine;

public static class ColorBufferManager
{
    private static readonly int gradientLength = Shader.PropertyToID("GradientLength");
    private static readonly int gradientKeys = Shader.PropertyToID("GradientKeys");
    private static readonly int gradientColors = Shader.PropertyToID("GradientColors");

    private static ComputeBuffer keysBuffer;
    private static ComputeBuffer colorsBuffer;

    public static void GenerateBuffersForGradient(Gradient heightmapGradient)
    {
        if (keysBuffer != null) ClearBuffers();

        var colorKeys = heightmapGradient.colorKeys;
        var length = colorKeys.Length;
        var keys = new float[length];
        var colors = new Color[length];

        for (var i = 0; i < length; i++)
        {
            keys[i] = colorKeys[i].time;
            colors[i] = colorKeys[i].color;
        }

        keysBuffer = new ComputeBuffer(length, sizeof(float));
        keysBuffer.SetData(keys);

        colorsBuffer = new ComputeBuffer(length, sizeof(float) * 4);
        colorsBuffer.SetData(colors);
        
        Shader.SetGlobalInt(gradientLength, length);
        Shader.SetGlobalBuffer(gradientKeys, keysBuffer);
        Shader.SetGlobalBuffer(gradientColors, colorsBuffer);
    }

    public static void ClearBuffers()
    {
        if (keysBuffer == null) return;
        
        keysBuffer.Dispose();
        keysBuffer = null;
        
        colorsBuffer.Dispose();
        colorsBuffer = null;
    }
}
