# CI (GitHub Actions)

Two workflows, designed to stay within the GitHub Free quota (2,000 Linux minutes/month on a private repo).

| Workflow | File | When | Cost |
|---|---|---|---|
| **Checks** | `.github/workflows/checks.yml` | Every push, every PR to `main` | ~10 s, no Unity |
| **Unity tests** | `.github/workflows/unity-tests.yml` | PR to `main` / push to `main` touching `Assets/`, `Packages/` or `ProjectSettings/`, or by hand | ~5–15 min |

## Checks (level 1)

- Every file in `Assets/` has its `.meta`, and no `.meta` is orphaned.
- No file over 10 MB was committed outside Git LFS.
- C# whitespace formatting in `Assets/_Project` (`dotnet format whitespace`).

Run them locally:

```bash
bash .github/scripts/check-meta.sh Assets
bash .github/scripts/check-large-files.sh 10
dotnet format whitespace --folder Assets/_Project
```

## Unity tests (level 2)

Runs EditMode tests with [GameCI](https://game-ci.com/docs/github/test-runner). PlayMode can be triggered by hand from the Actions tab (*Run workflow* → `playmode` or `all`).

Tests live in `Assets/_Project/Tests/EditMode`. `DataValidationTests` checks every asset in `Assets/_Project/Data`: no missing script, no reference to a deleted asset. Add a test here for each new rule on game data (unique IDs, recipe ingredients that exist, reachable quests…).

### One-time setup: Unity license secrets

Without these secrets the workflow does not fail: it shows a warning and skips the tests.

In GitHub: **Settings → Secrets and variables → Actions → New repository secret**

| Secret | Value |
|---|---|
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |
| `UNITY_LICENSE` | Full content of your license file (Personal license works) |

The license file is on your PC once Unity Hub has activated a license:
`C:\ProgramData\Unity\Unity_lic.ulf`. Open it in a text editor and paste all of it into the secret.
Full guide: https://game-ci.com/docs/github/activation

## Staying within the free quota

Already built in:

- Linux runners only (Windows minutes count double, macOS ten times).
- `paths` filters: changing `docs/` never starts Unity.
- `concurrency` + `cancel-in-progress`: a new push cancels the previous run.
- `timeout-minutes` on every job.
- `Library/` cache (avoids a full reimport) and LFS cache (saves LFS bandwidth).
- Artifacts kept 3 days.

Keep an eye on usage: **github.com → Settings → Billing and plans → Usage**.
With no payment method (or a $0 spending limit), going over the quota just pauses the workflows until next month. You are not billed.

## Later

- **Builds** (WebGL → itch.io or GitHub Pages, Windows): `game-ci/unity-builder`, only on a `v*` tag or by hand.
- **Self-hosted runner** on your own PC: no GitHub minutes used, but the PC must be on.
