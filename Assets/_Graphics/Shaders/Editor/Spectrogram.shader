Shader "Unlit/Spectrogram"
{
    Properties
    {
        _Stencil ("Stencil ID", Integer) = 0

        [Toggle] _FlipX ("Flip X", Float) = 0.0
        [Toggle] _FlipY ("Flip Y", Float) = 0.0
        _OutlineWidth ("Outline Width", Float) = 0.1
        _ValueCutoff ("Value Cutoff", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Stencil
            {
                Ref [_Stencil]
                Comp Equal
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Local variables
            float _FlipX = 0;
            float _FlipY = 0;
            float _OutlineWidth = 0;
            float _ValueCutoff = 0;

            // Global variables
            uniform float _Spectrogram_Shift = 1;
            uniform float _Spectrogram_BilinearFiltering = 0;
            
            uniform float _SongTimeSeconds = 0;
            uniform float _ViewStart = 0;
            uniform float _ViewEnd = 1;

            uniform uint FFTFrequency = 1024;
            uniform uint FFTSize = 1024;
            uniform uint FFTCount = 1024;
            uniform StructuredBuffer<float> FFTResults;
            
            uniform uint GradientLength = 4;
            uniform StructuredBuffer<float> GradientKeys;
            uniform StructuredBuffer<float4> GradientColors;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Remap(float value, float from1, float to1, float from2, float to2)
            {
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }

            float sampleSpectrogram(uint resultIdx)
            {
                // Grab our FFT sample if its within bounds
                return resultIdx < FFTCount
                    ? FFTResults[resultIdx]
                    : 0;
            }

            float calculateSpectrogramValue(float currentSeconds, float2 uv)
            {
                // Single sample
                if (_Spectrogram_BilinearFiltering <= 0.5)
                {
                    // Convert our X location to the nearest chunk of FFT data
                    uint fftSampleLocation = (uint)((currentSeconds * FFTFrequency) / FFTSize) * FFTSize;

                    // Calculate our Y position within the view
                    uint fftSampleOffset = (uint)(lerp(0, FFTSize, uv.y * pow(_Spectrogram_Shift, uv.y - 1)));

                    // Calculate final FFT sample position
                    uint resultIdx = fftSampleLocation + fftSampleOffset;

                    // Return single sample
                    return sampleSpectrogram(resultIdx);
                }
                else
                // Bilinear sampling
                {
                    // Grab our chunk location as a float
                    float x = currentSeconds * FFTFrequency / FFTSize;

                    // Grab our offset location as a float
                    float y = lerp(0, FFTSize, uv.y * pow(_Spectrogram_Shift, uv.y - 1));

                    // Calculate bilinear sampling X/Y points
                    float x1 = floor(x);
                    float y1 = floor(y);

                    float x2 = ceil(x);
                    float y2 = ceil(y);

                    // Division by zero errors can happen if x and y are whole numbers.
                    // Nudge x2/y2 if this is the case.
                    if (x1 == x2) x2++;
                    if (y1 == y2) y2++;

                    // Perform bilinear sampling using weighted mean
                    // (Matrix math is likely more efficient but looks worse IMO)
                    float4 samples = float4(sampleSpectrogram((uint)x1 * FFTSize + (uint)y1),
                        sampleSpectrogram((uint)x1 * FFTSize + (uint)y2),
                        sampleSpectrogram((uint)x2 * FFTSize + (uint)y1),
                        sampleSpectrogram((uint)x2 * FFTSize + (uint)y2));
                    
                    // Calculate strengths of our sampling points
                    float4 strength = float4((x2 - x) * (y2 - y),
                        (x2 - x) * (y - y1),
                        (x - x1) * (y2 - y),
                        (x - x1) * (y - y1));
                    
                    float4 weightedMean = strength * samples;
                    weightedMean /= (x2 - x1) * (y2 - y1);

                    // Return accumulation of weighted mean
                    return weightedMean.r + weightedMean.g + weightedMean.b + weightedMean.a;
                }
            }

            float inverseLerp(float a, float b, float value)
            {
                return a != b
                    ? saturate((value - a) / (b - a))
                    : 0.0f;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = float2(_FlipX > 0.5 ? 1 - i.uv.x : i.uv.x, _FlipY > 0.5 ? 1 - i.uv.y : i.uv.y);
                
                // Calculate our X position within the view in seconds
                float currentSeconds = lerp(_ViewStart, _ViewEnd, uv.x);

                // fix shimmering pixels by saturating our spectrogram value between [0,1]
                //   I guess when interpolating between pixels, the above math can produce values slightly out of intended range.
                float value = currentSeconds > 0
                    ? saturate(calculateSpectrogramValue(currentSeconds, uv))
                    : 0.0;

                // Value clipping
                clip(value - _ValueCutoff);
                
                // Calculate gradient max
                uint upperGradientIdx = 0;
                for (; upperGradientIdx < GradientLength; upperGradientIdx++)
                {
                    if (value < GradientKeys[upperGradientIdx])
                        break;
                }
    
                // Edge case 1: Value is above maximum gradient key
                if (upperGradientIdx == GradientLength)
                {
                    return GradientColors[GradientLength - 1];
                }
    
                // Edge case 2: Value is below minimum threshold
                if (upperGradientIdx == 0)
                {
                    return GradientColors[0];
                }
    
                // Calculate gradient min
                upperGradientIdx = clamp(upperGradientIdx, 0, GradientLength - 1);
                uint lowerGradientIdx = clamp(upperGradientIdx - 1, 0, GradientLength - 1);

                // Gradient interpolation
                float4 color = lerp(GradientColors[lowerGradientIdx],
                    GradientColors[upperGradientIdx],
                    Remap(value, GradientKeys[lowerGradientIdx], GradientKeys[upperGradientIdx], 0, 1));

                // Add 0.2 to each component if we are within outline
                // TODO: Because units are in seconds, this outline scales with the length of grid
                //   Need to make the outline time/scale independent
                if (abs(currentSeconds - _SongTimeSeconds) < _OutlineWidth)
                {
                    color += float4(0.2, 0.2, 0.2, 0.2);
                }
                
                // Convert linear to gamma space
                color = pow(color, 2.2);
                
                return color;
            }
            ENDCG
        }
    }
}
