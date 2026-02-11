#ifndef CUSTOM_TIME_CG_INCLUDED
#define CUSTOM_TIME_CG_INCLUDED

#if defined(_CUSTOM_TIME_SONG_TIME)
uniform float _SongTime;
#define GET_TIME(_TimeOffset) \
    float4(_SongTime * 0.05, _SongTime, _SongTime * 2, _SongTime * 3)
#elif defined(_CUSTOM_TIME_FREEZE)
#define GET_TIME(_TimeOffset) \
    float4(_TimeOffset * 0.05, _TimeOffset, _TimeOffset * 2, _TimeOffset * 3)
#else
#define GET_TIME(_TimeOffset) \
    _Time
#endif

#endif
