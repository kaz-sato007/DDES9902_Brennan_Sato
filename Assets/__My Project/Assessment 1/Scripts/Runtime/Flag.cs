using System.Collections;
using UnityEngine;

namespace LandNav
{
    /// <summary>
    /// One coloured flag picket. It can be carried (parented to a hand), planted in the ground,
    /// and gives diegetic feedback on a small sign attached to its pole after planting.
    /// </summary>
    public class Flag : MonoBehaviour
    {
        public enum State { Stowed, Held, Planted }

        public FlagColour colour;
        public State state = State.Stowed;
        public int attempts;
        public bool plantedCorrectly;
        public TerrainFeature lastFeature;

        const float PoleLength = 1.5f;
        const float GripHeight = 0.55f; // where the hand holds the pole, from the bottom

        Transform _visual;
        Transform _cloth;
        WorldUI.Panel _sign;
        AudioSource _audio;
        Coroutine _plantRoutine;
        bool _showSign;

        public static Flag Create(FlagColour colour, Material pole, Material cloth, Transform parent)
        {
            var root = new GameObject($"Flag_{colour}");
            root.transform.SetParent(parent, false);
            var flag = root.AddComponent<Flag>();
            flag.colour = colour;
            flag.Build(pole, cloth);
            return flag;
        }

        void Build(Material poleMat, Material clothMat)
        {
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            var pole = Prim(PrimitiveType.Cylinder, "Pole", _visual, poleMat);
            pole.localScale = new Vector3(0.022f, PoleLength / 2f, 0.022f);
            pole.localPosition = new Vector3(0, PoleLength / 2f, 0);

            var tip = Prim(PrimitiveType.Sphere, "Tip", _visual, clothMat);
            tip.localScale = Vector3.one * 0.05f;
            tip.localPosition = new Vector3(0, PoleLength + 0.02f, 0);

            _cloth = new GameObject("ClothPivot").transform;
            _cloth.SetParent(_visual, false);
            _cloth.localPosition = new Vector3(0, PoleLength - 0.16f, 0);
            var cloth = Prim(PrimitiveType.Cube, "Cloth", _cloth, clothMat);
            cloth.localScale = new Vector3(0.42f, 0.28f, 0.01f);
            cloth.localPosition = new Vector3(0.22f, 0, 0);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0.6f;
            _audio.minDistance = 2f;

            _sign = WorldUI.CreatePanel("FeedbackSign", transform, new Vector2(0.85f, 0.5f), new Color(0, 0, 0, 0.78f), 44, 27);
            _sign.root.transform.localPosition = new Vector3(0, 1.95f, 0);
            _sign.root.SetActive(false);
        }

        static Transform Prim(PrimitiveType type, string name, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>()); // flags never block the walker
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go.transform;
        }

        /// <summary>Attach to a hand anchor so the pole sits in the grip.</summary>
        public void HoldIn(Transform hand)
        {
            if (_plantRoutine != null) StopCoroutine(_plantRoutine);
            state = State.Held;
            gameObject.SetActive(true);
            transform.SetParent(hand, false);
            transform.localPosition = new Vector3(0, -GripHeight, 0);
            transform.localRotation = Quaternion.identity;
            _visual.localPosition = Vector3.zero;
            _showSign = false;
            _sign.root.SetActive(false);
        }

        public void Stow(Transform kit)
        {
            state = State.Stowed;
            transform.SetParent(kit, false);
            gameObject.SetActive(false);
        }

        /// <summary>Plant at a ground point and evaluate against the registry.</summary>
        public void PlantAt(Vector3 groundPoint, Vector3 viewer)
        {
            state = State.Planted;
            transform.SetParent(null, true);
            Vector3 toViewer = viewer - groundPoint; toViewer.y = 0;
            transform.SetPositionAndRotation(groundPoint, toViewer.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(-toViewer.normalized) // cloth faces the planter
                : Quaternion.identity);
            if (_plantRoutine != null) StopCoroutine(_plantRoutine);
            _plantRoutine = StartCoroutine(DriveIn());

            attempts++;
            Evaluate(groundPoint);
        }

        IEnumerator DriveIn()
        {
            _audio.PlayOneShot(Tones.Plant);
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                _visual.localPosition = Vector3.Lerp(new Vector3(0, 0.35f, 0), new Vector3(0, -0.2f, 0), t / 0.25f);
                yield return null;
            }
            _visual.localPosition = new Vector3(0, -0.2f, 0); // 20 cm into the ground
        }

        void Evaluate(Vector3 point)
        {
            var reg = FeatureRegistry.Instance;
            var target = reg.TargetFor(colour);
            var on = reg.Classify(point);
            lastFeature = on;
            plantedCorrectly = target != null && on == target;

            string col = $"<color={WorldUI.Hex(colour)}>{colour.ToString().ToUpper()}</color>";
            if (plantedCorrectly)
            {
                _sign.background.color = new Color(0.05f, 0.32f, 0.12f, 0.88f);
                _sign.title.text = $"CORRECT: {target.displayName.ToUpper()}";
                _sign.body.text = $"{col} flag planted on the {target.displayName.ToLower()}.\n\n{target.teaching}";
                _audio.PlayOneShot(Tones.Correct);
            }
            else
            {
                _sign.background.color = new Color(0.45f, 0.22f, 0.05f, 0.88f);
                _sign.title.text = "NOT QUITE";
                string where = on != null
                    ? $"You are on the <b>{on.displayName.ToLower()}</b>. {on.teaching}"
                    : "This spot is not on any of the marked features. You are on open slope between them.";
                string hint = "Compare the contours on your map with the ground around you, then try again.";
                if (attempts >= 2 && target != null)
                {
                    float d = FeatureRegistry.HorizontalDistance(point, target.position);
                    hint = $"Hint: the {col} flag's feature is about {Mathf.Max(10, Mathf.Round(d / 10f) * 10f):0} m to the {FeatureRegistry.CompassDirection(point, target.position)}.";
                }
                _sign.title.text = "NOT QUITE";
                _sign.body.text = $"{where}\n\n{hint}";
                _audio.PlayOneShot(Tones.Incorrect);
            }
            _showSign = true;
            SessionManager.Instance?.OnFlagPlanted(this);
        }

        void LateUpdate()
        {
            // keep the sign turned towards the viewer (yaw only, so it never tilts)
            if (_sign != null && Camera.main != null)
            {
                Vector3 d = _sign.root.transform.position - Camera.main.transform.position;
                d.y = 0;
                // only show the sign to someone nearby, so it does not give answers away from a distance
                bool show = _showSign && state == State.Planted && d.magnitude < 40f;
                if (_sign.root.activeSelf != show) _sign.root.SetActive(show);
                if (show && d.sqrMagnitude > 0.001f) _sign.root.transform.rotation = Quaternion.LookRotation(d);
            }
            // gentle flutter
            if (_cloth != null)
                _cloth.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 2.3f + (int)colour) * 12f, 0);
        }
    }
}
