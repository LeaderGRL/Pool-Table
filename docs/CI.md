# Continuous integration

The repository uses two complementary GitHub Actions workflows.

## Repository validation

`.github/workflows/repository-ci.yml` runs the fast credential-free repository baseline on pull requests and pushes to `main`.

## Unity tests

`.github/workflows/unity-tests.yml` runs EditMode and PlayMode as independent jobs on every pull request. Both jobs use Unity `6000.5.1f1`, matching `ProjectSettings/ProjectVersion.txt`, and keep running independently when one mode fails.

The hosted workflow mirrors the local test contract established by `scripts/tests/Run-UnityTests.ps1`. GameCI provides the Unity Editor inside the hosted runner, while the local script resolves an installed editor directly.

### Required GitHub secrets

For a Unity Personal license, configure:

- `UNITY_LICENSE`: contents of the activated `.ulf` license file.
- `UNITY_EMAIL`: Unity account email.
- `UNITY_PASSWORD`: Unity account password.

For a Unity Professional license, configure:

- `UNITY_SERIAL`: Unity subscription serial.
- `UNITY_EMAIL`: Unity account email.
- `UNITY_PASSWORD`: Unity account password.

The workflow validates that one supported license strategy is available before starting Unity and never prints secret values.

### Test evidence

Each job uploads a `unity-editmode-results` or `unity-playmode-results` artifact even when the Unity test step fails. Evidence is retained for 14 days under `Logs/TestResults` and includes GameCI test results/logs plus any gameplay scenario JSON written to `Logs/TestResults/Scenarios`.

### Focused category runs

Manual workflow runs accept an optional Unity Test Framework category. Current categories include `Input`, `Physics`, `Functional`, `Gameplay`, and `SceneSmoke`.

Examples of the equivalent local commands:

```powershell
pwsh ./scripts/tests/Run-UnityTests.ps1 -TestPlatform EditMode
pwsh ./scripts/tests/Run-UnityTests.ps1 -TestPlatform PlayMode
pwsh ./scripts/tests/Run-UnityTests.ps1 -TestPlatform PlayMode -Category Functional
```

Pull requests always run the full EditMode and PlayMode suites. The category input is only for focused manual diagnostics.
