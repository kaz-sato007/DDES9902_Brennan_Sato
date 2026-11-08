# Assessment 1: Change Log

One entry per change, newest first. Each entry records the feedback, the change suggested in response, and exactly what was changed in the project.

---

## Change 5: Move the Current location button into the map grid (09 Nov 2026)

**Feedback**
The "Current location [C]" button should sit at the bottom right of the map grid itself, not at the bottom right of the map board.

**Suggested change**
Place the button inside the bottom-right corner of the gridded map area.

**What was changed**

*Scripts/Runtime/MapBoard.cs*
1. The button's position is now worked out from `mapFrame` (the gridded 500 m area of the map image): its right and bottom edges sit 4 mm inside the grid's bottom-right corner. If `mapFrame` is adjusted in the Inspector, the button follows it.
2. It stays 4 mm in front of the map, so the flicker fix from Change 4 still applies.

Note: the button now covers a small patch in the south-east corner of the map (about 110 m in from the east edge and 80 m up from the south edge). No exercise features are there. If the trainee stands in that corner, the location dot sits behind the button.

No other files were changed.

**Status**
- Tested in Unity: not yet
- Tested on headset: not yet
- Committed to GitHub: not yet

---

## Change 4: Fix the Current location button's position and flicker (09 Nov 2026)

**Feedback**
The "Current location [C]" button appeared in the centre of the map instead of the bottom right, and it kept flickering.

**Suggested change**
Move the button to the bottom right of the map board and stop the flicker.

**What was changed**

*Scripts/Runtime/MapBoard.cs*
1. Cause of both problems: Change 3 positioned the text inside the button rather than the button itself, so the button stayed at the centre of the board and in the same plane as the map image. Two surfaces at the same depth make the graphics card flip between them every frame (z-fighting), which shows as flicker.
2. The button's canvas is now positioned instead, under the compass at the bottom right of the board and 4 mm in front of the map, so it no longer shares a plane with the map.
3. The red dot and arrow of the location marker were moved slightly further in front of their white ring (0.4 mm to 0.8 mm) to prevent any similar flicker on the marker.

No other files were changed.

**Status**
- Tested in Unity: not yet
- Tested on headset: not yet
- Committed to GitHub: not yet

---

## Change 3: Current location button (09 Nov 2026)

**Feedback**
Trainees who get lost have no way to recover except wandering until they recognise the ground. Requested a "Current location" option, kept hidden behind a button so it is only used when needed.

**Suggested change**
Add a "Current location" button on the map. Pressing it shows the trainee's exact position (red dot) and facing direction on the map for 5 seconds, then it hides again. Unlimited uses, but every use is counted.

**What was changed**

*Scripts/Runtime/MapBoard.cs*
1. Added two Inspector settings under "Current location": `locationShowSeconds` (5 s) and `mapFrame` (where the 500 m map frame sits inside the map image, taken from make_map.py: x 0.06, y 0.255, width 0.88, height 0.704).
2. The board now builds a hidden "CurrentLocation" marker on the map: a red dot on a light ring, with a short red arrow showing which way the trainee is facing. It uses the existing compass needle and pole materials, so no new assets.
3. The board now builds a small "Current location [C]" button under the compass. It shows how many times it has been used ("used 0") and turns red while the marker is showing.
4. New methods: `SetLocationKey` (shows "C" on screen or "X" in VR on the button), `ShowLocation` (opens the marker for 5 s and returns whether this press counts as a new check), `LocationVisible`, and `UpdateLocationMarker`.
5. While showing, the marker follows the trainee live: world position is converted to a point on the printed map using the terrain's size and position, and the arrow turns with the trainee. It pops in, pulses gently so the eye finds it, and shrinks away over the last 0.4 s.
6. Pressing again while it is already showing restarts the 5 s timer but is not counted as another check.

*Scripts/Runtime/LandNavPlayerBridge.cs*
1. Flat screen: the **C** key shows the current location (works with the map raised or lowered).
2. VR: the **X** button on the left controller (the map hand) shows the current location, with a light haptic pulse.
3. The new VR input is released when the scene closes.

*Scripts/Runtime/SessionManager.cs*
1. Counts location checks (`OnLocationCheck`, `LocationChecks`) and writes each one to the Console with the time, for example "[LandNav] Location check 2 at 184.3 s" (behavioural data for Assessment 2).
2. The "EXERCISE COMPLETE" message now includes the number of location checks.

*Scripts/Runtime/WorldUI.cs*
1. Added `CreateLabel`, a small centred text panel used for the button.

No scenes, prefabs or other scripts were changed. Nothing needs to be re-run in the scene builder: the marker and button are created when the scene plays.

**Status**
- Tested in Unity: not yet
- Tested on headset: not yet
- Committed to GitHub: not yet

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
