UI Configuration

- Scene-driven UI: add `CrashLab.UI.CrashUIBuilder` to any GameObject.
- Assign a `CrashUIButton` prefab.
- Assign two target containers (RectTransform): `Crashes Content` and `Errors Content`.
- The builder auto-populates buttons into the matching container based on the action.
- The legacy auto-installed `CrashUI` overlay is deprecated and not created automatically at runtime.

Quick steps

1) Create a UI Canvas and two child panels (empty `RectTransform`s) — name them e.g. "Crashes" and "Errors".
2) Create a prefab with a `Button` and a `Text`, add `CrashUIButton`, assign the `Text` to the script field.
3) Add `CrashUIBuilder` anywhere in the scene, assign `Crashes Content`, `Errors Content`, and the button prefab.
4) Press Play to see buttons appear under the two panels.

Groups

- Two fixed groups are supported:
  - Crashes — crash triggers (managed unhandled, native crashes, etc.).
  - Errors — everything else (hangs, threading, OOM, IO/Data, diagnostics, scheduling).

Notes

- The optional `Content` field is a fallback: if one of the two targets isn’t assigned, buttons will be placed there; otherwise entries are skipped with a warning.
