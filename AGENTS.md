# Moonbrew: agent guidelines

Cozy farm-and-café life sim. Solo project, single-player. Unity 6 (6000.3), URP, C#, Unity Test Framework 1.6. See `README.md` for the concept and `Docs/` for design and CI.

## Language

- Code, comments, workflow names, commit messages, branch names: English.
- `README.md` and `AGENTS.md`: English. Documentation in `Docs/`: French.
- Replies to the user: French.

## Principles

- Write as little code as possible: reuse what exists, no abstraction or code "for later". An interface is justified only when a test needs to substitute the dependency.
- Keep replies and explanations short and to the point.
- Comments only when they are useful: the non-obvious *why*, never a paraphrase of the code. Prefer clear names.

## Architecture

| Folder `Assets/_Project/Scripts/` | Assembly | Role |
|---|---|---|
| `Core/` | `Moonbrew.Core` | Time and clock, events, save, game state (base) |
| `Inventory/` | `Moonbrew.Inventory` | Player items and café stock (base) |
| `Player/` | `Moonbrew.Player` | Movement, interaction |
| `Farming/` | `Moonbrew.Farming` | Crops, fields |
| `Kitchen/` | `Moonbrew.Kitchen` | Ingredient processing, recipes, cooking |
| `Cafe/` | `Moonbrew.Cafe` | Customers, orders, service |
| `Employee/` | `Moonbrew.Employee` | Automatic service when the owner is away |
| `Economy/` | `Moonbrew.Economy` | Money, shops, prices |
| `Npc/` | `Moonbrew.Npc` | Villagers, relationships |
| `Quests/` | `Moonbrew.Quests` | Quests and objectives |
| `Rendering/` | `Moonbrew.Rendering` | Pixel rendering, camera, palettes |
| `Ui/` | `Moonbrew.Ui` | Menus, HUD: display only |

Other folders under `Assets/_Project/`: `Data/` (ScriptableObject instances), `Art/`, `Audio/`, `Prefabs/`, `Scenes/`, `Tests/`.

### Non-negotiable rules

- **Dependencies:** `Ui` → feature modules → base modules. Base modules are `Core` (depends on nothing) and `Inventory` (depends on `Core` only). A feature module may reference `Core` and `Inventory`, never another feature module.
- **UI has no game logic:** it reads state and forwards player actions.
- **Communication between systems:** events, no direct cross-references. Subscribe in `OnEnable`, unsubscribe in `OnDisable`. Events are named in the past tense (`OrderServed`, `DayEnded`). They are `readonly struct`s declared in `Core` (`GameEvents.cs`) and go through the shared `SO_EventBus` asset (`EventBusAsset`), which components receive by serialized reference or `Init`.
- **Dependencies are passed explicitly** (constructor or `Init`). No singletons, no `FindObjectOfType` / `FindFirstObjectByType` to reach a system.
- **Content is data:** items, recipes, customers and quests are ScriptableObjects. Adding content never requires code changes.
- **No magic numbers:** prices, durations, quantities and rates live in ScriptableObjects.
- **Stable IDs:** every content ScriptableObject has a unique, stable ID (string or GUID), never derived from the file name. Saves store this ID, never a Unity reference.
- **Game time** always goes through the `Core` clock, never `Time.time` directly.
- **Versioned saves:** changing the save format requires a migration and a test that loads the previous version.
- **Visuals:** only colors from the project palette, low-resolution rendering, no post-processing that breaks the pixelated style.

## C# conventions

- `PascalCase`: classes, methods, public properties. `camelCase`: locals, parameters. `_camelCase`: private non-serialized fields.
- `[SerializeField] private` rather than `public` fields.
- Asset names prefixed by type: `SO_` ScriptableObject, `PF_` prefab, `M_` material, `T_` texture, `SC_` scene (`SO_Item_Wheat`, `PF_Customer`).
- No raw strings for tags, layers or animator parameters. Use constants or `Animator.StringToHash`.
- Formatting follows `.editorconfig`. Run `dotnet format whitespace --folder Assets/_Project` before committing (checked by CI).

### `Update`

- No game logic in `Update`. Use events, `Core` clock ticks, Input System callbacks, coroutines or `Awaitable` instead.
- `Update` is allowed only for what really changes every frame: player movement, camera, visual interpolation. It stays minimal and delegates to plain C# classes.
- No `GetComponent`, `Find` or allocations (LINQ, `new List`, string concatenation) in per-frame code. Cache references in `Awake`.
- Never leave an empty `Update` in a script.

## Unity precautions

