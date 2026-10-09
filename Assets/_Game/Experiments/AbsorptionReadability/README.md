# Absorption Readability Slices

These six small levels isolate the normal Blob interaction. They contain no Ghost,
Trail, Grave, Rock, or other special mechanic.

Use the `MechanicTestSliceController` on `Graybox.unity` to run every level in the
same scene and compare:

1. **Baseline** — resolved moves snap to their result with no merge choreography.
2. **Absorption** — the existing graybox slide/overlap/pulse communicates consumption.
3. **Growth Readability** — selects the authored size slice and presents state-backed
   Small, Normal, and Large blobs.

In Growth Readability, Small + Small produces a Normal survivor, Normal + Normal remains
Normal, and a larger target consumes a smaller mover. Flags, Ghosts, and Rocks do not
carry size state.

Before entering Play Mode, select `GameBootstrapper` and choose `Initial Mode` plus an
`Initial Level Index` from 0–5. Selecting Growth Readability jumps to the final size
slice. The controller also exposes `SetMode`, `LoadLevel`,
`NextLevel`, and `PreviousLevel` for temporary test UI buttons if a facilitator wants
to switch slices without leaving Play Mode.

## Level intent

1. `01_FirstAbsorption`: one merge followed by the goal; checks first-glance reading.
2. `02_TurnTheCorner`: the survivor must finish a two-direction sequence.
3. `03_ChooseTheSurvivor`: asks which color/position should remain for the next move.
4. `04_MergeOrder`: multiple candidates make order and destination legibility important.
5. `05_AccumulatedMass`: a longer chain checks repeated absorption readability.
6. `Size_01_GrowThenYield`: two Small blobs merge into a Normal source; that source is
   then consumed by a Large target, which can move into its matching Flag to win.

Compare Baseline and Absorption on the five absorption slices. Use Growth Readability
for the authored size slice. Avoid explaining the animation; ask the player what they
think happened after the first merge.
