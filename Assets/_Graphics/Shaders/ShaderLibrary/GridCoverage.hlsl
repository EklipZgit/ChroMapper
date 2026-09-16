#ifndef GRID_COVERAGE_INCLUDED
#define GRID_COVERAGE_INCLUDED

// GridLineCoverageAA replaces the binary "mod() <= thickness" edge test with analytic coverage.
// filterWidth is the pixel footprint along the line gradient (fwidth of the driving coordinate).
// Resolved lines keep a crisp ~1px box-filter edge ramp. A pure box filter still emits a
// hard-edged band for sub-pixel lines (coverage steps 0 -> 2*halfWidth/filterWidth wherever the
// line center lands inside a pixel), which stair-steps along diagonals, so sub-pixel lines blend
// to a tent falloff with the same peak coverage that fades smoothly to zero across the footprint.
// When the cell spacing itself shrinks toward the footprint, coverage converges to the duty-cycle
// average (2*halfWidth/spacing) so dense lines melt into a uniform faint field instead of
// shimmering between on/off pixels. The 1e-7 floor keeps degenerate zero footprints from NaN-ing.
float GridLineCoverageAtDistance(float dist, float halfWidth, float filterWidth)
{
    float halfFilter = max(filterWidth * 0.5, 1e-7);
    float thinness = saturate(halfWidth / halfFilter);
    float box = saturate((min(dist + halfFilter, halfWidth) - max(dist - halfFilter, -halfWidth))
        / (halfFilter * 2.0));
    float tent = thinness * saturate(1.0 - dist / (halfFilter * 2.0));
    return lerp(tent, box, thinness);
}

float GridLineCoverage(float pos, float spacing, float halfWidth, float filterWidth)
{
    float m = abs(pos) % spacing;
    float halfFilter = max(filterWidth * 0.5, 1e-7);
    float thinness = saturate(halfWidth / halfFilter);
    // Tent contributions from the lines on both cell edges, so pixels sitting between two
    // near-pixel-sized cells still collect partial coverage instead of dropping to zero.
    float tent = thinness * (saturate(1.0 - m / (halfFilter * 2.0))
        + saturate(1.0 - (spacing - m) / (halfFilter * 2.0)));
    float dense = saturate(filterWidth / spacing);
    float thin = lerp(tent, saturate(halfWidth * 2.0 / spacing), dense);
    float dist = min(m, spacing - m);
    float box = saturate((min(dist + halfFilter, halfWidth) - max(dist - halfFilter, -halfWidth))
        / (halfFilter * 2.0));
    return lerp(thin, box, thinness);
}
#endif
