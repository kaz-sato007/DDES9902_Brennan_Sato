# Iteration Notes: Design Rationale

This file explains why each design change was made and the literature behind it, for use in the Assessment 2 report. The technical detail of each change is in CHANGELOG.md. Newest iteration first.

---

## Iteration 5: Clear end and readable feedback (09 Nov 2026)

**What we observed**
A Version 1 tester was not sure when they had finished and asked for a completion indicator. In a walkthrough of the v3 build, the feedback sign sat above eye level and too close to read in one look, with its title cut off.

**Design response**
1. An "Exercise complete" panel appears when the fourth flag is planted correctly, with time, plants, location checks and a per-flag summary. It stays in the world and only glides back into view if the trainee turns or moves well away.
2. The feedback sign now appears at eye level about 2.4 m from the trainee, beyond the flag and clear of its cloth.

**Rationale**
1. *Visibility of system status.* Users should always know what is going on (Nielsen 1994). Without a clear end state, trainees cannot tell the task is done; without a readable sign, the feedback that makes the exercise work is easy to miss.
2. *Comfort.* The completion panel is world-fixed with a lazy follow rather than locked to the head, consistent with the rest frame reasoning in Iteration 2 (Prothero 1998).

---

## Iteration 4: Walking restored while the map is up (09 Nov 2026)

**What we observed**
On review, the decision in Iteration 2 to stop the player moving while the map is raised rested on an untested assumption. The nausea reported in Iteration 2 came from turning the camera to scan a zoomed map, not from walking. Stopping movement also removed the ability to read the map on the move, which some trainees may want.

**Design response**
1. Walking (W/A/S/D or the arrows) works again while the map is raised.
2. Mouse look stays paused while the map is up, and the mouse still slides the map. This keeps the fix that addressed the observed problem: the camera does not turn while the trainee reads.
3. W/A/S/D no longer slide the map, because they now walk. Sliding is by mouse only, and zoom is unchanged.

**Rationale**
1. *Keep the change that matches the evidence.* Sensory conflict from turning the view (Reason and Brand 1975; LaViola 2000), amplified by magnification (Draper et al. 2001), explains the reported discomfort. Pausing mouse look removes that cause. Pausing walking was not supported by the observation or the sources.
2. *A raised map may act as a stable foreground.* A fixed object in the foreground of the view, such as a cockpit frame or a virtual nose, has been reported to reduce simulator sickness by giving the eye a stable reference (Whittinghill et al. 2015). A map held in front of the eyes while walking could work the same way. This is a hypothesis to test, not a claim.
3. *User control.* Letting trainees choose whether to halt or read on the move gives them more control over the experience, and halting remains available as the field practice it reflects.

**How to verify in the next round of testing**
Record whether testers walk with the map up, and include the discomfort rating (0 to 10, or the SSQ by Kennedy et al. 1993). Compare discomfort between testers who read on the move and those who halt.

---

## Iteration 3: Current location button (09 Nov 2026)

**What we observed**
A trainee who loses track of their position has no way to recover other than wandering until the ground looks familiar. This is frustrating and wastes the session.

**Design response**
A "Current location" button on the map (C on screen, X in VR). Each press shows an exact red dot with a facing arrow for 5 seconds, then hides it again. Uses are unlimited but counted, shown on the button and in the completion message, and logged with a timestamp.

**Rationale**
1. *Support that does not replace the skill.* Turn by turn GPS style aids weaken spatial learning. Ishikawa et al. (2008) found that people navigating with a GPS device travelled further, stopped more often and formed poorer mental maps of the route than people using a paper map. A permanent "you are here" dot would let trainees skip the map to ground matching the exercise exists to teach. Keeping it hidden, brief and opt in means the trainee must still read the map and terrain, and only falls back on the marker when genuinely lost.
2. *Scaffolding.* The button acts as scaffolding in the sense of Wood, Bruner and Ross (1976): help that is available when the learner cannot progress alone, which they rely on less as their skill grows. Counting uses makes that reliance visible to the trainee and to the instructor.
3. *Exact dot, by choice.* An approximate circle was considered, since it would force the trainee to finish fixing their position from the contours. The exact dot was chosen to keep the aid simple and unambiguous for novices. The time limit and the counter keep it from becoming a crutch.
4. *Behavioural data.* The number of location checks, and when they happen, is a direct measure of how often testers became lost. It can be compared across prototype versions in Assessment 2 testing.

**How to verify in the next round of testing**
Record location checks per tester alongside completion time and wrong plants. Ask whether the button helped them recover, and whether they felt it made the exercise too easy.

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
   *Correction (09 Nov 2026):* the claim that walking with the map up adds conflicting motion was an inference, not a finding from the sources cited. None of them tests walking while reading a map, and the discomfort actually observed came from turning the camera, not from walking. Walking was restored in Iteration 4, which sets out the revised reasoning.

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

Ishikawa, T., Fujiwara, H., Imai, O. and Okabe, A. (2008) 'Wayfinding with a GPS-based mobile navigation system: a comparison with maps and direct experience', *Journal of Environmental Psychology*, 28(1), pp. 74–82.

Kennedy, R.S., Lane, N.E., Berbaum, K.S. and Lilienthal, M.G. (1993) 'Simulator Sickness Questionnaire: an enhanced method for quantifying simulator sickness', *The International Journal of Aviation Psychology*, 3(3), pp. 203–220.

LaViola, J.J. (2000) 'A discussion of cybersickness in virtual environments', *ACM SIGCHI Bulletin*, 32(1), pp. 47–56.

Legge, G.E. and Bigelow, C.A. (2011) 'Does print size matter for reading? A review of findings from vision science and typography', *Journal of Vision*, 11(5), article 8.

Lin, J.J.W., Duh, H.B.L., Parker, D.E., Abi-Rached, H. and Furness, T.A. (2002) 'Effects of field of view on presence, enjoyment, memory, and simulator sickness in a virtual environment', *Proceedings IEEE Virtual Reality 2002*, pp. 164–171.

Nielsen, J. (1994) 'Enhancing the explanatory power of usability heuristics', *Proceedings of the SIGCHI Conference on Human Factors in Computing Systems (CHI '94)*, pp. 152–158.

Prothero, J.D. (1998) *The role of rest frames in vection, presence and motion sickness*. PhD thesis. University of Washington.

Reason, J.T. and Brand, J.J. (1975) *Motion sickness*. London: Academic Press.

Shneiderman, B. (1996) 'The eyes have it: a task by data type taxonomy for information visualizations', *Proceedings 1996 IEEE Symposium on Visual Languages*, pp. 336–343.

Whittinghill, D.M., Ziegler, B., Case, T. and Moore, B. (2015) 'Nasum virtualis: a simple technique for reducing simulator sickness', paper presented at the *Game Developers Conference (GDC) 2015*, San Francisco, 2–6 March.

Wood, D., Bruner, J.S. and Ross, G. (1976) 'The role of tutoring in problem solving', *Journal of Child Psychology and Psychiatry*, 17(2), pp. 89–100.

Note: these references were compiled without checking each one against the original source. Confirm page numbers and read at least each abstract before citing in the report.
