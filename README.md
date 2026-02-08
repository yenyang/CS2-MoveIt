# Move It (Alpha) mod — Cities: Skylines II

Move It (Alpha) is a **work-in-progress** tool that lets selected objects be moved, copied, pasted, manipulated, and deleted.

Supported (current):
- Buildings
- Trees, props, decals
- Network nodes
- Network segment curves
- Standalone Surfaces

---

## Ways to Help

- If you want to help in some way, but too busy, then send coffee; it's much appreciated: donations linked on PDX [PayPal](https://www.paypal.com/paypalme/YenyangsMods) or [Buy Me A Coffee](https://buymeacoffee.com/yenyang_mods). <br>
- Collaborators: submit new code with github Pull-requests
- Testers: test + break the mod and give reports/logs
- Translators: see below, use crowdin.

---

## Quick Start

- Press **M**, or click the **Move It** icon at the bottom-right to enable the tool.

---

## Project Status (Read This)

This is an **ALPHA** build under active development by **Yenyang**, continued **with permission from Quboid**.

- Expect bugs and missing features.
- **Undo/Redo is mostly broken**.
- Use with caution and **back up saves** before testing.

---

## Dependencies

- **Unified Icon Library**

---

## Hotkeys

| Action | Shortcut |
|---|---|
| Toggle Move It tool | `M` |
| Toggle Marquee selection mode | `Ctrl + M` |
| Enter/Exit Manipulation Mode | `Alt + M` |
| Enter Manipulation Mode on object | `Alt + Click` |
| Align to Terrain Height | `Ctrl + G` |
| Align to Object Height | `Ctrl + H` |
| Rotate at Center | `Alt + A` |
| Rotate in-Place | `Shift + A` |
| Copy | `Ctrl + C` |
| Delete | `Del` |
| Toggle Follow Terrain | `Alt + T` |

> Some shortcuts can be rebound in Options → Hotkeys.

---

## Selection

### Basic selection
- **Left-click**: select object
- **Shift + left-click**:
  - unselected object: add to selection
  - selected object: remove from selection
- **Right-click**: clear selection and hide control points

### Marquee selection (rectangle select)
- Toggle with **Ctrl + M** or via Options.
- When enabled: **Left-click + drag** to draw a selection rectangle.

### Filters
Open the **Filters** foldout menu to limit what types are selectable.

- Top checkbox toggles all filters on/off
- Right-click a filter to enable it and disable all others
- If the Filters foldout menu is closed, filtering does not apply

---

## Modes

- **Single Selection**: click to select, Shift+click add/remove
- **Marquee Selection**: drag a selection rectangle (single clicks still work)
- **Manipulation Mode**: adjust supported objects (currently segments)
- **Copy**: duplicates selected entities
- **Delete**: immediately deletes selected entities (**cannot currently be undone**)

---

## Moving & Rotating

- **Left-click + drag**: move selection
- **Right-click + drag**: rotate selection
- Hold **Ctrl** while moving for fine control (low sensitivity + hides selection overlays)

Snapping is currently broken / not implemented.

---

## Copy

1. Make a selection.
2. Press **Copy**.
3. Move the copy with the mouse.
4. **Right-click drag** to rotate.
5. Use **Page Up/Down** etc. to raise/lower.
6. **Left-click** to apply.

Notes:
- Selecting network nodes also selects all connected segments.
- With compatible versions of **Anarchy**, custom components may be copied too.

---

## Delete

1. Make a selection.
2. Press **Delete**.
3. Selected entities are deleted immediately (**cannot currently be undone**).

Note:
- Selecting network nodes will also delete connected segments.

---

## Follow Terrain Toggle

When Moving, Copying, or using Manipulation Mode, displaced objects can follow terrain height across the movement.

- Trees on the ground stay on the ground wherever they are moved.

---

## Manipulation Mode

Enter:
- click the manipulation icon, or
- **Alt + M**, or
- **Alt + click** on a supported object

Exit:
- choose a different mode, or
- **Alt + M**, or
- right-click with nothing selected

Current scope:
- Only **segments** can be manipulated for now.
- Segment manipulation moves control points in all axes (node connections + curve points).

Extreme alterations may cause visual artifacts or break traffic routing.

---

## Toolbox

Some toolbox tools still follow Quboid’s original setup and may be refactored later.

- **Align to Terrain Height** (`Ctrl + G`)  
  Moves selection up/down to terrain height. Does not work with objects that affect terrain (buildings, ground-level networks).

- **Align to Object Height** (`Ctrl + H`)  
  Moves selection up/down to the height of the clicked object.

- **Rotate at Center** (`Alt + A`)  
  Rotates selection to match the clicked object, around the selection’s center.

- **Rotate in-Place** (`Shift + A`)  
  Rotates selection to match the clicked object, without changing position.

---

## Options

- Invert Rotation
- Extended Debug Logging
- Save Logs To Desktop (Skyve can also export logs)
- Advanced: Show Debug Panel
- Advanced: Hide Move It Icon
- Advanced: Show Debug Lines
- Hotkeys: view and change key bindings

Save often. Save different.

---

## Known Issues

- Align to terrain height does not work with terrain-affecting objects (buildings, on-ground roads).
- Page Up/Down do not work in the editor. Use **Numpad 9/3** or rebind.
- Undo/Redo is mostly broken.
- Overlays may have imperfections.
- Snapping is broken / not implemented.
- Don't move, delete, or copy utility structures such as water pumps because one of their utilty nodes is not handled correctly yet.

---

## Credits

- **Current Author:** Yenyang
- **Original Author:** Quboid  
- Code Review and Contributions: Honu

Thanks to: SamSamTS, Krzychu124, T.D.W., Klyte45, REV0, BadPeanut, Sully, Algernon, CanadianMoosePlays!

Icons from SVG Repo, and from WishForge.Games under CC Attribution License.  
Unified Icon Library icons by Chameleon TBN

---

## Translations

Volunteers are welcome.

Discussion happens in the **Cities: Skylines Modding Discord**:
`CSM Community Projects -> move-it -> Move It: Translations`  
(CrowdIn link is available there.)

### Translators 
(language names in English)

- Polish: Pingwin_PL, karmel68, Spanish: elGendo87, Portuguese: ti4goc, French: Morgan, aidan taylor, prospr, 
Portuguese (Brazil): nemoriees, 0belix, Japanese: ExpedientFalcon, allegretic, Chinese (Simplified): Nullpinter, RilkeXS, 
Chinese (Traditional): allegretic, Dutch: MyNameIsntRealHere, WISSAH, Russian: HSneon, Korean: acelion19

(language names in native form)

- Polski: Pingwin_PL, karmel68, Español: elGendo87, Português: ti4goc, Français: Morgan, aidan taylor, prospr, 
Português (Brasil): nemoriees, 0belix, 日本語: ExpedientFalcon, allegretic, 简体中文: Nullpinter, RilkeXS, 繁體中文: allegretic, 
Nederlands: MyNameIsntRealHere, WISSAH, Русский: HSneon, 한국어: acelion19

