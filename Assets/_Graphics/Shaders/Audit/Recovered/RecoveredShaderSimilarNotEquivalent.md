# Similar, Not Equivalent

## Scope decision

The evidence-based catalog contains 26 shaders. It is the authoritative recovered scope.

The behavior scan contains 29 candidates because it uses a broader evidence rule. The three extra candidates are adjacent and unverified for catalog membership:

- `GrabPassTexture1.shader` has binary evidence for a helper role.
- `Post Process/BlitBlendColor.shader` has binary evidence for a hidden replacement role.
- `Post Process/ChromaticAberration.shader` is a derived split from a combined bloom corpus. No standalone source export joins this split.

These three shaders do not become recovered catalog entries. Their behavior records remain available with an `adjacent_unverified` scope status.

## Result

The review contains 17 `SEMANTICALLY_SIMILAR_NOT_SAFE` records. These records are do-not-merge evidence.

## 1. `environment.json/cross_shader_classification/1`

- Source: environment.json
- Json Path: /cross_shader_classification/1
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: CloudsOpaque:251 ordinary fog lerp(color,0.1,1-hFade) vs Spectrogram:183 lerp(0.1,rgb,heightRetained); CLOUD_REAUDIT corrected opaque vs transparent behavior sections; Spectrogram S4 height-fog always evaluated.
- Regions: fragment ordinary (non-bloom) fog fallback

## 2. `environment.json/cross_shader_classification/11`

- Source: environment.json
- Json Path: /cross_shader_classification/11
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: WaterLit:320-340 TBN path (UnpackNormal, detail lerp by _DetailNormalIntensity, falling scale 1+_NormalScaleVertical*(1-worldNormal.y), lerp by _NormalScale) vs Mirror:203-220 screen-space bump path (red*=alpha, XY unpack, z=sqrt(1-min(dot,1)), detail UV tiles _DetailNormalTextureScale, reflectionUV offset by viewY). Same DETAIL_NORMAL_MAP keyword name, different spaces.
- Helpers: UnpackNormal (UnityCG)
- Regions: fragment normal + detail-normal mapping

## 3. `environment.json/cross_shader_classification/13`

- Source: environment.json
- Json Path: /cross_shader_classification/13
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: Same uniform name _TimeHelperOffset with different lanes per shader (see blockers); same _Color name with different ownership (see blockers); _FogStartOffset/_FogScale reused across incompatible fog formulas; _NoiseTex means world-noise (CloudsOpaque) vs lightning displacement noise (Lightning) vs normal map (WaterLit/Mirror)
- Regions: uniform/property name reuse (not behavior reuse)

## 4. `includes.json/includes/7/operations/1`

- Source: includes.json
- Json Path: /includes/7/operations/1
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: medium-high (formulae match per-component; dispatch behavior provably differs)
- Evidence: Each manual site reproduces one GetTime component for the standard branch only. None honors _CUSTOM_TIME_FREEZE/_CUSTOM_TIME_SONG_TIME; several omit _TimeHelperOffset entirely (Arc, ObstacleDistortion) or use a different offset source (Rain).
- Call Sites: see contract_mismatches list
- Name: Manual _Time(+helper)(+offset) component reads (all non-Time.hlsl shaders)
- Operation Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Signature: varies: _Time.x|_Time.y|_Time.z + _TimeHelperOffset.{x,z} | _TimeOffset | raw _Time.y

## 5. `particles.json/regions/18`

