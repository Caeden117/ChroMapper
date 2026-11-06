Shader "ChroMapper/Object/Basic Gradient"
{
    Properties
    {
        _ColorA("Color A", Vector) = (1.0, 0.0, 0.0, 1.0)
        _ColorB("Color B", Vector) = (1.0, 0.0, 0.0, 1.0)
        _StrobeColorA("Strobe Color A", Vector) = (1.0, 0.0, 0.0, 1.0)
        _StrobeColorB("Strobe Color B", Vector) = (1.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _LightDistributionTex("Light Distribution", 2D) = "black" {}
        _LightDistributionWidth("Light Distribution Width", Float) = 0
        _UseLightDistribution("Use Light Distribution", Float) = 0
        _UseLightTimeline("Use Light Timeline", Float) = 0
        _LightTimelineDuration("Light Timeline Duration", Float) = 1
        _LightTimelineStart("Light Timeline Start", Float) = 0
        // Appended ribbon payload: (boosted endpoint texel XY, boost boundary table texel XY).
        _LightBoostData("Light Boost Data", Vector) = (0,0,0,0)
        _LightBoostCount("Light Boost Count", Float) = 0
        _LightBoostInitial("Light Boost Initial", Float) = 0
        // Extend rasterization past the boundary to complete the one-pixel AA ramp.
        _RibbonEdgePadding("Ribbon Edge Padding", Float) = 0
        // Adjacent ribbons need the same lane coordinates so their edges meet on the same pixels.
        _UseRibbonPlane("Use Ribbon Plane", Float) = 0
        _RibbonPlaneOrigin("Ribbon Plane Origin", Vector) = (0,0,0,0)
        _RibbonPlaneTime("Ribbon Plane Time", Vector) = (0,0,1,0)
        _RibbonPlaneWidth("Ribbon Plane Width", Vector) = (1,0,0,0)
        _StrobeDuration("Strobe Duration", Float) = 1
        _StrobeFade("Strobe Fade", Float) = 0
        _StrobeFrequencyA("Strobe Frequency A", Float) = 0
        _StrobeFrequencyB("Strobe Frequency B", Float) = 0
        _UseStrobeColors("Use Strobe Colors", Float) = 0
        _EasingID("Easing ID", Int) = 0
        _UseHSV("Use HSV", Int) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }
        LOD 100
        ZWrite Off
        Cull Off
        Blend SrcColor OneMinusSrcColor

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Easings.hlsl"

            sampler2D _LightDistributionTex;
            float4 _LightDistributionTex_TexelSize;
            // Ribbon lanes use the editor's common scrolling beat coordinate.
            float _Rotation;
            float4 _SongBpmTime;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorB)
                UNITY_DEFINE_INSTANCED_PROP(float4, _StrobeColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _StrobeColorB)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeDuration)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFade)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFrequencyA)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeFrequencyB)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseStrobeColors)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseLightDistribution)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightDistributionWidth)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseLightTimeline)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightTimelineDuration)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightTimelineStart)
                UNITY_DEFINE_INSTANCED_PROP(float4, _LightBoostData)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightBoostCount)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightBoostInitial)
                UNITY_DEFINE_INSTANCED_PROP(float, _RibbonEdgePadding)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseRibbonPlane)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RibbonPlaneOrigin)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RibbonPlaneTime)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RibbonPlaneWidth)
                UNITY_DEFINE_INSTANCED_PROP(int, _EasingID)
                UNITY_DEFINE_INSTANCED_PROP(int, _UseHSV)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                // Use one lane projection for neighboring ribbons so their edge coverage agrees.
                nointerpolation float3 laneTime : TEXCOORD1;
                nointerpolation float3 laneWidth : TEXCOORD2;
                nointerpolation float3 laneDenominator : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 RGBToHSV(float3 color)
            {
                const float epsilon = 1e-10f;
                const float4 constants = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
                float4 p = lerp(float4(color.bg, constants.wz), float4(color.gb, constants.xy), step(color.b, color.g));
                float4 q = lerp(float4(p.xyw, color.r), float4(color.r, p.yzx), step(p.x, color.r));
                float chroma = q.x - min(q.w, q.y);
                return float3(abs(q.z + ((q.w - q.y) / ((6.0f * chroma) + epsilon))), chroma / (q.x + epsilon), q.x);
            }

            float3 HSVToRGB(float3 color)
            {
                float3 rgb = abs((frac(color.xxx + float3(0.0f, 2.0f / 3.0f, 1.0f / 3.0f)) * 6.0f) - 3.0f);
                return color.z * lerp(1.0f, saturate(rgb - 1.0f), color.y);
            }

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // Pad both axes by one screen pixel and extrapolate UVs so AA can
                // shade outside the mesh without moving the boundary.
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonEdgePadding) > 0.5f)
                {
                    float sine, cosine;
                    sincos(radians(_Rotation), sine, cosine);
                    float3x3 rotation = float3x3(cosine, 0, sine, 0, 1, 0, -sine, 0, cosine);
                    float3 widthAxis = mul(rotation,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonPlaneWidth).xyz);
                    float3 timeAxis = mul(rotation,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonPlaneTime).xyz);
                    float3 origin = mul(rotation,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _RibbonPlaneOrigin).xyz);
                    float3 projectedOrigin = mul(UNITY_MATRIX_VP, float4(origin, 1.0f)).xyw;
                    float3 projectedTime = mul(UNITY_MATRIX_VP, float4(timeAxis, 0)).xyw;
                    float3 projectedWidthAxis = mul(UNITY_MATRIX_VP, float4(widthAxis, 0)).xyw;
                    o.laneTime = cross(projectedWidthAxis, projectedOrigin);
                    o.laneWidth = cross(projectedOrigin, projectedTime);
                    o.laneDenominator = cross(projectedTime, projectedWidthAxis);
                    float4 along = UnityObjectToClipPos(v.vertex + float4(1, 0, 0, 0));
                    float4 across = UnityObjectToClipPos(v.vertex + float4(0, 1, 0, 0));
                    float2 position = o.vertex.xy / o.vertex.w;
                    float2 tangent = (along.xy / along.w - position) * _ScreenParams.xy;
                    float2 normal = normalize(float2(-tangent.y, tangent.x));
                    // Reuse the projected width axis for padding in both directions.
                    float2 widthTangent = (across.xy / across.w - position) * _ScreenParams.xy;
                    float projectedWidth = abs(dot(widthTangent * 0.5f, normal));
                    float padding = (v.uv.y > 0.5f ? 1.0f : -1.0f)
                        / max(projectedWidth, 1e-4f);
                    // Extend across event joins so the next ribbon can draw the shared edge pixel.
                    float2 timeNormal = normalize(float2(-widthTangent.y, widthTangent.x));
                    float projectedLength = abs(dot(tangent * 0.5f, timeNormal));
                    float timePadding = (v.uv.x > 0.5f ? 1.0f : -1.0f)
                        / max(projectedLength, 1e-4f);
                    o.vertex = UnityObjectToClipPos(v.vertex + float4(timePadding, padding, 0, 0));
                    o.uv.x += timePadding;
                    o.uv.y += padding;
                }

                return o;
            }

            // Match light interpolation: mode 1 uses scalar HSV; mode 2 takes the shortest hue path.
            float4 InterpolateRibbonColor(float4 startColor, float4 endColor, float t, int colorLerpType)
            {
                float4 color;
                if (colorLerpType != 0)
                {
                    float4 startHsv = float4(RGBToHSV(startColor.rgb), startColor.a);
                    float4 endHsv = float4(RGBToHSV(endColor.rgb), endColor.a);
                    float hue;
                    if (colorLerpType == 1)
                    {
                        // Legacy HSV interpolates scalar hue, preserving the long arc through green.
                        hue = lerp(startHsv.x, endHsv.x, t);
                    }
                    else
                    {
                        // Match Mathf.LerpAngle's shortest delta, clamping only hue's easing input.
                        float hueDelta = frac(endHsv.x - startHsv.x);
                        if (hueDelta > 0.5f)
                            hueDelta -= 1.0f;
                        hue = frac(startHsv.x + (hueDelta * saturate(t)));
                    }

                    float3 hsv = float3(
                        hue,
                        lerp(startHsv.y, endHsv.y, t),
                        lerp(startHsv.z, endHsv.z, t));
                    color = float4(HSVToRGB(hsv), lerp(startHsv.a, endHsv.a, t));
                }
                else
                {
                    color = lerp(startColor, endColor, t);
                }

                return color;
            }

            float4 InterpolatePhysicalRibbonColor(
                float4 startColor,
                float4 endColor,
                float t,
                int colorLerpType,
                bool preserveConstantOutputPeak)
            {
                float4 color = InterpolateRibbonColor(startColor, endColor, t, colorLerpType);
                float startPeak = max(startColor.r, max(startColor.g, startColor.b));
                float endPeak = max(endColor.r, max(endColor.g, endColor.b));
                // Keep equal-brightness RGB endpoints at constant visual energy,
                // while preserving easing overshoot and intentional brightness changes.
                if (preserveConstantOutputPeak
                    && colorLerpType == 0
                    && t >= 0.0f
                    && t <= 1.0f
                    && abs(startColor.a - endColor.a) <= 0.00001f
                    && abs(startPeak - endPeak) <= 0.00001f)
                {
                    float targetPeak = lerp(startPeak, endPeak, t);
                    float mixedPeak = max(color.r, max(color.g, color.b));
                    if (mixedPeak > 0.00001f)
                    {
                        color.rgb *= targetPeak / mixedPeak;
                    }
                }

                return color;
            }

            // Ribbon opacity and white boost approximate the preview lights while keeping timeline colors
            // readable.

            // A light at brightness 100 produces 0.6 ribbon opacity. Opacity approaches 1 at higher
            // brightness.
            static const float RibbonAlphaAtLightLevel100 = 0.6f;
            // Above brightness 100, blend the ribbon toward white. The blend reaches 50% at brightness 400
            // and approaches RibbonMaximumWhiteBlend.
            static const float RibbonHalfWhiteLightLevel = 4.0f;
            // Limit the white blend to 85% so even extremely bright ribbons retain some of the light's color.
            static const float RibbonMaximumWhiteBlend = 0.85f;

            float AsymptoticRibbonAlpha(float lightLevel)
            {
                lightLevel = max(lightLevel, 0.0f);
                float scale = (1.0f - RibbonAlphaAtLightLevel100) / RibbonAlphaAtLightLevel100;
                return lightLevel / (lightLevel + scale);
            }

            float4 DisplayRibbonColor(float4 color)
            {
                float lightLevel = max(color.a, 0.0f);
                float ribbonAlpha = AsymptoticRibbonAlpha(color.a);
                // Normalize HDR hue so excess brightness can drift toward white without clipping.
                float colorPeak = max(color.r, max(color.g, color.b));
                // Clamp negative easing overshoot before display compensation.
                float3 normalizedColor = max(color.rgb / max(colorPeak, 1.0f), 0.0f);
                // Measure excess brightness above the level-100 baseline.
                float overbright = max(lightLevel - 1.0f, 0.0f);
                // Derive the scale so the half-white point stays fixed when the cap changes.
                float halfWhiteOverbright = RibbonHalfWhiteLightLevel - 1.0f;
                float whiteCurveScaleSquared = (halfWhiteOverbright * halfWhiteOverbright)
                    * ((RibbonMaximumWhiteBlend / 0.5f) - 1.0f);
                float whiteMix = RibbonMaximumWhiteBlend * (overbright * overbright)
                    / ((overbright * overbright) + whiteCurveScaleSquared);
                color.rgb = lerp(normalizedColor, 1.0f, whiteMix) * ribbonAlpha;
                color.a = 0;
                return color;
            }

            float4 DisplayLightStripColor(float4 color)
            {
                color = DisplayRibbonColor(color);
#ifndef UNITY_COLORSPACE_GAMMA
                color.rgb = float3(
                    GammaToLinearSpaceExact(color.r),
                    GammaToLinearSpaceExact(color.g),
                    GammaToLinearSpaceExact(color.b));
#endif
                color.rgb = sqrt(color.rgb);
                return color;
            }

            float4 TimelineTexel(int texel)
            {
                int width = (int)_LightDistributionTex_TexelSize.z;
                float2 pixel = float2(texel % width, texel / width) + 0.5f;
                return tex2Dlod(_LightDistributionTex,
                    float4(pixel * _LightDistributionTex_TexelSize.xy, 0.0f, 0.0f));
            }

            float EvaluateStrobeCycles(float startFrequency, float endFrequency, float duration, float progress, bool fade)
            {
                float cycles = duration * ((startFrequency * progress)
                    + (0.5f * (endFrequency - startFrequency) * progress * progress));
                if (fade && startFrequency <= 0.0f && endFrequency > 0.0f)
                    cycles -= (startFrequency + endFrequency) * duration * 0.5f;
                return cycles;
            }

            float EvaluateStrobeFade(float phase, float pulseWidth, int easing)
            {
                return phase < pulseWidth
                    ? EvaluateEasing(phase / pulseWidth, easing)
                    : 1.0f - EvaluateEasing((phase - pulseWidth) / (1.0f - pulseWidth), easing);
            }

            float StrobeDutyIntegral(float cycles, float pulseWidth)
            {
                return floor(cycles) * (1.0f - pulseWidth) + max(frac(cycles) - pulseWidth, 0.0f);
            }

            float LinearStepCoverage(float from, float to)
            {
                float span = abs(to - from);
                return span == 0.0f ? step(0.0f, from) : saturate(max(from, to) / span);
            }

            float StrobeDutyCoverage(float cycles, float cycleFootprint, float pulseWidth, float pulseWidthFootprint)
            {
                if (cycleFootprint <= 1e-4f)
                    return step(pulseWidth, frac(cycles));
                float halfFootprint = cycleFootprint * 0.5f;
                if (pulseWidthFootprint == 0.0f || cycleFootprint > 1.0f)
                {
                    return saturate((StrobeDutyIntegral(cycles + halfFootprint, pulseWidth)
                        - StrobeDutyIntegral(cycles - halfFootprint, pulseWidth)) / cycleFootprint);
                }
                float startCycles = cycles - halfFootprint;
                float endCycles = cycles + halfFootprint;
                float startPhase = frac(startCycles);
                float endPhase = frac(endCycles);
                float startPulseWidth = saturate(pulseWidth - (pulseWidthFootprint * 0.5f));
                float endPulseWidth = saturate(pulseWidth + (pulseWidthFootprint * 0.5f));
                if (floor(startCycles) == floor(endCycles))
                    return LinearStepCoverage(startPhase - startPulseWidth, endPhase - endPulseWidth);
                float wrapProgress = (1.0f - startPhase) / cycleFootprint;
                float wrapPulseWidth = lerp(startPulseWidth, endPulseWidth, wrapProgress);
                return (wrapProgress * LinearStepCoverage(startPhase - startPulseWidth, 1.0f - wrapPulseWidth))
                    + ((1.0f - wrapProgress) * LinearStepCoverage(-wrapPulseWidth, endPhase - endPulseWidth));
            }

            float4 PresentHardStrobe(float4 normalColor, float4 strobeColor, float mix)
            {
                if (mix <= 0.0f)
                    return DisplayLightStripColor(normalColor);
                if (mix >= 1.0f)
                    return DisplayLightStripColor(strobeColor);
                float4 normalPresented = DisplayLightStripColor(normalColor);
                float4 strobePresented = DisplayLightStripColor(strobeColor);
                return float4(sqrt(lerp(normalPresented.rgb * normalPresented.rgb,
                    strobePresented.rgb * strobePresented.rgb, mix)), 0.0f);
            }

            // Neighboring strips store the same tween data. PresentLightTimeline decides which ribbon draws
            // each pixel.
            float4 EvaluateLightTimeline(
                int texel,
                int stride,
                float time,
                float pixelWidth,
                bool useBoost,
                out float coverage,
                out float4 hardStrobeColor,
                out float hardStrobeMix)
            {
                float4 times = TimelineTexel(texel + 4 * stride);
                // Compare ribbon boundaries in absolute song time. Keep each tween's stored times relative to
                // preserve precision.
                times += UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                coverage = 0.0f;
                hardStrobeColor = 0.0f;
                hardStrobeMix = -1.0f;
                // An invalid interval owns no pixels, even if endpoint RGB is set.
                if (times.y <= times.x)
                    return 0.0f;
                // Fractional row-7 flags identify joins between lit ribbons, preventing both edges from
                // fading to black.
                float4 flags = TimelineTexel(texel + 7 * stride);
                float joins = frac(flags.z);
                bool joinedStart = joins >= 0.25f && (joins < 0.5f || joins >= 0.75f);
                bool joinedEnd = joins >= 0.5f;
                if (pixelWidth <= 1e-7f)
                {
                    coverage = time >= times.x && time <= times.y ? 1.0f : 0.0f;
                }
                else
                {
                    float halfPixel = pixelWidth * 0.5f;
                    coverage = saturate((min(halfPixel, times.y - time)
                        - max(-halfPixel, times.x - time)) / pixelWidth);
                    // Give the shared timestamp to the next owner to avoid gaps and double draws.
                    if (joinedStart && abs(time - times.x) <= halfPixel)
                        coverage = time >= times.x ? 1.0f : 0.0f;
                    else if (joinedEnd && abs(time - times.y) <= halfPixel)
                        coverage = time < times.y ? 1.0f : 0.0f;
                }
                if (coverage <= 0.0f)
                    return 0.0f;
                time = clamp(time, times.x, times.y);
                float4 rates = TimelineTexel(texel + 5 * stride);
                float4 brightness = TimelineTexel(texel + 6 * stride);
                float4 easings = TimelineTexel(texel + 8 * stride);
                float4 normalFrom = TimelineTexel(texel);
                float4 normalTo = TimelineTexel(texel + stride);
                float4 altStrobeFrom = 0.0f;
                float4 altStrobeTo = 0.0f;
                if (useBoost)
                {
                    int boostTextureWidth = (int)_LightDistributionTex_TexelSize.z;
                    float4 boostData = UNITY_ACCESS_INSTANCED_PROP(Props, _LightBoostData);
                    int altBase = (int)boostData.x + ((int)boostData.y * boostTextureWidth);
                    int boostLightCount = (int)UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    int mainRecordEnd = 10 * boostLightCount;
                    // Owned records index their column; each nine-texel neighbor record follows in order.
                    int recordOrdinal = texel < mainRecordEnd
                        ? texel
                        : boostLightCount + ((texel - mainRecordEnd) / 9);
                    int altTexel = altBase + (4 * recordOrdinal);
                    float4 altNormalFrom = TimelineTexel(altTexel);
                    float4 altNormalTo = TimelineTexel(altTexel + 1);
                    altStrobeFrom = TimelineTexel(altTexel + 2);
                    altStrobeTo = TimelineTexel(altTexel + 3);
                    // Match a record packed with boosted colors: raw alphas then brightness multiply or
                    // the same composed-endpoint path.
                    normalFrom = float4(altNormalFrom.rgb, altNormalFrom.a * rates.z);
                    normalTo = float4(altNormalTo.rgb, altNormalTo.a * rates.w);
                    brightness.z = altNormalFrom.a;
                    brightness.w = altNormalTo.a;
                    flags.x = altStrobeFrom.a;
                    flags.y = altStrobeTo.a;
                }
                float normalizedAlpha = saturate((time - times.x) / (times.y - times.x));
                float normalizedColor = times.w == times.z ? 0.0f : saturate((time - times.z) / (times.w - times.z));
                float alphaProgress = EvaluateEasing(normalizedAlpha, (int)easings.x);
                float strobeFlags = round(flags.w);
                bool composedEndpoints = strobeFlags >= 2.0f;
                if (!composedEndpoints)
                {
                    normalFrom.a = brightness.z;
                    normalTo.a = brightness.w;
                }
                bool preserveNormalPeak = abs((normalFrom.a * rates.z) - (normalTo.a * rates.w)) <= 0.00001f;
                float4 color = InterpolatePhysicalRibbonColor(normalFrom, normalTo,
                    EvaluateEasing(normalizedColor, (int)easings.y), (int)flags.z, preserveNormalPeak);
                if (!composedEndpoints)
                    color.a *= lerp(rates.z, rates.w, alphaProgress);
                if (rates.x > 0.0f || rates.y > 0.0f)
                {
                    float4 strobeFrom = useBoost ? float4(altStrobeFrom.rgb, 0.0f)
                        : TimelineTexel(texel + 2 * stride);
                    float4 strobeTo = useBoost ? float4(altStrobeTo.rgb, 0.0f)
                        : TimelineTexel(texel + 3 * stride);
                    strobeFrom.a = flags.x;
                    strobeTo.a = flags.y;
                    bool preserveStrobePeak = abs((flags.x * brightness.x) - (flags.y * brightness.y)) <= 0.00001f;
                    float4 strobe = InterpolatePhysicalRibbonColor(strobeFrom, strobeTo,
                        EvaluateEasing(normalizedColor, (int)easings.z), (int)flags.z, preserveStrobePeak);
                    strobe.a = lerp(flags.x * brightness.x, flags.y * brightness.y, alphaProgress);
                    float duration = times.y - times.x;
                    bool fadeEnabled = fmod(strobeFlags, 2.0f) >= 1.0f;
                    float strobeEasing = round(easings.w);
                    float startStrobePulseWidth = 0.5f + ((flags.w - strobeFlags) * 2.0f);
                    float endStrobePulseWidth = 0.5f + ((easings.w - strobeEasing) * 2.0f);
                    float strobePulseWidth = lerp(startStrobePulseWidth, endStrobePulseWidth, normalizedAlpha);
                    float cycles = EvaluateStrobeCycles(rates.x, rates.y, duration, normalizedAlpha, fadeEnabled);
                    float phase = frac(cycles);
                    if (fadeEnabled)
                    {
                        float fade = EvaluateStrobeFade(phase, strobePulseWidth, (int)strobeEasing);
                        color = InterpolateRibbonColor(color, strobe, fade, (int)flags.z);
                    }
                    else
                    {
                        hardStrobeColor = strobe;
                        hardStrobeMix = StrobeDutyCoverage(
                            cycles, abs(lerp(rates.x, rates.y, normalizedAlpha)) * pixelWidth, strobePulseWidth,
                            (endStrobePulseWidth - startStrobePulseWidth) * pixelWidth / duration);
                    }
                }
                return color;
            }

            float4 PresentTimelineRecord(int texel, int stride, float time, float pixelWidth, bool useBoost)
            {
                // Share display conversion and coverage for owned and borrowed tween records.
                float coverage;
                float4 hardStrobeColor;
                float hardStrobeMix;
                float4 color = EvaluateLightTimeline(
                    texel,
                    stride,
                    time,
                    pixelWidth,
                    useBoost,
                    coverage,
                    hardStrobeColor,
                    hardStrobeMix);
                float4 presented = hardStrobeMix >= 0.0f
                    ? PresentHardStrobe(color, hardStrobeColor, hardStrobeMix)
                    : DisplayLightStripColor(color);
                return presented * sqrt(coverage);
            }

            // Only colors sampled from another ribbon need its ownership flag.
            float4 PresentLightTimeline(
                float coordinate, float time, float pixelWidth, bool sampleNeighbors, bool useBoost,
                out bool foreignColumn)
            {
                foreignColumn = false;
                int lightCount = (int)UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                int light = clamp((int)floor(coordinate * lightCount), 0, lightCount - 1);
                // Evaluate this ribbon before borrowing colors from neighboring strips.
                float4 presented = PresentTimelineRecord(light, lightCount, time, pixelWidth, useBoost);
                // A delayed neighbor can cross several nodes. Search its sorted records only at edges.
                if (sampleNeighbors && presented.r + presented.g + presented.b <= 0.0f)
                {
                    float packedFlags = frac(TimelineTexel(light + 7 * lightCount).z);
                    bool hasNeighbors = fmod(floor(packedFlags * 64.0f), 2.0f) >= 1.0f;
                    if (hasNeighbors)
                    {
                        int lower = 0;
                        float4 neighbors = TimelineTexel(light + 9 * lightCount);
                        int upper = (int)neighbors.x;
                        int firstTexel = (int)neighbors.y + (int)neighbors.z * (int)_LightDistributionTex_TexelSize.z;
                        // Time distributions can put neighboring lights on different events. Search their
                        // sorted intervals at strip edges; most lookups find the active interval on the first
                        // pass.
                        [loop]
                        while (lower < upper)
                        {
                            int middle = (lower + upper) >> 1;
                            float start = TimelineTexel(firstTexel + 9 * middle + 4).x
                                + UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                            if (start <= time)
                                lower = middle + 1;
                            else
                                upper = middle;
                        }
                        float4 sidecarPresented = PresentTimelineRecord(
                            firstTexel + 9 * max(lower - 1, 0),
                            1,
                            time,
                            pixelWidth,
                            useBoost);
                        if (sidecarPresented.r + sidecarPresented.g + sidecarPresented.b > 0.0f)
                        {
                            presented = sidecarPresented;
                            foreignColumn = true;
                        }
                    }
                }
                return presented;
            }

            void StripCoverage(float uvY, float width, float pixelFootprint,
                out float index, out float neighbour, out float blend)
            {
                float stripPos = saturate(uvY) * width;
                index = min(floor(stripPos), width - 1.0f);
                float halfFootprint = pixelFootprint * 0.5f;
                float coverageNext = saturate((stripPos + halfFootprint - (index + 1.0f)) / pixelFootprint);
                float coveragePrev = saturate((index - (stripPos - halfFootprint)) / pixelFootprint);
                // If a pixel crosses both strip edges, blend with the neighbor covering more of it.
                blend = max(coverageNext, coveragePrev);
                neighbour = clamp(index + (coverageNext > coveragePrev ? 1.0f : -1.0f),
                    0.0f, width - 1.0f);
                if (neighbour == index)
                    blend = 0.0f;
            }

            // Integrate the full interval when a subpixel strip touches background on both sides.
            float StripIntervalCoverage(float uvY, float width, float footprint, float index)
            {
                float stripPos = saturate(uvY) * width;
                float halfFootprint = footprint * 0.5f;
                return saturate((min(stripPos + halfFootprint, index + 1.0f)
                    - max(stripPos - halfFootprint, index)) / footprint);
            }

            bool RibbonStripEmits(float4 presented)
            {
                return presented.r + presented.g + presented.b > 0.0f;
            }

            float4 BlendPresentedStrips(float4 presented, float4 neighbourPresented, float blend)
            {
                // SrcColor blending squares the source. Average the displayed
                // linear energy before taking its square root to avoid dark seams.
                return sqrt(lerp(presented * presented, neighbourPresented * neighbourPresented, blend));
            }

            float RibbonAxisCoverage(float uv, float pixelWidth)
            {
                if (pixelWidth <= 1e-7f)
                    return 1.0f;
                return saturate(0.5f + (min(uv, 1.0f - uv) / pixelWidth));
            }

            float4 ApplyRibbonEdgeCoverage(float4 presented, float2 uv, float2 pixelWidth, bool includeTimeEdge)
            {
                float coverage = RibbonAxisCoverage(uv.y, pixelWidth.y);
                if (includeTimeEdge)
                    coverage *= RibbonAxisCoverage(uv.x, pixelWidth.x);
                return presented * sqrt(coverage);
            }

            float4 PresentStrobeOverlay(
                float4 color, float4 startStrobeColor, float4 endStrobeColor, float t, float progress,
                float progressPixelWidth, int colorLerpType)
            {
                float4 strobeColor = lerp(startStrobeColor, endStrobeColor, t);
                float duration = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeDuration);
                float startFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyA);
                float endFrequency = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFrequencyB);
                bool fadeEnabled = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeFade) > 0.5f;
                float cycles = EvaluateStrobeCycles(startFrequency, endFrequency, duration, progress, fadeEnabled);
                float phase = frac(cycles);
                float trianglePhase = 1.0f - abs((phase * 2.0f) - 1.0f);
                float strobeMix;
                [branch]
                if (fadeEnabled)
                {
                    strobeMix = Cubic_InOut(trianglePhase);
                    return DisplayLightStripColor(InterpolateRibbonColor(color, strobeColor, strobeMix, colorLerpType));
                }
                else
                {
                    strobeMix = StrobeDutyCoverage(
                        cycles,
                        abs(lerp(startFrequency, endFrequency, progress)) * duration * progressPixelWidth, 0.5f, 0.0f);
                    return PresentHardStrobe(color, strobeColor, strobeMix);
                }
            }

            float4 EvaluateDistributedStrip(
                float lightCoordinate, float t, float progress, float progressPixelWidth,
                int colorLerpType, float useStrobeColors)
            {
                float4 color = lerp(
                    tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.125f, 0.0f, 0.0f)),
                    tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.375f, 0.0f, 0.0f)),
                    t);
                if (useStrobeColors > 0.5f)
                {
                    return PresentStrobeOverlay(
                        color,
                        tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.625f, 0.0f, 0.0f)),
                        tex2Dlod(_LightDistributionTex, float4(lightCoordinate, 0.875f, 0.0f, 0.0f)),
                        t,
                        progress,
                        progressPixelWidth,
                        colorLerpType);
                }

                return DisplayLightStripColor(color);
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Compute derivatives before per-pixel branches so all owners use the same footprint.
                float2 laneUv = i.uv;
                float timelineOffset = i.uv.x * UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineDuration);
                float timelineOrigin = UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _UseRibbonPlane) > 0.5f)
                {
                    float2 ndc = i.vertex.xy / _ScreenParams.xy * 2.0f - 1.0f;
                    #if UNITY_UV_STARTS_AT_TOP
                        ndc.y = -ndc.y;
                    #endif
                    float3 pixel = float3(ndc, 1.0f);
                    float inverseDenominator = rcp(dot(i.laneDenominator, pixel));
                    timelineOffset = dot(i.laneTime, pixel) * inverseDenominator;
                    timelineOrigin = _SongBpmTime.y;
                    laneUv.y = dot(i.laneWidth, pixel) * inverseDenominator;
                }
                float2 uvPixelWidth = fwidth(laneUv);
                // Use relative times so scrolling far into the song does not change subpixel coverage.
                float timelinePixelWidth = fwidth(timelineOffset);
                float timelineTime = timelineOffset + timelineOrigin;

                float index;
                float neighbour;
                float blend;
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _UseLightTimeline) > 0.5f)
                {
                    float width = UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    // Use the same one-pixel footprint for color and background
                    // boundaries so lit neighbors do not widen the edge ramp.
                    float footprint = max(uvPixelWidth.y * width, 1e-4f);
                    StripCoverage(laneUv.y, width, footprint, index, neighbour, blend);
                    float time = timelineTime;
                    float timePixelWidth = timelinePixelWidth;
                    // Resolve the boost state once per fragment so owned strips and borrowed
                    // neighbor records share it; an at-boundary sample uses the new state.
                    bool useBoost = false;
                    int boostCount = (int)UNITY_ACCESS_INSTANCED_PROP(Props, _LightBoostCount);
                    if (boostCount > 0)
                    {
                        int boostTextureWidth = (int)_LightDistributionTex_TexelSize.z;
                        float4 boostData = UNITY_ACCESS_INSTANCED_PROP(Props, _LightBoostData);
                        int boostTable = (int)boostData.z + ((int)boostData.w * boostTextureWidth);
                        float boostTimelineStart = UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                        int boostLower = 0;
                        int boostUpper = boostCount;
                        [loop]
                        while (boostLower < boostUpper)
                        {
                            int boostMiddle = (boostLower + boostUpper) >> 1;
                            if (TimelineTexel(boostTable + boostMiddle).x + boostTimelineStart <= time)
                                boostLower = boostMiddle + 1;
                            else
                                boostUpper = boostMiddle;
                        }
                        useBoost = boostLower > 0
                            ? TimelineTexel(boostTable + boostLower - 1).y > 0.5f
                            : UNITY_ACCESS_INSTANCED_PROP(Props, _LightBoostInitial) > 0.5f;
                    }
                    bool indexForeignColumn;
                    float4 presented = PresentLightTimeline(
                        (index + 0.5f) / width, time, timePixelWidth,
                        blend > 0.0f, useBoost, indexForeignColumn);
                    [branch]
                    if (blend > 0.0f)
                    {
                        bool neighbourForeignColumn;
                        float4 neighbourPresented = PresentLightTimeline(
                            (neighbour + 0.5f) / width, time, timePixelWidth,
                            true, useBoost, neighbourForeignColumn);
                        // At a lit join, only the ribbon covering the pixel center draws. An empty center may
                        // receive edge coverage from a neighboring ribbon.
                        if (indexForeignColumn)
                        {
                            presented = 0.0f;
                        }
                        else if (!RibbonStripEmits(presented) && neighbourForeignColumn)
                            presented = 0.0f;
                        else
                        {
                            // Integrate isolated narrow strips across both sides
                            // without leaking their color into another lane.
                            bool indexEmits = RibbonStripEmits(presented);
                            bool neighbourEmits = RibbonStripEmits(neighbourPresented);
                            // Check the far side only for isolated subpixel strips.
                            bool isolatedStrip = indexEmits && !neighbourEmits
                                && index > 0.0f && index < width - 1.0f;
                            if (isolatedStrip)
                            {
                                float opposite = index + (index - neighbour);
                                if (StripIntervalCoverage(laneUv.y, width, footprint, opposite) > 0.0f)
                                {
                                    float4 oppositeTimes = TimelineTexel((int)opposite + 4 * (int)width)
                                        + UNITY_ACCESS_INSTANCED_PROP(Props, _LightTimelineStart);
                                    isolatedStrip = !(oppositeTimes.y > oppositeTimes.x
                                        && time >= oppositeTimes.x && time <= oppositeTimes.y);
                                }
                            }
                            if (isolatedStrip)
                            {
                                presented *= sqrt(StripIntervalCoverage(laneUv.y, width, footprint, index));
                            }
                            else
                            {
                                presented = BlendPresentedStrips(presented, neighbourPresented, blend);
                            }
                        }
                    }
                    return ApplyRibbonEdgeCoverage(presented, laneUv, uvPixelWidth, false);
                }

                float progress = i.uv.x;
                float t = EvaluateEasing(progress, UNITY_ACCESS_INSTANCED_PROP(Props, _EasingID));
                int colorLerpType = UNITY_ACCESS_INSTANCED_PROP(Props, _UseHSV);
                float useStrobeColors = UNITY_ACCESS_INSTANCED_PROP(Props, _UseStrobeColors);
                float useLightDistribution = UNITY_ACCESS_INSTANCED_PROP(Props, _UseLightDistribution);
                [branch]
                if (useLightDistribution > 0.5f)
                {
                    float lightWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _LightDistributionWidth);
                    StripCoverage(i.uv.y, lightWidth, max(uvPixelWidth.y * lightWidth, 1e-4f), index, neighbour, blend);
                    float progressPixelWidth = uvPixelWidth.x;
                    float4 presented = EvaluateDistributedStrip(
                        (index + 0.5f) / lightWidth, t, progress, progressPixelWidth,
                        colorLerpType, useStrobeColors);
                    [branch]
                    if (blend > 0.0f)
                    {
                        float4 neighbourPresented = EvaluateDistributedStrip(
                            (neighbour + 0.5f) / lightWidth, t, progress, progressPixelWidth,
                            colorLerpType, useStrobeColors);
                        presented = BlendPresentedStrips(presented, neighbourPresented, blend);
                    }

                    return ApplyRibbonEdgeCoverage(presented, i.uv, uvPixelWidth, true);
                }

                // Uniform ribbons use material endpoints while distributed strips read texture endpoints.
                float4 color = InterpolateRibbonColor(
                    UNITY_ACCESS_INSTANCED_PROP(Props, _ColorA),
                    UNITY_ACCESS_INSTANCED_PROP(Props, _ColorB),
                    t, colorLerpType);
                [branch]
                if (useStrobeColors > 0.5f)
                {
                    color = PresentStrobeOverlay(
                        color,
                        UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorA),
                        UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorB),
                        t,
                        progress,
                        uvPixelWidth.x,
                        colorLerpType);
                }

                return ApplyRibbonEdgeCoverage(useStrobeColors > 0.5f
                    ? color : DisplayLightStripColor(color), i.uv, uvPixelWidth, true);
            }
            ENDHLSL
        }
    }
}
