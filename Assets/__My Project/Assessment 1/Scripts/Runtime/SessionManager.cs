using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LandNav
{
    /// <summary>Tracks the exercise: time, attempts, and which flags are correctly placed.</summary>
    public class SessionManager : MonoBehaviour
    {
        public FlagKit kit;
        public MapBoard map;
        public string controlsHint = "";

        [Header("Completion pop-up")]
        [Tooltip("How far in front of the trainee the completion panel floats, metres")]
        public float popupDistance = 2.2f;

        public static SessionManager Instance { get; private set; }

        float _startTime;
        bool _complete;
        int _locationChecks;
        float _completeTime;
        AudioSource _audio;

        WorldUI.Panel _popup;
        bool _popupOpen, _popupFollowing;
        float _popupAge;
        InputAction _closeVR;

        void Awake() { Instance = this; }

        void Start()
        {
            _startTime = Time.time;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
        }

        public void OnFlagPlanted(Flag f)
        {
            if (!_complete && CorrectCount() == 4)
            {
                _complete = true;
                _completeTime = Time.time - _startTime;
                _audio.PlayOneShot(Tones.Complete);
                ShowCompletion();
            }
        }

        public void OnFlagPulled(Flag f) { }

        /// <summary>Called each time the trainee opens the 'Current location' marker.</summary>
        public void OnLocationCheck()
        {
            _locationChecks++;
            float t = Time.time - _startTime;
            Debug.Log($"[LandNav] Location check {_locationChecks} at {t:0.0} s");
        }

        public int LocationChecks => _locationChecks;

        int CorrectCount()
        {
            int n = 0;
            if (kit == null) return 0;
            foreach (var f in kit.Flags) if (f.state == Flag.State.Planted && f.plantedCorrectly) n++;
            return n;
        }

        int TotalAttempts()
        {
            int n = 0;
            if (kit == null) return 0;
            foreach (var f in kit.Flags) n += f.attempts;
            return n;
        }

        /// <summary>Open the "Exercise complete" panel in front of the trainee.</summary>
        void ShowCompletion()
        {
            if (_popup == null)
            {
                _popup = WorldUI.CreatePanel("CompletionPanel", null, new Vector2(1.1f, 0.8f),
                    new Color(0.04f, 0.26f, 0.12f, 0.93f), 64, 30);
                _closeVR = new InputAction(binding: "<XRController>{RightHand}/{PrimaryButton}");
                _closeVR.Enable();
            }

            float t = _completeTime;
            string time = $"{(int)(t / 60):00}:{(int)(t % 60):00}";
            int plants = TotalAttempts();
            var sb = new StringBuilder();
            sb.Append($"All four features identified.\n\nTime {time}    Plants {plants}    Location checks {_locationChecks}\n\n");
            foreach (var f in kit.Flags)
            {
                string feature = f.lastFeature != null ? f.lastFeature.displayName : "";
                sb.Append($"<color={WorldUI.Hex(f.colour)}>{f.colour}</color>  {feature}  ({f.attempts} plant{(f.attempts == 1 ? "" : "s")})\n");
            }
            sb.Append("\nWalk back to any flag to review it.\nPress Enter (A on the controller) to close.");
            _popup.title.text = "EXERCISE COMPLETE";
            _popup.body.text = sb.ToString();

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 fwd = cam.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                _popup.root.transform.position = cam.transform.position + fwd.normalized * popupDistance - Vector3.up * 0.05f;
            }
            _popupAge = 0f;
            _popupFollowing = false;
            _popupOpen = true;
            _popup.root.SetActive(true);
            Debug.Log($"[LandNav] Exercise complete in {t:0.0} s, {plants} plants, {_locationChecks} location checks");
        }

        /// <summary>
        /// Keep the panel readable without fixing it to the face: it stays put in the world and only
        /// glides back in front of the trainee once they have turned or walked well away from it.
        /// </summary>
        void UpdateCompletion()
        {
            var kb = Keyboard.current;
            bool close = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                         || (_closeVR != null && _closeVR.WasPressedThisFrame());
            if (close) { _popupOpen = false; _popup.root.SetActive(false); return; }

            var cam = Camera.main;
            if (cam == null) return;
            var tr = _popup.root.transform;
            Vector3 eye = cam.transform.position;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 target = eye + fwd * popupDistance - Vector3.up * 0.05f;

            Vector3 toPanel = tr.position - eye; toPanel.y = 0f;
            if (!_popupFollowing && (Vector3.Angle(fwd, toPanel) > 35f || Mathf.Abs(toPanel.magnitude - popupDistance) > 1f))
                _popupFollowing = true;
            if (_popupFollowing)
            {
                tr.position = Vector3.Lerp(tr.position, target, 1f - Mathf.Exp(-4f * Time.deltaTime));
                if ((tr.position - target).sqrMagnitude < 0.01f) _popupFollowing = false;
            }

            Vector3 look = tr.position - eye; look.y = 0f;
            if (look.sqrMagnitude > 1e-4f) tr.rotation = Quaternion.LookRotation(look);

            // gentle pop-in rather than appearing instantly
            _popupAge += Time.deltaTime;
            float s = Mathf.SmoothStep(0.7f, 1f, Mathf.Clamp01(_popupAge / 0.25f));
            tr.localScale = Vector3.one * 0.001f * s;
        }

        void OnDestroy() { _closeVR?.Dispose(); }

        void Update()
        {
            if (_popupOpen && _popup != null) UpdateCompletion();
            if (map == null || kit == null || kit.Flags.Count == 0) return;
            float t = _complete ? _completeTime : Time.time - _startTime;
            string time = $"{(int)(t / 60):00}:{(int)(t % 60):00}";

            if (_complete)
            {
                map.SetStatus("EXERCISE COMPLETE",
                    $"All four features identified in {time} with {TotalAttempts()} plants and {_locationChecks} location check{(_locationChecks == 1 ? "" : "s")}. Walk back to any flag to review its feature.");
                return;
            }

            var sb = new StringBuilder();
            foreach (var f in kit.Flags)
            {
                string mark = f.state == Flag.State.Planted ? (f.plantedCorrectly ? "OK" : "X") : (f.state == Flag.State.Held ? "in hand" : "-");
                sb.Append($"<color={WorldUI.Hex(f.colour)}>{f.colour}</color> {mark}   ");
            }
            sb.Append($"\n{controlsHint}");
            map.SetStatus($"Correct {CorrectCount()}/4    Time {time}", sb.ToString());
        }
    }
}
