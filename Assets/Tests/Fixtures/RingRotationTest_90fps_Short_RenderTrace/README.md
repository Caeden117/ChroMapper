# RingRotationTest_90fps_Short_RenderTrace

Beat Saber 1.44.1 / Chroma capture session `2026-08-13T00:18:54.6999213Z` of the same
120 BPM short map as `RingRotationTest_90fps_Short`. It records every fixed checkpoint,
wave start, zero-ahead and look-ahead basic-event callback, and rendered ring transform.

The event-heavy interval is a complete stable 90 FPS run. The only post-start render gap
longer than 15 ms is at song time 10.2 seconds, after all movement has settled. This run
legitimately groups two dense callbacks together and ends at `-810` degrees, while the first
short capture separates them and ends at `-900` degrees. Tests therefore use each run's own
captured assignment frames for recurrence and do not pretend one callback phase is universal.

The render rows prove that Beat Saber applies raw, unclamped interpolation: 6,520 post-startup
Small-ring rows have a factor above one, including visible high-speed destination overshoot.
The stable post-start render phase measures `0.408825` fixed ticks modulo one tick across 902
ring-zero frames; ChroMapper rounds this to its deterministic `0.4` render-pair convention.

The callback CSV contains several `CallbacksInTime` ahead-time buckets. Only `aheadTime = 0`
represents the visible light/ring callback. The callback frame lights immediately but renders
the old fixed pair; the new ring wave first affects the following fixed tick.
