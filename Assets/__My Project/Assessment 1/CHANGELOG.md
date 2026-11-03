# Assessment 1: Change Log

One entry per change, newest first. Each entry records the feedback, the change suggested in response, and exactly what was changed in the project.

---

## Change 2: Slide the map instead of looking around it (03 Nov 2026)

**Feedback**
After zooming in, reading other parts of the map meant looking up and down with the mouse, which made the tester feel slightly sick.

**Suggested change**
Keep the view still and shift the map itself up and down instead.

**What was changed**

*Scripts/Runtime/LandNavPlayerBridge.cs*
1. Added one Inspector setting under "Map zoom": `mapPanKeySpeed` (0.8, share of the screen per second the keys slide the map).
2. Added internal variables: `_pan` (the point on the map held at the centre of view) and `_walker` (the EZPZ FirstPersonController, which handles walking and mouse look). Removed `_raisedYaw` (no longer needed).
3. Flat-screen setup now finds the EZPZ FirstPersonController on the flat player so it can be paused. Controls hint changed to: "M map (mouse or WASD to slide, scroll or +/- to zoom) | 1-4 or Q/E flag | F plant | G pull up".
4. Rewrote the raised-map part of `UpdateFlatMap` (replaces Change 1, item 6):
    - Raising the map pauses the FirstPersonController, so the player halts and the camera does not move at all while the map is up, as when stopping to read a real map.
    - The map is attached to the view again and starts centred on the whole board.
    - Moving the mouse slides the map 1:1 on screen; W/A/S/D or the arrow keys slide it at a steady speed. Moving "up" brings the upper part of the map into view.
    - The map cannot be slid past its edges, and stays centred while it already fits the screen.
    - Zoom (scroll or +/-) is unchanged and keeps the same spot in the centre as it zooms.
    - Lowering the map restores walking and looking, and resets zoom and position.

No other files were changed. VR is unchanged: in the headset the map is moved by moving your hand, so there is no camera motion to remove.

**Status**
- Tested in Unity: not yet
- Tested on headset: not applicable (flat-screen change only)
- Committed to GitHub: not yet

---

## Change 1: Map zoom (03 Nov 2026)

**Feedback**
Testers found the details on the map too small to read when it was raised.

**Suggested change**
Make the map bigger, or let users zoom in and out of it.

**What was changed**

*Scripts/Runtime/LandNavPlayerBridge.cs*
1. Added four Inspector settings under "Map zoom" on LandNav Kit: `flatMapDistance` (0.6 m), `flatMapFit` (0.92), `maxZoom` (4), `vrMaxZoom` (2.5).
2. Added internal variables for zoom level, raised state, the heading the map is held at, the camera, and a VR left-trigger input.
3. Flat-screen setup now stores the camera so the map can be sized to the screen. Controls hint changed to: "M map, scroll or +/- to zoom | 1-4 or Q/E flag | F plant | G pull up".
4. VR setup now listens to the left trigger. Controls hint changed to: "Left trigger enlarge map | A switch flag | Right trigger plant | B pull up".
5. Replaced the old raise/lower code (map fixed in size and stuck to the camera) with a call to a new method, `UpdateFlatMap`.
6. New method `UpdateFlatMap`:
    - Lowered: map returns to the hip at normal size; zoom resets to 1x.
    - Raised: map is held 0.6 m in front of the eyes at the compass heading the player faced when raising it, so the mouse can look across it. At 1x the whole board fills 92% of the screen height (sized from the camera's field of view). Mouse wheel zooms 15% per step and +/- keys 25% per press, between 1x and 4x.
7. VR: the map in the left hand grows smoothly from 1x (trigger released) to 2.5x (trigger fully squeezed).
8. The new trigger input is released when the scene closes.

*Scripts/Runtime/MapBoard.cs*
1. Added two read-only helpers: `BoardSize` (map plus status strip, metres) and `BoardCentreOffset` (0.075 m), used to fit and centre the board on screen.

No scenes, prefabs or other scripts were changed. The old `_mapRaised` anchor is still created but no longer used (harmless).

**Status**
- Tested in Unity: not yet
- Tested on headset: not yet
- Committed to GitHub: not yet
