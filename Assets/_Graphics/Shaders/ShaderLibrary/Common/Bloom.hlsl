#ifndef CHROMAPPER_BLOOM_INCLUDED
#define CHROMAPPER_BLOOM_INCLUDED

// Main-effect white boost is camera-global. Do not expose these values in a
// shader Properties block because material values override Shader globals.
float _BaseColorBoost;
float _BaseColorBoostThreshold;

// Lit main-effect white-boost term, shared with the simple-shader white-boost routes:
// whiteBoost = (bloomValue * whiteboostMultiplier)^2 * baseColorBoost - baseColorBoostThreshold
inline float CalculateWhiteBoost(float bloomValue, float whiteboostMultiplier,
                                 float baseColorBoost, float baseColorBoostThreshold)
{
    float whiteBoost = bloomValue * whiteboostMultiplier;
    return whiteBoost * whiteBoost * baseColorBoost - baseColorBoostThreshold;
}

// Bloom composition: premultiplied color plus the Lit white-boost term, shared by
// the Deferred and Mixed bloom types (and the selector-free Opaque/Transparent route).
// bloomValue drives the white boost; premultiplyAlpha scales the color (pass 1
// for the game's additive, alpha-preserving routes).
// rgb = saturate(rgb * premultiplyAlpha
//                + (bloomValue * whiteboostMultiplier)^2 * baseColorBoost
//                - baseColorBoostThreshold)
inline float3 CalculateBloomComposition(float3 rgb, float premultiplyAlpha, float bloomValue,
                                        float whiteboostMultiplier, float baseColorBoost,
                                        float baseColorBoostThreshold)
{
    float whiteBoost = CalculateWhiteBoost(bloomValue, whiteboostMultiplier,
                                           baseColorBoost, baseColorBoostThreshold);
    return saturate(rgb * premultiplyAlpha + whiteBoost);
}

// Post-process bloom route (game: MAIN_EFFECT_ENABLED on, ChroMapper global
// POST_BLOOM on): the post-process bloom provides the glow, so the white-boost
// term compiles out. Plain premultiplied composition with alpha scaled by the
// bloom multiplier (pass 1 when the material has no multiplier slot).
inline float4 CalculateBloomPostComposition(float3 rgb, float alpha, float bloomMultiplier)
{
    return float4(rgb * alpha, alpha * bloomMultiplier);
}

#endif
