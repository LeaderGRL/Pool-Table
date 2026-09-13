# Pool Table modernization roadmap

This document is the source of truth for the modernization plan. Before starting any new issue, branch, or PR, the relevant step must be reviewed here and its status must be verified. A step is marked complete only after real validation and the corresponding PR has been merged.

## Execution rules

- One small issue per coherent change.
- One branch per issue.
- One PR per issue.
- Merging remains manual: after a PR is opened, work stops until the repository owner merges it.
- Issue and PR descriptions should match the level of precision used in FrogbyteEngine/Frogbyte.
- Tests relevant to the change must be run before opening the PR.
- Code comments must be written in English.
- Tasks may be split into smaller sub-issues without changing the roadmap goal.

Recommended branch names:

- `infrastructure/<issue>-<slug>`
- `upgrade/<issue>-<slug>`
- `architecture/<issue>-<slug>`
- `feature/<issue>-<slug>`
- `bugfix/<issue>-<slug>`
- `rendering/<issue>-<slug>`
- `networking/<issue>-<slug>`
- `tests/<issue>-<slug>`
- `ci/<issue>-<slug>`

## Current status

Legend: `DONE` = merged and verified, `PARTIAL` = part of the goal is already covered but the full objective is still open, `TODO` = not started, `PR` = PR open and waiting to be merged.

Work already completed in support of this backlog:

