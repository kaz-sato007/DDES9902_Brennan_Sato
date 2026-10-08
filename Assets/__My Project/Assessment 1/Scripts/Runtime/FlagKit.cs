using System.Collections.Generic;
using UnityEngine;

namespace LandNav
{
    /// <summary>
    /// The four flags the trainee carries from the start. One is "in hand" at a time; the trainee
    /// switches between them freely and plants whichever one they judge belongs where they stand.
    /// A planted flag can be pulled up again (by standing next to it) to re-attempt.
    /// </summary>
    public class FlagKit : MonoBehaviour
    {
        public Material poleMaterial;
        public Material blueMaterial, redMaterial, yellowMaterial, greenMaterial;
        public Transform hand;            // where the held flag sits (right hand)
        public float maxPlantReach = 2.0f; // flags are planted at your feet/hand, never far away
        public float pickUpRange = 1.8f;

        readonly List<Flag> _flags = new List<Flag>();
        int _selected = -1;
        AudioSource _audio;

        public IReadOnlyList<Flag> Flags => _flags;
        public Flag Selected => _selected >= 0 ? _flags[_selected] : null;

        void Start()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            _flags.Add(Flag.Create(FlagColour.Blue, poleMaterial, blueMaterial, transform));
            _flags.Add(Flag.Create(FlagColour.Red, poleMaterial, redMaterial, transform));
            _flags.Add(Flag.Create(FlagColour.Yellow, poleMaterial, yellowMaterial, transform));
            _flags.Add(Flag.Create(FlagColour.Green, poleMaterial, greenMaterial, transform));
            foreach (var f in _flags) f.Stow(transform);
            if (hand != null) Select(0, silent: true);
        }

        /// <summary>Move the kit to a new hand anchor (called once the player rig is known).</summary>
        public void SetHand(Transform newHand)
        {
            hand = newHand;
            if (_flags.Count == 0) return; // Start() will pick a flag up
            if (Selected != null && Selected.state == Flag.State.Held) Selected.HoldIn(hand);
            else if (_selected < 0) CycleNext();
        }

        /// <summary>Put the flag at index i in hand. Planted flags stay in the ground and are skipped.</summary>
        public bool Select(int i, bool silent = false)
        {
            if (i < 0 || i >= _flags.Count || _flags[i].state == Flag.State.Planted) return false;
            if (Selected != null && Selected.state == Flag.State.Held) Selected.Stow(transform);
            _selected = i;
            _flags[i].HoldIn(hand);
            if (!silent) _audio.PlayOneShot(Tones.Select, 0.6f);
            return true;
        }

        public void CycleNext(int dir = 1)
        {
            for (int k = 1; k <= _flags.Count; k++)
            {
                int i = ((_selected + dir * k) % _flags.Count + _flags.Count) % _flags.Count;
                if (_flags[i].state != Flag.State.Planted) { Select(i); return; }
            }
        }

        /// <summary>Plant the held flag at a ground point. Returns the flag planted, or null.</summary>
        public Flag PlantSelected(Vector3 groundPoint, Vector3 viewerPosition)
        {
            var f = Selected;
            if (f == null || f.state != Flag.State.Held) return null;
            f.PlantAt(groundPoint, viewerPosition);
            _selected = -1;
            CycleNext(); // next unplanted flag comes to hand, if any
            return f;
        }

        /// <summary>Pull up the nearest planted flag within reach and put it in hand.</summary>
        public Flag PickUpNearest(Vector3 position)
        {
            Flag best = null; float bestD = pickUpRange;
            foreach (var f in _flags)
            {
                if (f.state != Flag.State.Planted) continue;
                float d = FeatureRegistry.HorizontalDistance(f.transform.position, position);
                if (d < bestD) { bestD = d; best = f; }
            }
            if (best == null) return null;
            SessionManager.Instance?.OnFlagPulled(best);
            best.plantedCorrectly = false;
            if (Selected != null && Selected.state == Flag.State.Held) Selected.Stow(transform);
            _selected = _flags.IndexOf(best);
            best.HoldIn(hand);
            _audio.PlayOneShot(Tones.Select, 0.6f);
            return best;
        }

        public bool HasPlantedFlagNear(Vector3 position)
        {
            foreach (var f in _flags)
                if (f.state == Flag.State.Planted && FeatureRegistry.HorizontalDistance(f.transform.position, position) < pickUpRange)
                    return true;
            return false;
        }
    }
}
