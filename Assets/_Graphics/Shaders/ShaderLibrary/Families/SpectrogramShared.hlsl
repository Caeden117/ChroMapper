#ifndef CHROMAPPER_SPECTROGRAM_SHARED_INCLUDED
#define CHROMAPPER_SPECTROGRAM_SHARED_INCLUDED

inline uint CalculateSpectrogramIndex(float horizontalUv)
{
    return uint(max(horizontalUv * 63.0, 0.0));
}

#endif
