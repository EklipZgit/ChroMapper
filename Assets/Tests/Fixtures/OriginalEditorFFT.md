# Original editor FFT reference

`OriginalEditorFFT.compute` is the frozen editor shader from before its butterfly was extracted into the shared `FftButterfly.hlsl`. `GpuSpectrumPreparationTest.SharedButterflyPreservesEditorPackedOutput` dispatches this independent implementation and compares defined packed frequency bins byte-for-byte with the production shader.

Keep the reference independent of production. Including the shared butterfly or replacing it with the current shader would let both sides inherit the same regression. The restored pre-extraction source has SHA-256 `bc0c27150e13b6def597c41560e3ebf23d9afb0fa4f88456853d4d050710ef2c`.
