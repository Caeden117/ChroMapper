#ifndef GRID_COVERAGE_INCLUDED
#define GRID_COVERAGE_INCLUDED

// Related technique: Ben Golus's PristineGrid (thin-line fading and dense-grid averaging).
// https://bgolus.medium.com/the-best-darn-grid-shader-yet-727f9278b9d8
// https://gist.github.com/bgolus/d49651f52b1dcf82f70421ba922ed064
// This variant uses an area-normalized kernel with a width-dependent ramp and flat top.

// Use a 1–1.5 pixel ramp to smooth diagonals without leaving thin lines too soft.
// Give resolved lines a flat top to reduce brightness changes at pixel boundaries.
void GridLineKernel(float halfWidth, float filterWidth, out float plateau, out float ramp)
{
    float fw = max(filterWidth, 1e-7);
    ramp = clamp(halfWidth + fw * 0.5, fw, fw * 1.5);
    float resolved = saturate(2.0 * halfWidth / fw - 1.0);
    plateau = max(halfWidth - ramp * 0.5, fw * 0.75 * resolved);
}

float GridLineCoverageAtDistance(float dist, float halfWidth, float filterWidth)
{
    float plateau, ramp;
    GridLineKernel(halfWidth, filterWidth, plateau, ramp);
    // Preserve line area as the kernel widens, so subpixel lines fade.
    float height = min(1.0, (2.0 * halfWidth) / (2.0 * plateau + ramp));
    return height * (1.0 - smoothstep(0.0, ramp, max(dist - plateau, 0.0)));
}

float GridLineCoverage(float pos, float spacing, float halfWidth, float filterWidth)
{
    float m = abs(pos) % spacing;
    float dist = min(m, spacing - m);
    float lineCov = GridLineCoverageAtDistance(dist, halfWidth, filterWidth);
    float plateau, ramp;
    GridLineKernel(halfWidth, filterWidth, plateau, ramp);
    // Blend to average coverage as the kernel starts overlapping adjacent cells.
    float dense = saturate((plateau + ramp) / spacing);
    return lerp(lineCov, saturate(2.0 * halfWidth / spacing), dense);
}

// Fade at +/- edge on a quad spanning [-0.5, 0.5]. Set reach to the kernel's half-extent
// in coord units for boundary-aligned lines, or 0 for lines ending at the boundary.
// Finish the fade inside the quad to avoid hard clipping at grazing angles.
float GridEdgeMask(float coord, float edge, float reach, float filterWidth)
{
    float boundary = min(edge + reach, 0.5 - filterWidth);
    return 1.0 - smoothstep(0.0, filterWidth, abs(coord) - boundary);
}
#endif
