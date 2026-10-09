# Color flag playtest levels

Five normal-monster levels for the September 23, 2026 multi-flag experiment. No runtime rules, existing levels, or scene assignments were changed.

## Run the levels

1. Open the Graybox scene and leave Play Mode.
2. Select the GameObject with `GameBootstrapper`.
3. Assign a level from `Assets/_Game/Content/Levels/SO/ColorFlags` to its **Level Asset** field.
4. Enter Play Mode. Repeat in numerical order. Use the same scene and animation for all five.

The assets use the existing graybox color palette: enum Red renders pink; the third color is Yellow. They also work with the monster scene's presentation setup.

## Rules and validation scope

These assets use the existing `ClearAllClearableBlobs` objective. Current rules allow a matching Normal monster to enter a flag when it is the only remaining clearable monster of that color. Other colors may remain. A flag remains in place and cannot be crossed on the way to another target.

The engine does not independently require every flag to be captured. These particular layouts were selected so **every reachable winning state involves capturing every flag anyway**. This is a property of these layouts under the current rules, not a new objective implementation.

A temporary .NET harness compiled the project's actual Core sources, built each board with `LevelFactory`, and exhaustively searched successful source/target moves with `MoveResolver`. Capture history was tracked separately to detect flag-skipping wins. Breadth-first search established the shortest solutions below. No simplified duplicate gameplay rules were used. Unity import, visual presentation, and phone interaction are not covered by that search.

The state counts include distinct piece identities and flag-capture history. Minimum move counts measure solution length, not player difficulty. Alternate solutions may exist.

## Playtest protocol

Ask the player to clear the monsters and send a survivor to each matching flag. Do not reveal the solutions or warn them about level 03. Record failed gestures separately from incorrect rule predictions. After a surprising result ask, “What did you expect?” Before repeating a level ask, “What would you do differently?”

For level 03, observe whether they can explain why pink must remain available. For level 04, observe whether they understand why retiring pink early can now work. Undo and retries are expected, not automatic test failures.

## Designer solutions — spoilers

Coordinates are zero-based, with (0,0) at bottom left. Each listed move is a drag from the first cell to the second; the listed source identity persists after normal merges. Grid symbols: P/B/Y = monsters, FP/FB/FY = flags, . = traversable empty cell.

### 01 — Level_ColorFlags_01_FirstPair

Learn that blue can remove an extra pink, then each survivor can enter its own flag.

Board: 3 × 2. Shortest solution: 3 moves. Reachable states checked: 6. Flag-skipping winning states: 0.

```text
P   FB  P
B   .   FP
```

1. blue-1: (0,0) → (0,1), targeting pink-2.
2. pink-1: (2,1) → (2,0), targeting pink-flag.
3. blue-1: (0,1) → (1,1), targeting blue-flag.

### 02 — Level_ColorFlags_02_KeepBothColors

Preserve both colors while choosing the correct direction of each interaction.

Board: 4 × 3. Shortest solution: 4 moves. Reachable states checked: 17. Flag-skipping winning states: 0.

```text
FB  P   .   P
.   .   .   B
FP  B   .   .
```

1. pink-2: (1,2) → (1,0), targeting blue-2.
2. blue-1: (3,1) → (3,2), targeting pink-1.
3. pink-2: (1,0) → (0,0), targeting pink-flag.
4. blue-1: (3,2) → (0,2), targeting blue-flag.

### 03 — Level_ColorFlags_03_WaitToCapture

Discover that a legal early pink capture can leave an unsolvable board.

Board: 4 × 3. Shortest solution: 4 moves. Reachable states checked: 9. Flag-skipping winning states: 0.

```text
B   FB  .   .
.   B   .   B
.   P   .   FP
```

1. pink-1: (1,0) → (1,1), targeting blue-3.
2. pink-1: (1,1) → (3,1), targeting blue-1.
3. pink-1: (3,1) → (3,0), targeting pink-flag.
4. blue-2: (0,2) → (1,2), targeting blue-flag.

Verified trap: capture pink immediately, (1,0) → (3,0). The move is legal, but no winning continuation exists. Undo restores the opportunity to solve it.

### 04 — Level_ColorFlags_04_ThirdColor

Contrast the previous level: pink can finish immediately while blue and yellow still clear each other.

Board: 4 × 3. Shortest solution: 5 moves. Reachable states checked: 26. Flag-skipping winning states: 0.

```text
B   FY  P   FP
Y   .   FB  .
.   .   Y   B
```

1. pink-1: (2,2) → (3,2), targeting pink-flag.
2. blue-1: (3,0) → (2,0), targeting yellow-1.
3. yellow-2: (0,1) → (0,2), targeting blue-2.
4. blue-1: (2,0) → (2,1), targeting blue-flag.
5. yellow-2: (0,2) → (1,2), targeting yellow-flag.

Verified contrast: the first solution move captures pink immediately; the remaining blue/yellow board is still solvable.

### 05 — Level_ColorFlags_05_PlanTheOrder

Plan across three colors, preserve useful survivors, and position them for their flags.

Board: 4 × 3. Shortest solution: 7 moves. Reachable states checked: 99. Flag-skipping winning states: 0.

```text
B   .   FP  Y
P   B   FB  Y
FY  .   P   B
```

1. pink-2: (0,1) → (0,2), targeting blue-1.
2. yellow-2: (3,1) → (3,0), targeting blue-2.
3. pink-1: (2,0) → (3,0), targeting yellow-2.
4. blue-3: (1,1) → (2,1), targeting blue-flag.
5. yellow-1: (3,2) → (3,0), targeting pink-1.
6. pink-2: (0,2) → (2,2), targeting pink-flag.
7. yellow-1: (3,0) → (0,0), targeting yellow-flag.
