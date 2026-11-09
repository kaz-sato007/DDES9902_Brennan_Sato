using UnityEngine;

namespace LandNav
{
    /// <summary>
    /// The paper map the trainee carries: a physical board with the printed map, a small compass
    /// set into the corner, and a status strip underneath. Everything is a 3D object in the world;
    /// nothing is drawn on the screen.
    /// </summary>
    public class MapBoard : MonoBehaviour
    {
        public Material mapMaterial;
        public Material boardMaterial;
        public Material needleMaterial;
        public Material faceMaterial;   // compass face
        public Vector2 mapSize = new Vector2(0.32f, 0.40f); // matches the 2048 x 2560 map image

        /// <summary>Size of the whole board (map plus status strip) at scale 1, metres.</summary>
        public Vector2 BoardSize => new Vector2(mapSize.x + 0.02f, mapSize.y + 0.17f);
        /// <summary>How far the board's centre sits below the map's centre, metres.</summary>
        public float BoardCentreOffset => 0.075f;

        [Header("Current location")]
        [Tooltip("How long the 'you are here' dot stays on the map after each press, seconds")]
        public float locationShowSeconds = 5f;
        [Tooltip("Where the 500 m map frame sits inside the map image, as fractions of the image (from make_map.py)")]
        public Rect mapFrame = new Rect(0.06f, 0.255f, 0.88f, 0.704f);

        [Header("Compass")]
        [Tooltip("How long the needle takes to settle after you turn, seconds (like a liquid-damped compass)")]
        public float needleDamping = 0.25f;

        Transform _needle;
        float _needleAngle, _needleVel;
        WorldUI.Panel _status;

        Transform _locMarker, _locTracked;
        float _locUntil = -1f;
        UnityEngine.UI.Text _locText;
        UnityEngine.UI.Image _locButton;
        string _locKey = "C";
        int _locUses;
        static readonly Color LocIdle = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        static readonly Color LocLit = new Color(0.75f, 0.12f, 0.10f, 0.95f);

        void Start()
        {
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            Destroy(board.GetComponent<Collider>());
            board.transform.SetParent(transform, false);
            board.transform.localScale = new Vector3(mapSize.x + 0.02f, mapSize.y + 0.17f, 0.008f);
            board.transform.localPosition = new Vector3(0, -0.075f, 0.005f);
            board.GetComponent<MeshRenderer>().sharedMaterial = boardMaterial;

            var map = GameObject.CreatePrimitive(PrimitiveType.Quad);
            map.name = "Map";
            Destroy(map.GetComponent<Collider>());
            map.transform.SetParent(transform, false);
            map.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1);
            map.GetComponent<MeshRenderer>().sharedMaterial = mapMaterial;
            map.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // compass: a capsule housing with a needle, top-right corner of the board
            var housing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            housing.name = "CompassHousing";
            Destroy(housing.GetComponent<Collider>());
            housing.transform.SetParent(transform, false);
            housing.transform.localPosition = new Vector3(mapSize.x / 2 - 0.03f, -(mapSize.y / 2 + 0.075f), -0.006f);
            housing.transform.localRotation = Quaternion.Euler(90, 0, 0);
            housing.transform.localScale = new Vector3(0.055f, 0.004f, 0.055f);
            housing.GetComponent<MeshRenderer>().sharedMaterial = faceMaterial != null ? faceMaterial : boardMaterial;

            _needle = new GameObject("Needle").transform;
            _needle.SetParent(transform, false);
            _needle.localPosition = housing.transform.localPosition + new Vector3(0, 0, -0.006f);
            var north = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(north.GetComponent<Collider>());
            north.transform.SetParent(_needle, false);
            north.transform.localScale = new Vector3(0.006f, 0.024f, 0.002f);
            north.transform.localPosition = new Vector3(0, 0.012f, 0);
            north.GetComponent<MeshRenderer>().sharedMaterial = needleMaterial;
            var south = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(south.GetComponent<Collider>());
            south.transform.SetParent(_needle, false);
            south.transform.localScale = new Vector3(0.006f, 0.024f, 0.002f);
            south.transform.localPosition = new Vector3(0, -0.012f, 0);
            south.GetComponent<MeshRenderer>().sharedMaterial = boardMaterial;

            // current location: a red dot with a facing arrow, hidden until the trainee asks for it
            _locMarker = new GameObject("CurrentLocation").transform;
            _locMarker.SetParent(transform, false);
            var ring = MarkerPart(PrimitiveType.Cylinder, faceMaterial != null ? faceMaterial : boardMaterial);
            ring.localRotation = Quaternion.Euler(90, 0, 0);
            ring.localScale = new Vector3(0.011f, 0.0005f, 0.011f);
            var dot = MarkerPart(PrimitiveType.Cylinder, needleMaterial);
            dot.localRotation = Quaternion.Euler(90, 0, 0);
            dot.localPosition = new Vector3(0, 0, -0.0008f);
            dot.localScale = new Vector3(0.007f, 0.0005f, 0.007f);
            var arrow = MarkerPart(PrimitiveType.Cube, needleMaterial);
            arrow.localPosition = new Vector3(0, 0.009f, -0.0008f);
            arrow.localScale = new Vector3(0.0025f, 0.008f, 0.001f);
            _locMarker.gameObject.SetActive(false);

