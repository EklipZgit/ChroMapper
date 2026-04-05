#ifndef CUSTOM_TIME_CG_INCLUDED
#define CUSTOM_TIME_CG_INCLUDED

// GET_TIME(offset) returns a float4 whose .y is the time scalar for UV panning.
// Matches SimpleLit es0.z logic:
//   FREEZE    -> offset alone          (frozen, no _Time.y)
//   SONG_TIME -> _SongTime.y + offset  (audio-synced)
//   Standard  -> _Time.y   + offset    (Unity wall-clock)

#if defined(_CUSTOM_TIME_FREEZE)
    #define GET_TIME(offset) float4(0, (offset), 0, 0)
#elif defined(_CUSTOM_TIME_SONG_TIME)
    #define GET_TIME(offset) float4(0, _SongTime.y + (offset), 0, 0)
#else
    #define GET_TIME(offset) float4(0, _Time.y + (offset), 0, 0)
#endif

#endif // CUSTOM_TIME_CG_INCLUDED
