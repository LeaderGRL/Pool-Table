# Contributing to Pool Table

Pool Table is being modernized incrementally. The repository workflow is designed to keep each change small, reviewable, testable, and easy to revert.

`docs/ROADMAP.md` is the source of truth for the modernization order. Review it before starting a new issue.

## Development prerequisites

- Git with Git LFS installed.
- Unity `6000.5.1f1` for changes that require opening or testing the project.
- PowerShell 7+ for the repository validation script.
- A clean checkout of `main` before creating a task branch.

Do not commit generated Unity folders, IDE files, local settings, player builds, or other paths excluded by `.gitignore`.

## Contribution workflow

Every coherent change follows the same lifecycle:

1. Review `docs/ROADMAP.md` and choose the next small task.
2. Create one GitHub issue describing the goal, current problem, expected outcomes, and validation.
3. Update local `main` and create one branch for that issue.
4. Implement only the scope required by the issue.
5. Run the validation relevant to the change.
6. Commit the work with a focused message.
7. Push the branch and open one pull request linked to the issue.
8. Address coherent review feedback on the same branch and re-run affected validation.
9. Wait for green CI and resolved review feedback.
10. The repository owner performs the final merge manually.

Do not start the next roadmap issue while the current pull request is waiting to be merged.

## Issues

Use an issue before implementation. Issues should be precise enough that the resulting pull request has a clear completion boundary.

Prefer this structure, inspired by FrogbyteEngine/Frogbyte:

- **Goal**: the outcome the issue should achieve.
- **Relevant code**: files, scenes, systems, or packages involved.
- **Problem**: why the current state is insufficient.
- **Expected outcomes**: observable conditions that define completion.
- **Validation**: tests or checks required before the pull request can be merged.

Keep unrelated cleanup and opportunistic refactors out of the issue. Create a separate issue when additional work is discovered.

## Branches

Create branches from an up-to-date `main` and include the GitHub issue number:

| Change type | Pattern | Example |
| --- | --- | --- |
| Infrastructure | `infrastructure/<issue>-<slug>` | `infrastructure/20-contribution-workflow` |
| Unity upgrade | `upgrade/<issue>-<slug>` | `upgrade/6-unity-6000-5` |
| Architecture | `architecture/<issue>-<slug>` | `architecture/21-assembly-boundaries` |
| Feature | `feature/<issue>-<slug>` | `feature/42-called-shot` |
| Bug fix | `bugfix/<issue>-<slug>` | `bugfix/43-pocket-detection` |
| Rendering | `rendering/<issue>-<slug>` | `rendering/44-urp-materials` |
| Networking | `networking/<issue>-<slug>` | `networking/55-relay-session` |
| Tests | `tests/<issue>-<slug>` | `tests/56-physics-calibration` |
| CI | `ci/<issue>-<slug>` | `ci/57-windows-build` |

Use lowercase slugs separated with hyphens.

## Commits

Use a concise subject in the form:

```text
Area: Imperative summary
```

Examples:

```text
Architecture: Introduce project assembly boundaries
Tests: Validate migrated gameplay wiring
CI: Validate repository baseline
```

Each commit should represent one understandable step. Review-fix commits are acceptable on the issue branch because the pull request may evolve through several review rounds.

For new pull requests, prefer **Squash and merge** so one issue produces one focused commit on `main`. Use the pull request title as the final squash commit subject.

## Pull requests

Open a pull request only after the branch is in a reviewable state. The description must explain the result rather than the conversation that led to it.

Every pull request should include:

- a concise summary of the problem and resulting behavior;
- `Closes #<issue>` when the pull request completes the issue;
- the main implementation decisions and relevant trade-offs;
- concrete validation results;
- screenshots or videos when a visual change requires them.

Keep the pull request scoped to its issue. If review reveals a separate concern, create another issue unless it is required for the current change to be correct.

## Validation

Every pull request must run:

```powershell
pwsh ./scripts/ci/Validate-Repository.ps1
git diff --check
```

Run additional validation according to the files and behavior changed:

- **EditMode tests** for pure C# logic, editor validation, rules, and architecture checks where applicable.
- **PlayMode tests** for scene wiring, runtime components, gameplay integration, and Unity lifecycle behavior.
- **Player builds** when build configuration, platform-specific code, packages, rendering, or deployment behavior changes.
- **Manual runtime validation** when automated tests cannot prove the user-facing behavior.

Record the exact relevant results in the pull request description. Documentation-only changes do not require Unity test execution unless they also modify Unity project content.

## Reviews

Treat review feedback as evidence to evaluate, not as changes to apply mechanically.

For every actionable review comment:

1. Verify the claim against the current branch and runtime behavior.
2. Apply the change when the finding is correct and in scope.
3. Run the validation affected by the change.
4. Push the fix to the same issue branch.
5. Request another review when appropriate.

Do not merge with unresolved correctness findings or failing required checks.

## `main` branch policy

`main` represents the latest reviewed and validated state of the project.

The intended repository settings are:

- changes reach `main` through pull requests;
- `Repository validation` is required before merge;
- force pushes and branch deletion are blocked on `main`;
- no mandatory approving review is required while the repository has a single owner;
- the repository owner performs the final merge;
- squash merge is preferred for new work;
- merged task branches can be deleted after the merge.

If repository ownership or team size changes, review these settings before increasing the required approval count.

## Unity and assets

- Keep the project on the Unity version pinned in `ProjectSettings/ProjectVersion.txt` unless an upgrade issue explicitly changes it.
- Track large binary assets according to `.gitattributes` and the repository Git LFS policy.
- Do not reserialize unrelated scenes, prefabs, materials, or ProjectSettings files.
- Remove editor-generated files from the change before committing.
- Write code comments in English.