- `DONE` Repository cleanup / Git LFS / history rewrite.
- `DONE` Project migration to Unity `6000.5.1f1` while keeping the Built-in Render Pipeline.
- `DONE` Credential-free repository validation CI (PR #9).
- `DONE` First Unity EditMode smoke test for `PoolTable.unity` (issue #10 / PR #11).

## Phase 0 — restore repository health

1. `DONE` **Infrastructure: Purge generated Unity artifacts from repository history**
   Purge `Library`, `Logs`, `obj`, `.vs`, `UserSettings`, Windows/WebGL builds, `.csproj`, `.sln`, and generated archives. Create a local `git bundle` backup before rewriting history, then rewrite history as agreed.
   Reference: GitHub issue #5.

2. `DONE` **Infrastructure: Define Unity repository ignore rules**
   Add a clean Unity `.gitignore` and remove the old `.collabignore` / `ignore.conf` systems.

3. `DONE` **Infrastructure: Define Git LFS asset policy**
   Configure `.gitattributes`, file normalization, and LFS for large binary source assets that are genuinely required.
   Reference: GitHub issue #2.

4. `DONE` **Infrastructure: Add repository development workflow**
   Add branch, commit, issue, and PR conventions.
   The workflow is documented for this personal, solo-maintained repository and supported by reusable GitHub issue and pull-request templates. Reference: GitHub issue #20 / PR #21.

## Phase 1 — Unity migration without changing rendering

5. `DONE` **Upgrade: Migrate project to Unity 6000.5.1f1**
   Open and reserialize the project while temporarily keeping the Built-in Render Pipeline.
   Reference: GitHub issue #6 / PR #7.

6. `DONE` **Upgrade: Remove obsolete Unity editor integrations**
   Remove legacy Collab/Plastic integrations and old editor extensions with no runtime value, including ERP if incompatible.
   ERP/Discord SDK, the remaining Collab/Plastic package, and the empty ERP asset-folder residue are removed. Reference: issue #16 / PR #17.

7. `DONE` **Upgrade: Update Unity packages for Unity 6.5**
   Update Cinemachine and the other packages to published compatible versions and pin their versions.

8. `DONE` **Upgrade: Restore the playable PoolTable scene**
   Fix broken references, missing scripts, and serialization issues.
   The scene opens with no missing scripts and passes the runtime startup smoke test. The remaining legacy gameplay wiring was validated in issue #18 / PR #19.

9. `DONE` **Upgrade: Restore Windows and Web build targets**
   Recreate the build settings because `EditorBuildSettings` currently contains no scenes.
   `PoolTable.unity` is restored as the player entry scene and a Windows x64 player build was validated in PR #13.

10. `DONE` **Testing: Establish Unity 6 migration baseline**
    Clean compilation, zero console errors, scene opening, and first PlayMode smoke test.
    Compilation, scene opening, EditMode validation, player build settings, and the first PlayMode smoke test are validated. Reference: issue #14 / PR #15.

## Phase 2 — modern foundations

11. `DONE` **Architecture: Introduce project assembly boundaries**
    Add Core, Gameplay, Physics, Input, Networking, Presentation, and Tests asmdefs.
    The modern runtime dependency graph and Unity-free Core boundary are enforced by EditMode architecture tests while legacy gameplay remains isolated in `Assembly-CSharp`. Reference: GitHub issue #22.

12. `PARTIAL` **Architecture: Introduce application composition root**
    Gradually replace global singletons with an explicit bootstrap/composition root.
    The scene composition root now explicitly wires collision audio and removes `SoundManager.Instance`; remaining legacy globals are intentionally deferred. Reference: GitHub issue #24.

13. `PARTIAL` **Architecture: Introduce typed ball identity**
    Replace the `white`, `black`, `filled`, `striped`, and `ball` tags with `BallId` and `BallGroup`.
    Typed `BallId` / `BallGroup` metadata now exists on all 16 scene balls with compatibility validation against legacy tags. Tag consumers remain intentionally unchanged until follow-up migration issues. Reference: GitHub issue #26.

14. `DONE` **Architecture: Model immutable match state**
    Introduce `MatchState`, players, groups, current turn, and match phase.
    The Unity-independent immutable domain model now covers players, group assignment, current turn, and match phase. Legacy `GameManager` integration is intentionally handled by later focused migration work. Reference: GitHub issue #28.

15. `DONE` **Architecture: Model shot intent and shot facts**
    Separate player commands from the events that are actually observed during a shot.
    Unity-independent `ShotIntent`, normalized shot direction/power, and immutable observed `ShotFacts` now define the command/fact boundary for future physics, WPA rules, and networking. Legacy shooting integration is intentionally deferred. Reference: GitHub issue #30.

## Phase 3 — WPA 8-ball rules

Every rule must have EditMode tests before being connected to gameplay.

16. `DONE` **Rules: Implement legal break resolution**
    The pure Core rules layer now evaluates the WPA legal-break requirement from immutable `ShotFacts`: pocketing any object ball satisfies the break requirement, otherwise at least four distinct object balls must reach a rail. Scratch/foul penalties and player options remain in their dedicated follow-up rules issues. Reference: GitHub issue #32.
17. `DONE` **Rules: Implement open-table state**
    The Core rules layer now owns the immutable `Break` to `OpenTable` transition and exposes an explicit open-table query while preserving unassigned groups. Group assignment and turn/foul resolution remain separate follow-up rules. Reference: GitHub issue #34.
18. `DONE` **Rules: Implement player group assignment**
    The Core rules layer now assigns complementary solids/stripes groups only from an open table when the current player is explicitly reported to have legally pocketed a grouped object ball. Direct group mutation is restricted to Core so higher-level systems cannot bypass the rule. Reference: GitHub issue #36.
19. `DONE` **Rules: Implement legal first-contact validation**
    Core now validates first object-ball contact for break, open-table, assigned-group, and cleared-group states using immutable match state plus a validated pre-shot object-ball snapshot. The 8-ball remains illegal as a first contact while the table is open and only becomes legal after groups are assigned and the shooter's group is cleared. Reference: GitHub issue #38.
20. `DONE` **Rules: Implement rail and pocket requirements**
    Core now evaluates WPA rule 3.3 for normal shots: any pocketed ball satisfies the narrow rail-or-pocket requirement; otherwise an object-ball contact must be followed by at least one cue-ball or object-ball rail contact. Break validation remains under `LegalBreakRule`, while scratch and combined foul resolution remain separate. Reference: GitHub issue #40.
21. `DONE` **Rules: Implement scratch and foul resolution**
    Core now composes first-contact and rail/pocket evaluations with cue-ball scratch detection into a multi-foul result for all currently modeled shot facts. Break scratch is detected without duplicating `LegalBreakRule`; ball-in-hand and other consequences remain separate. Reference: GitHub issue #42.
22. `DONE` **Rules: Implement ball-in-hand state**
    Core now models ball-in-hand as immutable match state with an explicit recipient and cue-ball placement area. Standard fouls advance the turn and grant the incoming player placement anywhere, while the restricted above-head-string consequence remains an explicit post-break-foul choice. Placement consumption clears the state without mutating the previous snapshot, and match invariants reject invalid recipients, phases, or placement scopes. Reference: GitHub issue #44.
23. `DONE` **Rules: Implement called-shot information**
    Core now models the WPA call-shot pair as an optional immutable object-ball + pocket declaration on `ShotIntent`. `ShotFacts` records each pocketed ball together with its stable six-pocket identifier while preserving the derived ball-only list used by existing rules. UI selection, Unity pocket mapping, shot legality, and 8-ball win/loss resolution remain separate follow-up work. Reference: GitHub issue #46.
24. `DONE` **Rules: Implement eight-ball win and loss conditions**
    Core now resolves terminal WPA 8-ball outcomes into immutable `MatchResult` data with winner, loser, and preserved end reasons. A legal called 8-ball after the shooter's group is cleared wins; foul, early pocket, wrong/uncalled pocket, or 8-ball off-table loses. `ShotFacts` now records balls driven off the table, foul resolution reports cue/object-ball off-table faults, finished match states require a result and reject later turn changes, and break-shot 8-ball outcomes remain intentionally non-terminal for separate break handling. Reference: GitHub issue #48.

### Gameplay integration checkpoint

- `PR` **Gameplay: Add Core rules integration checkpoint**
  `MatchShotResolver` composes immutable shot intent/facts with the Core rules into one authoritative next `MatchState`. Terminal 8-ball outcomes are handled before non-terminal consequences, standard fouls advance the turn with ball-in-hand, clean successful calls can assign an open table and continue the shooter's turn, and unresolved break choices are surfaced explicitly instead of guessed. The resolver remains independent from legacy gameplay singletons and is covered by deterministic EditMode tests plus a scene PlayMode smoke using typed ball identities. Reference: GitHub issue #50.

## Phase 4 — new billiards physics

25. `PR` **Physics: Normalize table and ball physical scale**
    The physics layer now defines one metric 9-foot table specification: one Unity world unit equals one meter, regulation balls use a 57.15 mm diameter, the reference playing surface is 2.54 m x 1.27 m, and the table bed uses a deterministic height inside the WPA equipment range. `PoolTable.unity` is normalized to that scale, including a legal WPA 8-ball rack layout, cue-ball head-string placement, metric cue-controller distance/stroke/impulse tuning, a metric stopped-ball threshold, and scratch recovery back to the cue ball's initial metric placement and idle state. EditMode specification tests and PlayMode scene-scale/gameplay regression coverage prevent future drift. Mass, collision detection, solver settings, timestep, friction, spin, rail response, and pocket rebuilding remain in their dedicated follow-up issues. Reference: GitHub issue #52.

26. `PR` **Physics: Rebuild ball rigidbody configuration**
    Define the authoritative metric rigidbody/simulation baseline: regulation-scale ball mass, continuous dynamic collision detection, interpolation, tighter contact tolerance, higher solver iteration counts, a lower sleep threshold, and a 0.005 s fixed timestep. Legacy 0.35/0.2 Rigidbody damping is retained explicitly as a temporary turn-termination safeguard until the cloth-resistance issue replaces it. Legacy upward-velocity damping is scaled by elapsed fixed time so changing the physics tick rate preserves the previous 20 ms behavior, and shot strength is expressed as a mass-independent target velocity change so the 0.17 kg ball mass does not multiply shot speed. Scene and runtime tests prevent the 16 billiard balls, shot-speed behavior, temporary resistance, timestep-sensitive damping, or project physics settings from drifting unexpectedly. Reference: GitHub issue #54.

27. `TODO` **Physics: Rebuild cloth friction model**
28. `TODO` **Physics: Implement sliding-to-rolling transition**
29. `TODO` **Physics: Implement cue-ball spin**
30. `TODO` **Physics: Implement rail collision response**
31. `TODO` **Physics: Rebuild pocket detection and capture**
32. `TODO` **Physics: Add shot simulation instrumentation**
    Measure trajectories, energy, stopping time, and collisions to calibrate gameplay.

The goal is not to make PhysX deterministic across machines. In multiplayer, only the host simulation will be authoritative.

## Phase 5 — player controls

33. `TODO` **Input: Migrate project to Unity Input System**
34. `TODO` **Gameplay: Implement aiming state**
35. `TODO` **Gameplay: Implement shot power control**
36. `TODO` **Gameplay: Implement cue-ball spin control**
37. `TODO` **Gameplay: Add controller input support**
38. `TODO` **Gameplay: Implement ball-in-hand placement**
39. `TODO` **Camera: Rebuild aiming camera**
40. `TODO` **Camera: Implement shot and spectate cameras**

## Phase 6 — URP conversion

41. `TODO` **Rendering: Install and configure URP**
42. `TODO` **Rendering: Convert legacy materials to URP**
43. `TODO` **Rendering: Rebuild pool-table PBR materials**
44. `TODO` **Rendering: Rebuild lighting and reflection setup**
45. `TODO` **Rendering: Add post-processing quality profile**
46. `TODO` **Rendering: Add Windows and Web quality profiles**

URP will also introduce Unity 6 Render Graph considerations, which must be taken into account for any future custom render feature.

## Phase 7 — presentation and juice

47. `TODO` **Audio: Rebuild impact audio from collision energy**
48. `TODO` **Audio: Add rail, pocket and cue impact layers**
49. `TODO` **VFX: Add chalk and cue impact feedback**
50. `TODO` **VFX: Add pocket feedback**
51. `TODO` **Camera: Add impact impulse and shot framing**
52. `TODO` **UI: Build modern match HUD**
53. `TODO` **UI: Add called-shot interaction**
54. `TODO` **UI: Add turn and foul feedback**

Visual juice may be pronounced as long as it does not alter the real physical trajectory of the balls.

## Phase 8 — multiplayer

55. `TODO` **Networking: Install Multiplayer Services and NGO**
56. `TODO` **Networking: Add anonymous Unity authentication**
57. `TODO` **Networking: Implement Relay session creation**
58. `TODO` **Networking: Implement join-by-code flow**
59. `TODO` **Networking: Make match state host authoritative**
60. `TODO` **Networking: Send authoritative shot intents**
61. `TODO` **Networking: Replicate ball simulation snapshots**
62. `TODO` **Networking: Add client interpolation**
63. `TODO` **Networking: Synchronize turns and WPA rules state**
64. `TODO` **Networking: Handle disconnects and session shutdown**
65. `TODO` **Networking: Add WebGL WSS transport configuration**
66. `TODO` **Networking: Add multiplayer latency simulation tests**

Relay is intended for a listen-server model: the host creates the session and players communicate through Relay without exposing their addresses. For the first version, if the host leaves the match, the match ends. Host migration is a later feature: migrating session ownership alone is not enough to automatically reconstruct the authoritative PhysX state.

## Phase 9 — quality and delivery

67. `TODO` **Testing: Add Core EditMode test suite**
68. `TODO` **Testing: Add gameplay PlayMode tests**
69. `TODO` **Testing: Add physics calibration tests**
70. `TODO` **Testing: Add two-player Multiplayer Play Mode tests**
71. `PARTIAL` **CI: Add Unity pull-request validation**
    Structural repository validation already exists. Hosted Unity execution and license handling still need to be added.
72. `TODO` **CI: Add Windows build validation**
73. `TODO` **CI: Add WebGL build validation**
74. `TODO` **CI: Add automated artifact builds from main**
75. `TODO` **Performance: Establish Windows performance budget**
76. `TODO` **Performance: Establish WebGL performance budget**

## Resume order

After issue #52 is merged, continue Phase 4 with **Physics: Rebuild ball rigidbody configuration**. Before every new issue, review this roadmap and verify the current status from repository, test, or merged-PR evidence.
