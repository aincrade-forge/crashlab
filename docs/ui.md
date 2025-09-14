UI Configuration

- Scene-driven UI: attach `CrashLab.UI.CrashUIBuilder` to a layout container (e.g., with a `VerticalLayoutGroup`).
- Assign `Content` (RectTransform) and a `CrashUIButton` prefab.
- Edit the serialized list of buttons (label + action) in the Inspector. The builder will instantiate one prefab per entry and wire it to `CrashActions` handlers.
- The legacy auto-installed `CrashUI` overlay is deprecated and not created automatically at runtime.

Quick steps

1) Create a UI Canvas/panel and layout group.
2) Create a prefab with a `Button` and a `Text`, add `CrashUIButton`, assign the `Text` to the script field.
3) Add `CrashUIBuilder` to your container, assign the content and the prefab, and customize the Buttons list.
4) Press Play to see buttons populate and trigger actions.
