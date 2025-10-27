## Quick Guide

### Shader & Material

ChroMapper uses alpha instead of emissive for bloom,
this means alpha channel should only be used to create glow effect.
If you need to use alpha transparency,
try to use dithered instead or use color blending.

### Render Queue

* 2000 | Opaque
    * Common constant for every opaque mesh/material, even for dithered
* 3000 | Transparent
    * Common constant, try to have every transparency material rendered on same render queue to avoid graphical oddity
* 3000+ | Transparent Game Object
    * Game object is rendered last
