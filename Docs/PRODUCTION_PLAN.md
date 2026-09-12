# Blobs production plan

Updated September 9, 2026. The production foundation already exists under `Assets/_Game/Production`;

Use [Game design](GAME_DESIGN.md) as the rule authority and [Rules implementation plan](RULES_IMPLEMENTATION_PLAN.md) as the ordered implementation/acceptance checklist. The old foundation milestones are superseded by this plan; their completion does not establish conformance to the newly agreed rules.

1. Reconcile reached-path resolution, Rock stops, Trail targets, Flag capture, Ghost/Grave behavior, and level validation.
2. Separate move count from action history and implement whole-action Undo, reverse playback, and dedicated Undo feedback.
3. Integrate playback locks, delayed victory presentation, and restart cancellation across all states.
4. Validate and extend the authored level sequence, complete player flow, and playtest.
5. Produce a verified final build and semester documentation within exactly 15 weeks.

Preserve the dependency boundaries in [Technical architecture](TECHNICAL_ARCHITECTURE.md). Core determines legality; Application manages session state and history; Presentation consumes ordered outcomes. Targeted changes should extend the existing foundation.

The implementation checklist contains the week allocation and specific acceptance cases. Optional mechanics must not displace required Undo, content, testing, or delivery work.
