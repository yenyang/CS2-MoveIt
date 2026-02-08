# Move It Alpha for Cities Skylines 2.

You can move, copy/paste, and delete trees, props, decals, buildings, nodes, surfaces and segment curves.

Press **M** or click on the Move It icon at the bottom right to enable the tool.

## Important Update on Development:
I am continuing the development of Move It with permission from Quboid. This is the ALPHA version of what I have been working on. There are big changes and this is still work-in-progress. Expect bugs. Some things don't work (i.e. Undo/Redo). Use with caution. Do not use on your favorite save file. Patience is appreciated. This will take time to develop.

## Dependencies
Unified Icon Library

## Donations
If you want to say thank you with a donation you can do so on Paypal or Buy Me A Coffee.

## Translations
I am looking for volunteers to help translate the mod into the other languages. For those interested please discuss the translation project in Cities Skylines Modding Discord. Thread: CSM Community Projects -> move-it -> Move It: Translations. CrowdIn Link available here.

## Selecting:

Left-click to select, shift+click on unselected object to add to selection or on selected object to remove from selection. Press Control+M or check options to toggle Marquee mode; when enabled you can drag out a rectangle to select multiple objects. Right-click to clear selection and hide all control points.

You can limit what types of objects are selected by opening the Filters foldout menu and unticking whatever you don't want. The tick box at the top toggles all filters on or off, and you can right-click on any filter to enable it and disable all others. If the Filters foldout menu is closed filtering doesn't apply.


## Mode Icons:
* Single Selection - Click on individual objects to select, shift+click to add an unselected object or to remove a selected object.
* Marquee Selection - Left+Click and drag to draw out a selection box. Single Selection clicks can also be used.
* Manipulation Mode - Alter aspects within an object.
* Copy - Copies selected entities. 
* Delete - Immediately deletes all selected entities. (Cannot currently be undone!)

## Moving and Rotating:

Left-Click drag to move selection, right-click drag to rotate selection. Snapping is currently broken. Remember this is Work-In-Progress.

Hold Control while moving for more precision by switching to low sensitivity and hiding selection overlays.

This version of Move It supports Previews and you can abort by pressing Escape although this will exit Move It tool currently. Error checks will be shown but currently do not prevent you from doing anything. With Compatible version of Anarchy use the keyboard shortcut to not check for Errors.

## Copying
Make a selection. Press Copy. Move the copy around with the mouse. Right click drag to rotate the copies. Page Up/Down etc. to raise and lower. Left click to apply a copy. 

With compatible version of Anarchy, custom components will be copied too. Hopefully other mods can and will add this type of support.

Selecting network nodes will also select all connected segments.

## Delete
Make a selection. Press Delete. Immediately deletes all selected entities. This cannot currently be undone.

Selecting network nodes will also select all connected segments.

## Follow Terrain Toggle
When Moving, Copying, or using Manipulation Mode. Whatever is being displaced will move up and down based on the height of the terrain from the start point to the end point for each thing that is moving. Trees on the ground stay on the ground wherever you move them.

## Manipulation:

To enter manipulation mode, click on the icon, press Alt+M, or Alt+click on a manipulatable object. To leave, choose a different mode, press Alt+M, or right-click with nothing selected to leave manipulation mode. For now, only segments can be manipulated.

Manipulating segments - you can move the control points in all three axis; the 2 node connections and the 2 curve points. This is very powerful, and extreme alterations will cause visual glitching and may break traffic routing. Snapping is probably broken right now.

## Toolbox
These still use Quboid's setup but I hope to eventually refactor these onto my setup.
* Align to Terrain Height [Control+G] - Selected objects move up or down to terrain height. This does **not** work with objects that affect terrain, which includes buildings and ground-level networks.
* Align to Object Height [Control+H] - Selected objects move up or down to the height of whatever object you click on.
* Rotate at Center [Alt+A] - Selected objects rotate to face the same direction as whatever object you click on, rotating around the selection's central point.
* Rotate in-Place [Shift+A] - Selected objects rotate to face the same direction as whatever object you click on, without changing their position.


## Options:
* Invert Rotation - Move It uses the same rotation direction that Cities 1 and Move It for Cities 1 used. If you prefer Cities 2's direction, you can invert it.
* Extended Debug Logging - Saves more information to the log file to help me hunt down errors.
* Save Logs To Desktop - Saves your current log files to your desktop so you can easily submit them with bug reports. You can also do this with Skyve.
* Advanced: Show Debug Panel - Show some technical information about Move It's current status. You probably don't want this.
* Advanced: Hide Move It Icon - If the icon is causing crashing issues and nothing else helps, enable this to hide the icon.
* Advanced: Show Debug Lines - Displays some debugging data.

Hotkeys: view and change any keyboard settings here.

Save often, save different.


## Known Issues
* Align to terrain height does not yet work with objects that affect terrain, e.g. buildings and on-ground roads.
* Page Up/Down do not work in the editor. Use Numpad 9 and 3 instead, or rebind the move up/down keys.
* Undo / Redo is mostly broken right now.
* Overlays are not perfect and will have some issues.
* Snapping is broken / not implemented.
* Don't move, delete, or copy utility structures such as water pumps because one of their utilty nodes is not handled correctly yet.

## Credits:
Current Author: Yenyang
Original Author: Quboid
Code Review and Contributions: Honu

Big thanks to SamSamTS, Krzychu124, T.D.W., Klyte45, REV0, BadPeanut, Sully, Algernon, CanadianMoosePlays!

Icons from SVG Repo, and from WishForge.Games under CC Attribution License.

Unified Icon Library icons by Chameleon TBN

Translators: Pingwin_PL and karmel68 (Polish), elGendo87 (Spanish), ti4goc (Portuguese), Morgan, aidan taylor and prospr (French), nemoriees and 0belix (Portugese, Brazilian), ExpedientFalcon and allegretic (Japenese), Nullpinter and RilkeXS (Chinese Simplified), allegretic (Chinese Traditional), MyNameIsntRealHere and WISSAH (Dutch), HSneon (Russian), acelion19 (Korean)