# Assessment 1: Change Log

One entry per change, newest first. Each entry records the feedback, the change suggested in response, and exactly what was changed in the project.

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
