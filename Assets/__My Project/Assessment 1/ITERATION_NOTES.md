# Iteration Notes: Design Rationale

This file explains why each design change was made and the literature behind it, for use in the Assessment 2 report. The technical detail of each change is in CHANGELOG.md. Newest iteration first.

---

## Iteration 2: Slide the map, hold the player still (03 Nov 2026)

**What we observed**
With Iteration 1 in place, reading another part of a zoomed map meant turning the camera up and down with the mouse. The tester (the designer, n = 1) reported mild nausea while doing this.

**Hypothesis: why zooming made it worse**
Zooming was not the problem by itself. The problem was what zooming led the user to do, and the conditions under which they did it.

1. *Visual and vestibular mismatch.* The leading explanation for motion sickness is sensory conflict. It occurs when the eyes report self motion that the inner ear (vestibular system) does not feel (Reason and Brand 1975). The same mechanism is the most widely cited cause of cybersickness in virtual environments (LaViola 2000; Chang, Kim and Yoo 2020). Turning the camera to scan the map swept the whole view while the tester sat still. This is exactly that mismatch.

2. *Magnification amplifies apparent motion.* Draper et al. (2001) found that showing a virtual scene at a scale other than 1:1 increased simulator sickness. A magnified image moves further and faster for the same head or camera movement than the brain expects. At up to 4x zoom, each small mouse movement became a large, fast sweep of fine map detail.

3. *A large share of the view was in motion, including the periphery.* Peripheral vision dominates the sense of self motion (vection), while central vision mainly judges the motion of objects (Brandt, Dichgans and Koenig 1973). A wider field of view showing motion is associated with more sickness (Lin et al. 2002). The zoomed map filled most of the screen, and the terrain behind it also moved as the camera turned, so the periphery received a strong motion signal.

Status of this hypothesis: it is based on one person's report and on the literature above. It has not yet been tested with other users. Individual susceptibility to motion sickness varies widely (Chang, Kim and Yoo 2020), so it must be confirmed with structured testing.

**Design response**

1. *The map moves instead of the view.* Mouse movement and W/A/S/D now slide the map across the screen while the camera stays fixed. Any motion is confined to the map, an object the user is looking at, rather than the whole visual field. Brandt, Dichgans and Koenig (1973) showed that motion seen centrally is read mainly as object motion, not self motion. A stationary surrounding scene also gives the user a stable reference, or "rest frame", against which the moving map is judged. Prothero (1998) argues that a stable rest frame reduces vection and sickness. Fernandes and Feiner (2016) showed that reducing optic flow in the periphery reduces VR sickness while keeping users engaged.

2. *The player cannot move while the map is up.* Raising the map now pauses walking and mouse look. This removes the second source of conflicting visual motion: walking forward while the eyes are fixed on a handheld map. It keeps the whole view still apart from the map, which preserves the rest frame described above (Prothero 1998). It also matches field practice, where soldiers halt to read a map rather than reading on the move.

3. *Zoom was kept.* Iteration 1 solved a real legibility problem (below). Iteration 2 changes how a zoomed map is explored, not whether it can be zoomed.

**Trade off accepted**
Users can no longer walk while reading the map. This is deliberate. Comfort and the halt to read habit outweigh the convenience of reading on the move.

**How to verify in the next round of testing**
Ask testers of both versions to rate discomfort, either with a single 0 to 10 rating after the task or with the Simulator Sickness Questionnaire (Kennedy et al. 1993). Compare the scores between the Iteration 1 and Iteration 2 builds.

---

## Iteration 1: Map zoom (03 Nov 2026)

**What we observed**
Testers reported that the details on the map were too small to read when it was raised.

**Why this matters**
Reading speed and accuracy fall sharply once text is smaller than a "critical print size", measured as the angle the text covers at the eye (Legge and Bigelow 2011). The map was drawn at a fixed physical size in the scene, so on a typical screen or headset its contour labels and feature markers fell below a comfortably legible size. In a terrain identification task, an unreadable map undermines the core skill being trained.

**Design response**
Let the user zoom the raised map between 1x and 4x (scroll or +/- on screen; left trigger in VR). This follows the established information visualisation principle "overview first, zoom and filter, then details on demand" (Shneiderman 1996). The user sees the whole map at 1x to orient themselves, then zooms in to read a specific feature. Zooming also gives the user control over the display instead of fixing one size for everyone, which accommodates differences in eyesight and screen size.

**What it led to**
Once zoomed, the map no longer fitted on screen, and the user had to turn the camera to scan it. This caused the discomfort addressed in Iteration 2.

---

## References

Brandt, T., Dichgans, J. and Koenig, E. (1973) 'Differential effects of central versus peripheral vision on egocentric and exocentric motion perception', *Experimental Brain Research*, 16(5), pp. 476–491.

Chang, E., Kim, H.T. and Yoo, B. (2020) 'Virtual reality sickness: a review of causes and measurements', *International Journal of Human–Computer Interaction*, 36(17), pp. 1658–1682.

Draper, M.H., Viirre, E.S., Furness, T.A. and Gawron, V.J. (2001) 'Effects of image scale and system time delay on simulator sickness within head-coupled virtual environments', *Human Factors*, 43(1), pp. 129–146.

Fernandes, A.S. and Feiner, S.K. (2016) 'Combating VR sickness through subtle dynamic field-of-view modification', *2016 IEEE Symposium on 3D User Interfaces (3DUI)*, pp. 201–210.

Kennedy, R.S., Lane, N.E., Berbaum, K.S. and Lilienthal, M.G. (1993) 'Simulator Sickness Questionnaire: an enhanced method for quantifying simulator sickness', *The International Journal of Aviation Psychology*, 3(3), pp. 203–220.

LaViola, J.J. (2000) 'A discussion of cybersickness in virtual environments', *ACM SIGCHI Bulletin*, 32(1), pp. 47–56.

Legge, G.E. and Bigelow, C.A. (2011) 'Does print size matter for reading? A review of findings from vision science and typography', *Journal of Vision*, 11(5), article 8.

Lin, J.J.W., Duh, H.B.L., Parker, D.E., Abi-Rached, H. and Furness, T.A. (2002) 'Effects of field of view on presence, enjoyment, memory, and simulator sickness in a virtual environment', *Proceedings IEEE Virtual Reality 2002*, pp. 164–171.

Prothero, J.D. (1998) *The role of rest frames in vection, presence and motion sickness*. PhD thesis. University of Washington.

Reason, J.T. and Brand, J.J. (1975) *Motion sickness*. London: Academic Press.

Shneiderman, B. (1996) 'The eyes have it: a task by data type taxonomy for information visualizations', *Proceedings 1996 IEEE Symposium on Visual Languages*, pp. 336–343.

Note: these references were compiled without checking each one against the original source. Confirm page numbers and read at least each abstract before citing in the report.
