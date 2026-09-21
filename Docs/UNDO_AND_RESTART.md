# Blobs — Undo and Restart

**Decision date: September 9, 2026.** Intended functional and implementation contract. See [game design](GAME_DESIGN.md) for the player-facing summary, [resolution rules](RULE_RESOLUTION.md) for action outcomes, and [technical architecture](TECHNICAL_ARCHITECTURE.md) for system boundaries.

Undo is in semester scope. One Undo restores the complete state before one board-changing action: intermediate merges, Trail spawns, Ghost consumption/haunt/landing, and Grave clearing all belong to that unit. Play the entire action in reverse order, with special Undo audio and effects rather than forward impact feedback. Preserve enough action information to restore removed occupants and their identities/properties.

Undo never reduces or increases move count. Repeated Undo is allowed back to the initial state with no use limit. There is no Redo; a new forward action discards the undone future.

Selection is disabled during board-changing forward playback and Undo playback. Undo requests made while history is available during playback are queued until playback completes. Reject/unchanged-contact feedback does **not** lock selection or Undo. Undo is unavailable when history is empty and from confirmation of a winning action through victory; it cannot undo a win. Logical completion locks gameplay immediately, while victory presentation waits for all winning effects to finish.

Restart is always available, including forward playback, reverse playback, winning playback, and the victory screen. It cancels all pending playback and victory callbacks, clears selection/history, restores the authored board, resets move count to zero, and resumes a fresh session. Stale callbacks must never overwrite the restarted board or display an old victory.

## Ownership

Core determines legality and the complete ordered outcome. Application owns session commands, move count, history, and command availability. Presentation animates the recorded forward or reverse sequence and reports playback completion; it does not decide legality.
