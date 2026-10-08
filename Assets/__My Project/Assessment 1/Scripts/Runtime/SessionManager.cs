using System.Text;
using UnityEngine;

namespace LandNav
{
    /// <summary>Tracks the exercise: time, attempts, and which flags are correctly placed.</summary>
    public class SessionManager : MonoBehaviour
    {
        public FlagKit kit;
        public MapBoard map;
        public string controlsHint = "";

        public static SessionManager Instance { get; private set; }

        float _startTime;
        bool _complete;
        float _completeTime;
        AudioSource _audio;

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
            }
        }

        public void OnFlagPulled(Flag f) { }

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

        void Update()
        {
            if (map == null || kit == null || kit.Flags.Count == 0) return;
            float t = _complete ? _completeTime : Time.time - _startTime;
            string time = $"{(int)(t / 60):00}:{(int)(t % 60):00}";

            if (_complete)
            {
                map.SetStatus("EXERCISE COMPLETE",
                    $"All four features identified in {time} with {TotalAttempts()} plants. Walk back to any flag to review its feature.");
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
