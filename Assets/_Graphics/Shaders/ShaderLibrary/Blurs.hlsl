// Legacy sampler2D blur variants; no static consumers found and external ownership is unknown.

#ifndef BLURS_INCLUDED
#define BLURS_INCLUDED

// 4-point box downsample
// Standard box filtering for downsampling - samples 4 points in a box pattern
float4 downsample4(sampler2D blurTex, float2 uv, float radius, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * radius;

    float4 s;
    s = tex2D(blurTex, uv + d.xy);
    s += tex2D(blurTex, uv + d.zy);
    s += tex2D(blurTex, uv + d.xw);
    s += tex2D(blurTex, uv + d.zw);

    return s * 0.25;
}

// 9-tap bilinear upsample (tent filter)
// Provides better quality upsampling with weighted samples
float4 upsampleTent(sampler2D blurTex, float2 uv, float radius, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * radius * 0.5;

    float4 s;
    s = tex2D(blurTex, uv - d.xy);
    s += tex2D(blurTex, uv - d.wy) * 2.0;
    s += tex2D(blurTex, uv - d.zy);

    s += tex2D(blurTex, uv + d.zw) * 2.0;
    s += tex2D(blurTex, uv) * 4.0;
    s += tex2D(blurTex, uv + d.xw) * 2.0;

    s += tex2D(blurTex, uv + d.zy);
    s += tex2D(blurTex, uv + d.wy) * 2.0;
    s += tex2D(blurTex, uv + d.xy);

    return s * (1.0 / 16.0);
}

// Legacy Kawase blur.
float4 kawase(sampler2D blurTex, float2 uv, float radius, float2 texelSize)
{
    float4 blurColor = float4(0, 0, 0, 0);

    // Center sample
    blurColor.rgb += tex2D(blurTex, uv).rgb;

    // Four diagonal samples
    blurColor.rgb += tex2D(blurTex, uv + float2(radius, radius) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(-radius, radius) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(radius, -radius) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(-radius, -radius) * texelSize).rgb;

    // Add axis-aligned samples for better coverage (9-tap)
    blurColor.rgb += tex2D(blurTex, uv + float2(radius, 0) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(-radius, 0) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(0, radius) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(0, -radius) * texelSize).rgb;

    blurColor.rgb /= 9.0;

    return blurColor;
}

// Legacy box blur.
float4 box(sampler2D blurTex, float2 uv, float radius, float2 texelSize, int boxRadius)
{
    float4 blurColor = float4(0, 0, 0, 0);
    int sampleCount = 0;

    for (int x = -boxRadius; x <= boxRadius; x++)
    {
        for (int y = -boxRadius; y <= boxRadius; y++)
        {
            blurColor.rgb += tex2D(blurTex, uv + (float2(x, y) * radius * texelSize)).rgb;
            sampleCount++;
        }
    }

    blurColor.rgb /= float(sampleCount);

    return blurColor;
}

#endif // BLURS_INCLUDED
