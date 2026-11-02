# Follow Windows Default Output Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the app follow the current Windows default playback endpoint whenever “Use only when this is the default output” is enabled, and keep the visible selection/status in sync.

**Architecture:** The engine resolves its effective output dynamically from `IAudioDeviceCatalog.GetDefaultDeviceId()` in default-only mode and continues using `SelectedDeviceId` in manual mode. When that effective endpoint changes, it stops the old renderer, resets the prior endpoint's idle history, and re-evaluates/restarts on the new endpoint. The tray context refreshes the visible form at a limited rate and reloads the endpoint list only after the effective ID changes; activating the form also synchronizes it immediately.

**Tech Stack:** .NET 10, WinForms, NAudio Core Audio, existing self-contained `win-x64` publish.

**Spec:** `docs/superpowers/specs/2026-11-02-follow-windows-default-output-design.md`

## Global Constraints

- With `OnlyWhenDefault == true`, the current Windows default playback endpoint is the effective target.
- With `OnlyWhenDefault == false`, preserve manual `SelectedDeviceId` targeting.
- On target changes, do not leave a renderer attached to the former endpoint.
- The visible destination must match the engine's effective destination.
- Add no dependencies and keep endpoint enumeration out of the engine's 100 ms loop.
- The local user instruction says not to add or run tests unless explicitly requested. Use build and non-test checks only; list manual audio scenarios without claiming they were run.
- Preserve the existing uncommitted modification in `tests/SpeakerKeepAwake.Tests/Core/KeepAliveEngineTests.cs`; do not stage, alter, or discard it as part of this plan.

## Review Focus

- Default changes while the app is in the tray: engine target changes even though the form is hidden; verify manually by switching FXSound to WZ BT5.
- Default changes while the form is open: displayed destination and engine target converge within the UI refresh interval; verify manually.
- Old endpoint becomes inactive: renderer stops/rebinds without repeatedly reporting it as selected; verify manually by turning FXSound off.
- Manual mode: changing the Windows default does not change the selected/active app destination; verify manually.
- No default endpoint is available: app waits without emitting and displays a useful waiting state; verify through code-path review and a build, since the local instruction disallows adding/running tests.

---

### Task 1: Resolve the effective engine endpoint dynamically

**Files:**
- Modify: `src/SpeakerKeepAwake/Core/KeepAliveEngine.cs`
- Modify: `src/SpeakerKeepAwake/Audio/AudioDeviceCatalog.cs` only if a narrow helper is needed to avoid duplicate default lookups.

**Interfaces:**
- Consumes: `IAudioDeviceCatalog.GetDefaultDeviceId()`, `IsDevicePresent(string?)`, current `AppSettings`.
- Produces: a public read-only `EffectiveDeviceId` (nullable string) representing the endpoint the engine is currently targeting, so the tray context can display the same target.

- [ ] **Step 1: Resolve the target at each engine tick.** If default-only mode is enabled, choose the current default ID; otherwise choose `SelectedDeviceId`. If the resolved ID is null or inactive, stop the renderer, update the controller to waiting, and clear `EffectiveDeviceId`.
- [ ] **Step 2: Rebind when the target changes.** Compare the resolved ID with `EffectiveDeviceId`; stop the previous renderer before assigning the new ID and reset `_controller` with the configured idle timeout so elapsed audio on the prior endpoint is not carried over. Then continue the normal activity/keep-alive state flow against the new endpoint. Do not restart the renderer every tick when the ID is unchanged.
- [ ] **Step 3: Keep startup/retry paths on the effective ID.** Ensure `TryStart` uses `EffectiveDeviceId`, including when a default changes while the engine is already keeping the old endpoint awake.
- [ ] **Step 4: Review the engine flow against the five manual cases in Review Focus.** Confirm missing default, disappeared old endpoint, default switch, and manual mode do not leave stale renderer state.

### Task 2: Keep the visible output in sync without frequent enumeration

**Files:**
- Modify: `src/SpeakerKeepAwake/UI/SettingsForm.cs`
- Modify: `src/SpeakerKeepAwake/TrayApplicationContext.cs`

**Interfaces:**
- Consumes: `KeepAliveEngine.EffectiveDeviceId` from Task 1 and current `AppSettings.OnlyWhenDefault`.
- Produces: a form update method that accepts the effective device ID and refreshes the dropdown/list only when the effective ID changed.

- [ ] **Step 1: Add a bounded UI refresh.** In `TrayApplicationContext.Tick`, send the engine's effective ID to the form at most once per second while it is visible. The form uses that ID as the displayed choice and enumerates active endpoints only when the ID changes. When the form is activated, synchronize immediately. The UI must not make its own Core Audio default lookup.
- [ ] **Step 2: Preserve manual selection.** When default-only mode is disabled, do not replace the user's dropdown choice with the Windows default; retain existing refresh-button behavior.
- [ ] **Step 3: Show unavailable/default-waiting clearly.** If the default ID cannot be resolved or is no longer active, clear the stale selection and show a concise waiting explanation; restore the matching current default when it returns.
- [ ] **Step 4: Check UI behavior for open and hidden windows.** The engine must follow changes while hidden; when reopened, the form must immediately display the engine's current target.

### Task 3: Document and package the behavior

**Files:**
- Modify: `README.md`
- Modify: `src/SpeakerKeepAwake/SpeakerKeepAwake.csproj`
- Deliver: `outputs/SpeakerKeepAwake.exe`
- Deliver: `outputs/SpeakerKeepAwake-win-x64.zip`

**Interfaces:**
- Consumes: completed engine/UI behavior from Tasks 1–2.
- Produces: release package with an incremented patch version and usage text explaining automatic default-following versus manual selection.

- [ ] **Step 1: Update README wording.** Explain that enabling default-only mode follows the current Windows output, while disabling it pins the selected endpoint.
- [ ] **Step 2: Increment the patch version** in `SpeakerKeepAwake.csproj`.
- [ ] **Step 3: Build and publish the self-contained `win-x64` executable.** Run `dotnet build .\SpeakerKeepAwake.slnx -c Release --no-restore` and the existing single-file publish command; require exit code 0 and no build warnings/errors.
- [ ] **Step 4: Replace the installed executable and rebuild the ZIP** with the executable and README, preserving `%LOCALAPPDATA%\SpeakerKeepAwake\settings.json`.
- [ ] **Step 5: Restart one installed app instance** and confirm executable version, process path, ZIP entries, and clean `git status` apart from the pre-existing test modification.

## Commit Boundaries

- Task 1: `fix: follow current Windows default output`
- Task 2: `fix: keep selected output in sync with Windows default`
- Task 3: `docs: explain automatic output switching`