            // the "Current location" button, tucked inside the bottom-right corner of the map grid
            Vector2 btn = new Vector2(0.056f, 0.04f);
            const float inset = 0.004f; // gap from the grid's edge, metres
            _locText = WorldUI.CreateLabel("LocationButton", transform, btn, LocIdle, 8, out _locButton);
            float gridRight = (mapFrame.xMax - 0.5f) * mapSize.x;
            float gridBottom = (mapFrame.yMin - 0.5f) * mapSize.y;
            // move the button's canvas (the text's parent), not the text inside it
            _locText.transform.parent.localPosition = new Vector3(gridRight - btn.x / 2 - inset, gridBottom + btn.y / 2 + inset, -0.004f);
            RefreshLocationButton();

            _status = WorldUI.CreatePanel("Status", transform, new Vector2(mapSize.x - 0.05f, 0.13f), new Color(0.08f, 0.08f, 0.08f, 0.95f), 15, 11);
            _status.root.transform.localPosition = new Vector3(-0.035f, -(mapSize.y / 2 + 0.075f), -0.004f);
        }

        public void SetStatus(string title, string body)
        {
            if (_status == null) return;
            _status.title.text = title;
            _status.body.text = body;
        }

        Transform MarkerPart(PrimitiveType type, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_locMarker, false);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// <summary>The key or button shown on the location button ("C" on screen, "X" in VR).</summary>
        public void SetLocationKey(string key) { _locKey = key; RefreshLocationButton(); }

        public bool LocationVisible => Time.time < _locUntil;

        /// <summary>
        /// Show where 'tracked' is on the map for locationShowSeconds. Returns true if this press opened
        /// the marker (counted as a location check); pressing again while it is showing only restarts the timer.
        /// </summary>
        public bool ShowLocation(Transform tracked)
        {
            bool opened = !LocationVisible;
            _locTracked = tracked;
            _locUntil = Time.time + locationShowSeconds;
            if (opened) _locUses++;
            if (_locMarker != null) _locMarker.gameObject.SetActive(true);
            RefreshLocationButton();
            return opened;
        }

        void RefreshLocationButton()
        {
            if (_locText == null) return;
            _locText.text = $"Current\nlocation [{_locKey}]\n<size=6>used {_locUses}</size>";
            _locButton.color = LocationVisible ? LocLit : LocIdle;
        }

        void UpdateLocationMarker()
        {
            if (_locMarker == null) return;
            float left = _locUntil - Time.time;
            if (left <= 0f || _locTracked == null)
            {
                if (_locMarker.gameObject.activeSelf) { _locMarker.gameObject.SetActive(false); RefreshLocationButton(); }
                return;
            }

            // world position -> fraction across the terrain -> point on the printed map
            Vector3 p = _locTracked.position;
            Vector2 uv;
            var t = Terrain.activeTerrain;
            if (t != null)
            {
                Vector3 tp = t.transform.position, ts = t.terrainData.size;
                uv = new Vector2((p.x - tp.x) / ts.x, (p.z - tp.z) / ts.z);
            }
            else uv = new Vector2(p.x / 500f, p.z / 500f);
            uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y);
            float u = mapFrame.x + mapFrame.width * uv.x;
            float v = mapFrame.y + mapFrame.height * uv.y;
            _locMarker.localPosition = new Vector3((u - 0.5f) * mapSize.x, (v - 0.5f) * mapSize.y, -0.002f);

            // arrow points the way the trainee is facing (the map is printed north-up)
            Vector3 f = _locTracked.forward; f.y = 0f;
            if (f.sqrMagnitude > 1e-4f)
                _locMarker.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg);

            // pop in, gentle pulse so the eye finds it, shrink away over the last 0.4 s
            float shown = locationShowSeconds - left;
            float s = Mathf.Min(Mathf.Clamp01(shown / 0.15f), Mathf.Clamp01(left / 0.4f));
            s *= 1f + 0.12f * Mathf.Sin(Time.time * 6f);
            _locMarker.localScale = Vector3.one * s;
        }

        void LateUpdate()
        {
            UpdateLocationMarker();
            if (_needle == null) return;

            // Heading indicator: work out which way the trainee is facing and point the needle that way
            // on the north-up map, so it always agrees with the "Current location" arrow (facing east,
            // both point right). (The original version measured north in the plane of the board, which
            // fails when the board is held upright to read.)
            // The board's "away from the reader" direction is its up axis when it lies flat and its
            // forward axis when it stands upright; adding the two covers every tilt in between.
            Vector3 facing = transform.up + transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude > 1e-4f)
            {
                float yaw = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg; // 0 = facing north, 90 = east
                // facing east the needle points right (clockwise), the same rule as the location arrow
                _needleAngle = Mathf.SmoothDampAngle(_needleAngle, -yaw, ref _needleVel, needleDamping);
            }
            _needle.localRotation = Quaternion.Euler(0, 0, _needleAngle);
        }
    }
}
