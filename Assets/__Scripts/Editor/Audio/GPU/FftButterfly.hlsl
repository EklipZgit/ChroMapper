// Shared butterfly for the editor display and full-precision environment spectrum.
uint ReverseFftBits(uint value, uint bits)
{
    uint result = 0;
    for (uint bit = 0; bit < bits; bit++)
    {
        result = (result << 1) | (value & 1);
        value >>= 1;
    }
    return result;
}

void CalculateFft(uint windowIndex)
{
    uint offset = windowIndex * SampleSize;
    uint butterflyCount = SampleSize >> 1;
    uint spacing = SampleSize;
    uint stages = (uint)log2(SampleSize);
    for (uint stage = 0; stage < stages; stage++)
    {
        float angle = pow(2, stage) * -2 * 3.14159265358979323846 / SampleSize;
        float stepReal = cos(angle);
        float stepImaginary = sin(angle);
        for (uint start = 0; start < SampleSize; start += spacing)
        {
            float real = 1;
            float imaginary = 0;
            for (uint butterfly = 0; butterfly < butterflyCount; butterfly++)
            {
                uint top = offset + start + butterfly;
                uint bottom = top + butterflyCount;
                float topReal = Real[top];
                float topImaginary = Imaginary[top];
                float bottomReal = Real[bottom];
                float bottomImaginary = Imaginary[bottom];
                Real[top] = topReal + bottomReal;
                Imaginary[top] = topImaginary + bottomImaginary;
                bottomReal = topReal - bottomReal;
                bottomImaginary = topImaginary - bottomImaginary;
                Real[bottom] = bottomReal * real - bottomImaginary * imaginary;
                Imaginary[bottom] = bottomReal * imaginary + bottomImaginary * real;
                float oldReal = real;
                real = real * stepReal - imaginary * stepImaginary;
                imaginary = oldReal * stepImaginary + imaginary * stepReal;
            }
        }
        butterflyCount >>= 1;
        spacing >>= 1;
    }
}

