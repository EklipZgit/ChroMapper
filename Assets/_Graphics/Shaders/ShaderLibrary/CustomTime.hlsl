#ifndef CUSTOM_TIME_CG_INCLUDED
#define CUSTOM_TIME_CG_INCLUDED

// GET_TIME(offset) returns a float4 whose .y is the time scalar for UV panning.
// Matches SimpleLit es0.z logic:
//   FREEZE    -> offset alone          (frozen, no _Time.y)
//   SONG_TIME -> _SongTime + offset  (audio-synced)
//   Standard  -> _Time   + offset    (Unity wall-clock)

uniform float4 _SongTime;

#if defined(_CUSTOM_TIME_FREEZE)
#define GET_TIME(offset) float4(offset * 0.05, offset, offset * 2, offset * 3) + offset
#elif defined(_CUSTOM_TIME_SONG_TIME)
#define GET_TIME(offset) _SongTime + offset
#else
#define GET_TIME(offset) _Time + offset
#endif

#endif // CUSTOM_TIME_CG_INCLUDED