- Source: particles.json
- Json Path: /regions/18
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: L1178-1221; reaudit R6 (0.1 + unscaled offset) + R9 (signed period).
- Behavior: Distortion applied to BASE uv BEFORE wrapping/sampling so flipbook inherits it (frag order comment). Layered MASK&&MASK2: sample=tex2D(_DistortionTex,layeredDistortionUv).rg; uv+=sample*(Props._DistortionStrength*0.1)*_DistortionAxes.xy*2-1 (frag db5d6342: -1 NOT scaled). Single: identical with i.distortionUv. CUSTOM_WRAPPING (frag 641ba0cb, R9 signed period): pad=_CustomPadding+1; biased=pad*10+uv; prod=pad*biased; period=per-axis pad or -pad by sign(prod.x>=-prod.x); uv=frac(biased/period)*period (Clamp-wrapped source texture required). PIXELATE (frag): uv=floor(uv*_PixelateResolution)/_PixelateResolution (selects before distortion? code: pixelate branch REPLACES uv, skipping distortion+wrap — L1180-1184: pixelate uv used directly, distortion/wrap inside #else of non-flipbook? Actually PIXELATE selects uv at L1180, distortion block L1186 still applies to `uv`? No: L1184 uv=i.uv.xy then distortion modifies uv — PIXELATE L1181 uv=floor... then L1186 distortion STILL applies (uv+=...). Wrapping L1205 applies to distorted uv in both pixelate/non-pixelate (wrap inside #if MAIN_TEXTURE after distortion, outside pixelate if/else? L1205 is inside #if MAIN_TEXTURE after distortion #endif, before #endif of MAIN_TEXTURE — yes applies to pixelated+distorted uv).
- Guards: DISTORTION_SIMPLE (both; frag apply); CUSTOM_WRAPPING (frag); PIXELATE (frag); MASK&&MASK2 selects layered coord.
- Id: P-MAIN-SAMPLE-UV
- Lines: 1178-1221
- Operation Signatures: uv += tex2D(_DistortionTex,distUv).rg * (Props._DistortionStrength*0.1) * _DistortionAxes.xy * 2 - 1 (note: -1 unscaled, R6); customPadding=_CustomPadding+1; biasedUv=pad*10+uv; period=sign-select(pad); uv=frac(biased/period)*period (R9); uv=floor(uv*_PixelateResolution)/_PixelateResolution
- Rgb Alpha: UV-domain only; determines which texels feed RGB/alpha next.
- Sampling: Distortion tex .rg; wrap is pure math (no sample); pixelate quantizes.
- Sharing Candidates: Lit/Note distortion+wrap idioms look similar but 0.1/unscaled-1/signed-period/order (distort->wrap->sample, flipbook inherits) differ — SEMANTICALLY_SIMILAR_NOT_SAFE, do not hoist
- Sharing Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Spaces: Base-uv space (main/atlas) perturbed then wrapped.
- Stage: frag
- Time: Distortion uv already time-panned in vert (0.1 factor).
- Title: Distortion-first + signed custom-wrap + pixelate (R6/R9)

## 6. `particles.json/regions/19`

- Source: particles.json
- Json Path: /regions/19
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: L1237-1281; reaudit Pyro 04ac3ff0 + _BaseLayer RGB-only note + early-clip order notes.
- Behavior: Non-flipbook main paths: TEXTURE_COLOR => full RGBA: sample=tex2D[Bias](_MainTex,uv) (tex2Dbias iff MIPMAP_BIAS, Pyro sampling-only R); blue=sample.b; rgb*=sample.rgb*_BaseLayer (RGB-ONLY scaling); alpha*=sample.r iff _ALPHACHANNEL_RED else sample.a; early alpha-clip compares sample-channel*color.a vs _Cutout BEFORE masks/fades/fog/dither/alpha-processing (frags 953990a3/40d13975 family). Non-TEXTURE_COLOR => alpha-only: same sample/LOD/clip but NO rgb multiply (final premult applies alpha once; comment L1273). Flipbook path (L1222-1236): dot-channel value (see P-FLIPBOOK-PACKED). _BaseLayer never touches alpha. MIPMAP_BIAS affects MainTex ONLY (info box).
- Guards: TEXTURE_FLIPBOOK (both; frag dot); TEXTURE_COLOR (frag); _ALPHACHANNEL_RED (frag); MIPMAP_BIAS (both); _CUTOUTTYPE_ALPHA_CLIP (frag early compares).
- Id: P-MAIN-SAMPLE
- Lines: 1222-1281
- Operation Signatures: tex2Dbias(_MainTex,float4(uv,0,_MipmapBias)) iff MIPMAP_BIAS else tex2D(_MainTex,uv); albedo.rgb*=tex.rgb*_BaseLayer (TEXTURE_COLOR only; RGB-only); albedo.a*=tex.r | tex.a (RED vs ALPHA channel); if (sampleChan*color.a < _Cutout) discard (early, iff ALPHA_CLIP)
- Rgb Alpha: Core RGB-vs-alpha split: TEXTURE_COLOR adds rgb; alpha-only defers rgb to premult; BaseLayer RGB-only is a sharing hazard if copied as full multiply.
- Sampling: MainTex with optional bias; flipbook dot reuses same texture.
- Sharing Candidates: Unlit/Parametric main-sample look similar but BaseLayer-RGB-only + early-clip-before-everything + flipbook-dot make verbatim reuse unsafe — SEMANTICALLY_SIMILAR_NOT_SAFE
- Sharing Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Spaces: Distorted/wrapped/atlas uv.
- Stage: frag
- Time: N/A (uv pre-panned).
- Title: Main sample paths: texture-color / alpha-only / mipmap-bias (Pyro)

## 7. `particles.json/regions/23`

- Source: particles.json
- Json Path: /regions/23
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: L1416-1454 + L1523-1526 + L1559-1562 + L1569-1572; reaudit Queen + R19.
- Behavior: Three dither slots + hologram (R19, Queen f6b512c4 post-color-fog order): (a) default pre-hologram iff NOISE && !CLIP && !AFTER_COLOR_FOG && !BLOOM_FOG && !HOLOGRAM: albedo=ApplyNoiseDither(albedo,noiseScreenPos,_GlobalBlueNoiseTex) (noise.r-0.5)/255 added pre-bloom, pre-premult. (b) HOLOGRAM (frag 26e48514): lp=worldPos-objectTranslation; hTime=time.w; bandIn=min(frac((hTime+lp.y*0.99)*0.2)*2,1); bandS=min(bandIn*20,1); band=smooth(bandS)*(1-bandIn); gridArg=lp*3-hTime*(0,1,0); grid=sin(frac(x)*π)*sin(frac(y)*π)*sin(frac(z)*π)*1.2^3; wave=cos(2hTime+lp.x+lp.y-7lp.z)*0.4+0.8; rgb+=band*(band+grid*wave)*_HologramColor.rgb. (c) post-hologram dither iff HOLOGRAM+NOISE && !CLIP && !AFTER && !BLOOM_FOG. (d) AFTER_COLOR_FOG slot (PARTICLES_DITHER_AFTER_COLOR_FOG: NOISE+_FOGTYPE_COLOR+HEIGHT_FOG && !BLOOM && !COLOR_BY_FOG && !CLIP): dither AFTER non-bloom COLOR fog. (e) BLOOM_FOG slot: dither inside bloom-fog AFTER fog, before whiteboost. (f) CLIP slot: dither first in deferred tail. Non-bloom dither is AFTER hologram composition (R19).
- Guards: NOISE_DITHERING (both); HOLOGRAM (frag); BLOOM_FOG; _FOGTYPE_COLOR+HEIGHT_FOG; COLOR_BY_FOG; _CUTOUTTYPE_ALPHA_CLIP; PARTICLES_DITHER_AFTER_COLOR_FOG (derived).
- Id: P-DITHER-HOLO
- Lines: 1416-1454, 1523-1526, 1559-1562, 1569-1572
- Operation Signatures: inline float4 ApplyNoiseDither(float4 result, float4 noiseScreenPosition, sampler2D globalBlueNoiseTex); inline float4 BuildNoiseScreenPosition(...) (vert L959); hologram band/grid/wave formula set (see behavior; hTime=time.w, lp world-relative)
- Rgb Alpha: Dither adds RGB noise (1/255); hologram adds RGB; both pre-premult (bloom input includes them). Position relative to fog/premult is load-bearing.
- Sampling: Blue-noise tex via noiseScreenPos (XR eye dims).
- Sharing Candidates: Lit/Mirror/WaterLit/Parametric dither+hologram look similar but slot order (post-color-fog vs pre/post-hologram vs inside-bloom-fog) + world-relative anchor + time.w differ — SEMANTICALLY_SIMILAR_NOT_SAFE
- Sharing Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Spaces: Screen (noise) + world-relative (hologram lp).
- Stage: frag (pos built vert)
- Time: time.w hologram only.
- Title: Dither slots + hologram order (Queen f6b512c4, R19)

## 8. `particles.json/regions/24`

- Source: particles.json
- Json Path: /regions/24
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: L1456-1505; reaudit R20 (+FOG_COLOR_HIGHLIGHT executable note).
- Behavior: COLOR_BY_FOG (R20 particle-specific prepass composition; FOG_COLOR_HIGHLIGHT executable, helper matches represented frag formula): masked-primary path (iff _FOG_MASK_SOURCE_PRIMARY_MASK+MASK+_FOGTYPE_ALPHA+FOG_COLOR_HIGHLIGHT): maskedInfluence=maskValue*_ObstacleColorInfluence; BLOOM_FOG subpath: prepassRgb=SampleBloomPrePass.rgb; particleRgb=prepassRgb*_ObstacleFogMultiplier; bloomMax=max channels; highlight=((max^3)*_ObstacleFogHighlightMultiplier capped by max-factor? code: highlight=min(highlight*bloomMax,_ObstacleFogMax) with cubic build); particleRgb=min(particleRgb*(1+highlight),max); rgb=mad(rgb,maskedInfluence,particleRgb) (mask scales ONLY base-color influence, not alpha/highlight). Non-bloom subpath: mult=min(1e-4*_ObstacleFogMultiplier,_ObstacleFogMax); highlight=min(0.1*_ObstacleFogHighlightMultiplier*(1+mult),max); rgb=mad(rgb,maskedInfluence,highlight); +height: a*=1-HeightClear iff HEIGHT_FOG. Fallback (else): unfogged=albedo; colorFog=ApplyColorFog(albedo,worldPos,_ObstacleFogMultiplier,_ObstacleFogMax,_ObstacleFogHighlightMultiplier,_ObstacleColorInfluence,_FogHeightScale,_FogHeightOffset); albedo=lerp(unfogged,colorFog,saturate(maskValue)) iff PRIMARY_MASK+MASK else colorFog.
- Guards: COLOR_BY_FOG (frag); _FOG_MASK_SOURCE_PRIMARY_MASK; MASK; _FOGTYPE_ALPHA; FOG_COLOR_HIGHLIGHT; BLOOM_FOG; HEIGHT_FOG.
- Id: P-COLORBYFOG
- Lines: 1456-1505
- Operation Signatures: inline float4 ApplyColorFog(...) (fallback; resolvedMult=min(1e-4*mult,max); highlight=...); mad(albedo.rgb, maskedInfluence, particleFogRgb) (masked prepass path; mask scales base influence only, R20); SampleBloomPrePass(i.screenPos).rgb (bloom subpath)
- Rgb Alpha: RGB-dominant (mad composition) + conditional alpha height gate in non-bloom highlight path. Primary-mask semantics are the hazard (influence-only vs full lerp).
- Sampling: Prepass rgb sample in bloom subpath.
- Sharing Candidates: Lit ApplyColorFog call looks identical but particle mad()+mask-influence-only + bloom-cubed highlight + height-alpha coupling differ — SEMANTICALLY_SIMILAR_NOT_SAFE
- Sharing Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Spaces: worldPos (height) + screenPos (prepass).
- Stage: frag
- Time: N/A.
- Title: Color-by-fog prepass composition (R20) + ApplyColorFog fallback

## 9. `particles.json/regions/25`

- Source: particles.json
- Json Path: /regions/25
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: high
- Evidence: L605-617 + L1507-1532; reaudit R7/R13 + PRECISE_FOG per-fragment note.
- Behavior: Non-bloom height-ONLY fog (FOG && HEIGHT_FOG && !BLOOM_FOG && !COLOR_BY_FOG; distance fog NOT present in these variants — supplied only by BLOOM_FOG path): clear=CalculateParticleHeightFogClearFactor(worldPos) (1-smoothstep; PRECISE_FOG selects per-fragment exact curve, else shared CalculateHeightFogFactor); alphaFactor=1-clear; LERP (R7): rgb=lerp(rgb,0.1,clear) RGB-ONLY (alpha preserved), boostAtten=whiteBoostFogFactor=alphaFactor attenuates whiteboost input separately; COLOR: rgb*=0.1 + a*=alphaFactor (+dither-after iff AFTER_COLOR_FOG slot); ALPHA: a*=alphaFactor (premult scales rgb once downstream). Source 0.1 fog color retained.
- Guards: FOG-derived + HEIGHT_FOG + _FOGTYPE_LERP|COLOR|ALPHA (frag) + PRECISE_FOG (frag) + !BLOOM_FOG + !COLOR_BY_FOG.
- Id: P-NONBLOOM-FOG
- Lines: 605-617, 1507-1532
- Operation Signatures: inline float CalculateParticleHeightFogClearFactor(float3 worldPosition) (PRECISE branch exact curve vs CalculateHeightFogFactor(heightInput)); LERP: albedo.rgb=lerp(rgb,0.1,clear); whiteBoostFogFactor=1-clear (R7); COLOR: rgb*=0.1; a*=1-clear | ALPHA: a*=1-clear
- Rgb Alpha: LERP preserves alpha (RGB-only) but attenuates boost input; COLOR/ALPHA gate alpha; premult downstream scales rgb once.
- Sampling: N/A.
- Sharing Candidates: Generic ApplyBloomFog* wrappers look similar but distance-absent + 0.1 + RGB-only-LERP + separate boost attenuation differ — SEMANTICALLY_SIMILAR_NOT_SAFE
- Sharing Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Spaces: worldPos.y altitude (+ interpolated vs per-fragment distance for bloom twin).
- Stage: frag (+vert interpolation of bloomFogDistanceSquared for BLOOM twin)
- Time: N/A.
- Title: Non-bloom height-only fog LERP/COLOR/ALPHA (R7)

## 10. `post_bloom.json/include_classifications/6`

- Source: post_bloom.json
- Json Path: /include_classifications/6
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Evidence: PostBloom dither = (SampleLevel(_GlobalBlueNoiseTex,(uv+(0.1,0.2))*params+random,0).r-0.5)/255 added to bloom BEFORE scene addition and fade multiply; PostProcess helper = (tex2D(noiseTex,noisePos.xy/ww).r-0.5)/255 added to result.rgb AFTER branch with no fade interaction. Different UV construction, LOD path, and composition order.
- Operation: PostBloom inline dither vs ApplyNoiseDither
- Users: PostBloom.shader FragMainEffect; BloomfogSkybox.shader frag
- Note: do-not-merge

## 11. `post_bloom.json/include_classifications/7`

- Source: post_bloom.json
- Json Path: /include_classifications/7
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Evidence: BloomAlphaGate rgb*=saturate(a*k) with k=_BloomParams.z (runtime threshold default 4, material _BloomThreshold in bake) applied to downsampled pyramid texel pre-pyramid; PostBloom whiteBoost=(mean4(a)*0.25)^2-style squared average * _BaseColorBoost - threshold applied to scene center in compositor; BloomfogMesh alpha is a^2 premultiply chain with u0 fog attenuation. Three distinct alpha semantics at three pipeline stages.
- Operation: bloom alpha handling (gate vs white-boost vs fog-premultiply)
- Users: Bloom.shader passes 0,1; PostBloom.shader FragMainEffect; BloomfogMesh.shader frag
- Note: do-not-merge

## 12. `post_bloom.json/include_classifications/8`

- Source: post_bloom.json
- Json Path: /include_classifications/8
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Evidence: Blurs.kawase (9-tap center+diagonal+axis, rgb-only, /=9, alpha untouched=0) and Blurs.box (NxN loop, rgb-only) resemble pyramid filters numerically but drop alpha and use legacy samplers; pyramid path preserves float4 alpha end-to-end because downstream alpha-gate/boost math depends on it.
- Operation: kawase/box vs pyramid kernels
- Users: (no live users)
- Note: do-not-merge with pyramid helpers

## 13. `scope_remaining.json/behavior_rows/10`

- Source: scope_remaining.json
- Json Path: /behavior_rows/10
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Evidence: Assets/_Graphics/Shaders/Object/Arc.shader:102-183; Assets/_Graphics/Shaders/Object/Arc.shader:135-150; Assets/_Graphics/Shaders/README.md:40-44; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/SliderNoteCrossedStrips/_shader.json
- Behavior: Line/arc adapter samples the project arc texture alpha, builds an across-strip edge fade, applies recovered bloom composition and optional fog, and adds editor distance fade.
- Id: ARC
- Stage: vertex+fragment
- Shader Path: Assets/_Graphics/Shaders/Object/Arc.shader
- Comparison Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Limits: The game mesh stores its edge coordinate in TEXCOORD1.w; the adapter uses uv.y because LineRenderer input is different.

## 14. `scope_remaining.json/behavior_rows/13`

- Source: scope_remaining.json
- Json Path: /behavior_rows/13
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Evidence: Assets/_Graphics/Shaders/Object/ObstacleOutline.shader:1-15; Assets/_Graphics/Shaders/PARAMETRIC_REAUDIT.md:37-50; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Custom/ParametricBoxFrameHD/_shader.json
- Behavior: Face-local cube outline selects dimensions from face normals, scales instanced size/color, performs optional cutout, and composes source-style bloom fog/white boost.
- Id: OBST-OUTLINE
- Stage: vertex+fragment
- Shader Path: Assets/_Graphics/Shaders/Object/ObstacleOutline.shader
- Comparison Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Limits: The source audit explicitly says this is an editor adapter, not an ObstacleCore replacement; FrameHD/LW bindings remain unresolved.

## 15. `scope_remaining.json/behavior_rows/21`

- Source: scope_remaining.json
- Json Path: /behavior_rows/21
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Evidence: Assets/_Graphics/Shaders/Post Process/ChromaticAberration.shader:1-7; Assets/_Graphics/Shaders/Post Process/ChromaticAberration.shader:72-101; /home/kival/beat-saber-shaders-1.44.3/by-shader/Shaders/Hidden/PostProcessing/Bloom/_shader.json
- Behavior: Radial end-point displacement chooses 3..16 samples and weights source samples by a default RGB spectral LUT; screen-space stereo transform is applied.
- Id: PP-CHROMA
- Stage: vertex+fragment
- Shader Path: Assets/_Graphics/Shaders/Post Process/ChromaticAberration.shader
- Comparison Class: SEMANTICALLY_SIMILAR_NOT_SAFE
- Limits: Evidence is a related combined PPv2 Uber/Bloom corpus, not a standalone recovered shader asset; the project split and runtime producer are not binary-join proven.

## 16. `parametric_object.json`

- Source: parametric_object.json
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: HIGH
- Evidence: PSB inverse ramp, opaque/transparent multiplication, Note channel-selecting fog, Arc joint route, and distortion screen-grab fog have incompatible direction/order/channels.
- Operation: Parametric/object fog composition

## 17. `parametric_object.json`

- Source: parametric_object.json
- Classification: SEMANTICALLY_SIMILAR_NOT_SAFE
- Confidence: HIGH
- Evidence: Probe encoding, LOD mapping, projection, fog scaling, and source-alpha independence differ.
- Operation: Single-cube Note reflection vs dual-packed-probe transparent reflection
