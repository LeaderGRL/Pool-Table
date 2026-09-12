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

12. `TODO` **Architecture: Introduce application composition root**
    Gradually replace global singletons with an explicit bootstrap/composition root.

13. `TODO` **Architecture: Introduce typed ball identity**
    Replace the `white`, `black`, `filled`, `striped`, and `ball` tags with `BallId` and `BallGroup`.

14. `TODO` **Architecture: Model immutable match state**
    Introduce `MatchState`, players, groups, current turn, and match phase.

15. `TODO` **Architecture: Model shot intent and shot facts**
    Separate player commands from the events that are actually observed during a shot.

## Phase 3 — WPA 8-ball rules

Every rule must have EditMode tests before being connected to gameplay.

16. `TODO` **Rules: Implement legal break resolution**
17. `TODO` **Rules: Implement open-table state**
18. `TODO` **Rules: Implement player group assignment**
19. `TODO` **Rules: Implement legal first-contact validation**
20. `TODO` **Rules: Implement rail and pocket requirements**
21. `TODO` **Rules: Implement scratch and foul resolution**
22. `TODO` **Rules: Implement ball-in-hand state**
23. `TODO` **Rules: Implement called-shot information**
24. `TODO` **Rules: Implement eight-ball win and loss conditions**

## Phase 4 — new billiards physics

25. `TODO` **Physics: Normalize table and ball physical scale**
    Use physically coherent dimensions, with a standard ball diameter of approximately 57.15 mm.

26. `TODO` **Physics: Rebuild ball rigidbody configuration**
    Configure mass, collision detection, solver settings, sleep thresholds, and fixed timestep for billiards.

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

After PR #21 is merged, begin Phase 2 with **Architecture: Introduce project assembly boundaries**. Before every new issue, review this roadmap and verify the current status from repository, test, or merged-PR evidence.
