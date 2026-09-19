#ifndef GRID_COVERAGE_INCLUDED
#define GRID_COVERAGE_INCLUDED

// GridLineCoverageAA replaces the binary "mod() <= thickness" edge test with analytic coverage.
// The kernel is a trapezoid in pixel space: a flat plateau plus a smoothstep ramp, with its
// height scaled so the covered area always equals the true line width 2*halfWidth.
//
// The ramp tracks the line width: clamp(halfWidth + 0.5px, 1px, 1.5px). A resolved diagonal
// line genuinely shifts a pixel column every few rows, so it keeps the 1.5px feather that
// spreads each step across ~3 pixels. As the line thins below ~1px the kernel collapses to a
// 1px-halfwidth tent — never wider — so distant lines thin to one pixel and then fade by
// alpha instead of staying a wide soft band. A 1px tent is also a perfect partition of unity
// under point sampling (smoothstep satisfies s(t) + s(1-t) = 1), so a subpixel line's sampled
// energy cannot wobble as it drifts across pixel centers.
//
// The plateau floor fades in only once the line is resolved (>= ~1px wide): a peaked kernel
// is brightest exactly when it straddles a pixel boundary, which reads as an inverted
// sawtooth on near-vertical lane lines, so resolved lines keep a >=0.5px flat top covering
// both straddled pixels at equal height. Subpixel lines get no floor — their correct coverage
// at a straddle IS two half-lit pixels, and a plateau there would re-invert the gradient.
//
// When the cell spacing itself shrinks toward the kernel footprint, coverage converges to the
// duty-cycle average (2*halfWidth/spacing) so dense lines melt into a uniform faint field
// instead of shimmering between on/off pixels. The 1e-7 floor keeps zero footprints from NaN-ing.
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
    float dense = saturate((plateau + ramp) / spacing);
    return lerp(lineCov, saturate(2.0 * halfWidth / spacing), dense);
}

// Soft clip against a quad boundary: coord is the object-space axis whose true edge sits at
// ±edge (quad spans [-0.5, 0.5]), faded over ~1 pixel. Line families running perpendicular
// to the edge pass reach = their own kernel half-extent (converted to coord units) so a
// silhouette-coincident line keeps its outer ramp; parallel families pass reach = 0 so their
// endpoints stop flush at the true edge instead of bleeding into the overdraw margin.
// The boundary is clamped so the ~1px fade always COMPLETES inside the quad: the overdraw
// margin (0.5 - edge) is a fixed world-space strip, but reach/filterWidth are screen-space —
// at extreme foreshortening the margin shrinks below one pixel and an unclamped boundary
// pushes the fade past the silhouette, hard-clipping the line all over again.
float GridEdgeMask(float coord, float edge, float reach, float filterWidth)
{
    float boundary = min(edge + reach, 0.5 - filterWidth);
    return 1.0 - smoothstep(0.0, filterWidth, abs(coord) - boundary);
}
#endif