- **`.meta` files:** never delete one or change a GUID. When renaming or moving an asset, move its `.meta` with it. Every new file under `Assets/` must be committed with its `.meta` (CI fails otherwise).
- **Scenes, prefabs, assets:** never hand-edit `.unity`, `.prefab` or `.asset` YAML. Use Unity MCP or an Editor script when available; otherwise describe the editor steps one by one instead of doing them.
- Never modify `ProjectSettings/` or `Packages/manifest.json` without asking first.
- **Synty assets** come from the private package `com.moonbrew.synty` (repo `moonbrew-synty`, read-only in Unity). Never copy a Synty asset into `Assets/`. For an object used in a scene, create a Prefab Variant in `Assets/_Project/Prefabs/`; recolored materials go in `Assets/_Project/Art/`. Adding a pack happens in `moonbrew-synty` (new tag), then the tag is bumped in `manifest.json`. This repository is public: anything derived from Synty content (recolored Synty textures, edited Synty meshes) also goes in `moonbrew-synty`, never here. `.gitignore` ignores `Polygon*`, `Synty` and `Sidekick*` folders under `Assets/` as a safety net, so never use these names for Moonbrew's own folders.
- Never touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`.

## Testing (Unity Test Framework)

The agent writes the tests for everything it implements or modifies.

### Testability

- Never put game rules, calculations or state transitions directly in a `MonoBehaviour`. Extract them into plain C# classes testable in EditMode. The `MonoBehaviour` only wires Unity to that logic.
- Inject the clock and random source so tests are deterministic.

### Organisation

- NUnit. EditMode in `Assets/_Project/Tests/EditMode/`, PlayMode in `Assets/_Project/Tests/PlayMode/`, namespace `Moonbrew.Tests.EditMode` / `Moonbrew.Tests.PlayMode`.
- Test assemblies cannot reference `Assembly-CSharp`: testing a module requires adding its assembly to the test `.asmdef`.
- One `<ClassName>Tests.cs` per logic class, one `<Feature>IntegrationTests.cs` per PlayMode feature.
- Naming `Method_Condition_ExpectedResult`, Arrange / Act / Assert structure.
- Test the public API only: no reflection.

### EditMode unit tests

- **Happy path:** standard inputs and outputs.
- **Edge cases (mandatory, at least 2 per public method):** `0`, negative values, `null`, empty collections, max capacity (buying with 0 gold, selling a missing item, full inventory, ending a day twice).
- **State transitions:** valid transitions succeed, invalid ones are rejected.
- **Serialization:** round-trip, corrupt or empty input handled without unhandled exceptions, loading a save from the previous version.
- **Data validation:** every new ScriptableObject type gets rules in `DataValidationTests` (unique IDs, required fields, references that exist).
- Objects created in a test are destroyed with `Object.DestroyImmediate` in `[TearDown]`.
- No assertions on physics or NavMesh in EditMode.

### PlayMode integration tests

For scene interactions, physics, NavMesh and UI, using `[UnityTest]`:

- Instantiate prefabs or load scenes in `[SetUp]` / `[UnitySetUp]`, destroy them in `[TearDown]` / `[UnityTearDown]`.
- Physics: step with `yield return new WaitForFixedUpdate()`, then assert the state change.
- Async or time-dependent flows: wait with a timeout so a test never hangs.
- Smoke tests: critical scene transitions complete with `LogAssert.NoUnexpectedReceived()`.
- PlayMode tests cost CI minutes: keep them for what EditMode cannot cover.

No tests for purely visual elements (shaders, animations, camera feel) or for Unity's own APIs.

## Git

- **Everything goes to `main`**, without branches or pull requests (solo project). One commit per ticket.
- **Before committing game code** (anything under `Assets/`): EditMode and PlayMode tests pass locally, and the Level 1 checks pass (`.github/scripts/check-meta.sh`, large files, `dotnet format whitespace --folder Assets/_Project --verify-no-changes`). CI runs again after the push.
- **Commits:** Conventional Commits, with the ticket number at the end when there is one: `<type>(<scope>): <description> (VS-<number>)`, e.g. `feat(kitchen): add oven recipe (VS-23)`. Tickets live in YouTrack (`moonbrew.youtrack.cloud`, project `VS`). Scope is the module and is optional. Types: `feat fix docs test refactor ci chore perf build style revert`. Lowercase, imperative, no trailing period, under 72 characters. Title only: no body, no trailers. Changes not tied to a ticket (configuration, packages, docs, CI, agent guidelines) have no ticket number.
- **Forbidden:** `push --force` on `main`, rewriting `main` history, secrets in the repository.
- The agent never commits or pushes unless explicitly asked. Otherwise it proposes the commit message.

## Working on a ticket

1. **Restate** the ticket and its acceptance criteria. Ask blocking questions before coding.
2. **Locate:** modules and assemblies affected, ScriptableObjects, scenes or prefabs to change.
3. **Propose** a short plan if the ticket is not trivial.
4. **Implement** as simply as possible in the right module, reusing existing events, clock and data.
5. **Write the tests** for the code produced. Flag what needs a manual test in the editor.
6. **Write the devlog entry:** one `.md` per finished feature in `Docs/Devlog/AAAA-MM-JJ_sujet.md`, following `Docs/08_Directives_Devlog — Moonbrew.md` and the `Docs/Devlog/_modele.md` template, and add it to the entry table in `Docs/07_Devlog — Moonbrew.md`. It goes in the same commit as the feature. Clips go in `Devlog/clips/` with the same prefix; no video editing unless asked.
7. **Deliver:** list of changed files, editor steps to do in Unity, and the commit message following the rules above.

Keep changes small: one ticket per change, no unrequested refactoring along the way. Do not add Unity packages or dependencies without asking. If a rule in this file blocks the task, say so instead of working around it.

## Definition of done

- Conventions respected, project compiles in Unity. If compilation was not verified, say so.
- Feature tested in the editor without errors, acceptance criteria met.
- New logic covered by EditMode tests; new prefab, physics or UI interaction covered by at least 1 PlayMode test.
- Devlog entry written in `Docs/Devlog/`.
- Level 1 checks pass (`.github/scripts/check-meta.sh`, large files, formatting).
- Committed and pushed to `main` once asked; CI checks green after the push.
