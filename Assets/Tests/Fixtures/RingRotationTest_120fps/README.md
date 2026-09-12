# RingRotationTest_120fps

This fixture is a clone of `RingRotationTest_170fps` captured in Beat Saber 1.44.1 with the display refresh rate set to 120 Hz.

It uses the same map files as the sibling fixture but has a distinct ChromaGLS trace capture:

- `ChromaGLS-RingWaveStarts.csv`
- `ChromaGLS-RingHalfBeatStates.csv`

The sibling `RingRotationTest_170fps` fixture was captured at 170 Hz. Do not merge the CSVs: callback timing and sampled half-beat positions differ between refresh rates and both behaviors are regression inputs.