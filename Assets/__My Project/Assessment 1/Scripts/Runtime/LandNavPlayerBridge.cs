using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace LandNav
{
    /// <summary>
    /// Connects the land-navigation kit (map + flags) to whichever EZPZ player is in use.
    /// The EZPZ players handle walking, looking and comfort (snap turn etc.); this script adds
    /// the map, the flag kit and the planting controls on top.
    ///  - Flat screen / WebGL: EZPZ Player Flat Screen WASD
    ///  - Headset: EZPZ Player XR (Dual Remote) — switched on automatically when an XR device is active
    /// </summary>
    public class LandNavPlayerBridge : MonoBehaviour
    {
        [Header("EZPZ players in the scene")]
        public GameObject flatPlayer;
        public GameObject xrPlayer;

        [Header("Kit")]
        public MapBoard map;
        public FlagKit kit;

        [Header("Flat screen placement (relative to the camera)")]
        public Vector3 flatMapLoweredPos = new Vector3(-0.22f, -0.6f, 0.4f);
        public Vector3 flatMapLoweredEuler = new Vector3(70f, 0f, 0f);
        public Vector3 flatMapRaisedPos = new Vector3(0f, 0.05f, 0.56f);
        public Vector3 flatFlagPos = new Vector3(0.4f, -0.95f, 0.95f);
        public Vector3 flatFlagEuler = new Vector3(-5f, 0f, -22f);

        [Header("Map zoom")]
        [Tooltip("Distance of the raised map in front of the eyes (flat screen), metres")]
        public float flatMapDistance = 0.6f;
        [Tooltip("How fast W/A/S/D or the arrow keys slide the raised map (share of the screen per second)")]
        public float mapPanKeySpeed = 0.8f;
        [Tooltip("Share of the screen height the whole map board fills at zoom 1")]
        public float flatMapFit = 0.92f;
        public float maxZoom = 4f;
        [Tooltip("How large the map grows in VR with the left trigger fully squeezed")]
        public float vrMaxZoom = 2.5f;

        [Header("VR placement (relative to the controllers)")]
        public Vector3 vrMapPos = new Vector3(0.0f, 0.06f, 0.12f);
        public Vector3 vrMapEuler = new Vector3(55f, 0f, 0f);
        public Vector3 vrFlagPos = new Vector3(0f, 0f, 0.02f);
        public Vector3 vrFlagEuler = new Vector3(30f, 0f, 0f);

        public bool IsVR { get; private set; }

        Transform _head, _leftHand, _rightHand;
        Transform _mapLowered, _mapRaised;
        bool _mapUp, _wasRaised;
        float _zoom = 1f;
        Vector2 _pan;            // point on the board (metres, unscaled) held at the centre of view
        Camera _cam;
        Behaviour _walker;       // the EZPZ first-person controller (movement and mouse look)
        InputAction _plant, _cycle, _pickUp, _mapZoomVR;

        IEnumerator Start()
        {
            // give XR a moment to initialise before deciding which player to use
            for (float t = 0; t < 1.5f && !XRSettings.isDeviceActive; t += Time.unscaledDeltaTime)
                yield return null;

            IsVR = XRSettings.isDeviceActive && xrPlayer != null;
            if (flatPlayer) flatPlayer.SetActive(!IsVR);
            if (xrPlayer) xrPlayer.SetActive(IsVR);
            yield return null; // let the chosen player initialise

            if (IsVR) SetupVR(); else SetupFlat();
        }

        static Transform FindDeep(Transform root, params string[] names)
        {
            foreach (var n in names)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == n) return t;
            }
            return null;
        }

        static Transform Anchor(string name, Transform parent, Vector3 pos, Vector3 euler)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(euler);
            return t;
        }

        void SetupFlat()
        {
            var cam = flatPlayer.GetComponentInChildren<Camera>(true);
            _head = cam != null ? cam.transform : Camera.main.transform;
            _cam = _head.GetComponent<Camera>();
            foreach (var b in flatPlayer.GetComponentsInChildren<Behaviour>(true))
                if (b.GetType().Name == "FirstPersonController") { _walker = b; break; }
            _mapLowered = Anchor("LandNav_MapLowered", _head, flatMapLoweredPos, flatMapLoweredEuler);
            _mapRaised = Anchor("LandNav_MapRaised", _head, flatMapRaisedPos, Vector3.zero);
            var flagHand = Anchor("LandNav_FlagHand", _head, flatFlagPos, flatFlagEuler);
            map.transform.SetParent(_mapLowered, false);
            kit.SetHand(flagHand);
            if (SessionManager.Instance)
                SessionManager.Instance.controlsHint = "M map (mouse or WASD to slide, scroll or +/- to zoom) | 1-4 or Q/E flag | F plant | G pull up";
        }

        void SetupVR()
        {
            var cam = xrPlayer.GetComponentInChildren<Camera>(true);
            _head = cam != null ? cam.transform : Camera.main.transform;
            _leftHand = FindDeep(xrPlayer.transform, "Left Controller", "LeftHand Controller", "Left Hand");
            _rightHand = FindDeep(xrPlayer.transform, "Right Controller", "RightHand Controller", "Right Hand");
            if (_leftHand == null || _rightHand == null)
            {
                Debug.LogWarning("[LandNav] Could not find XR controllers; falling back to head-attached kit.");
                _leftHand = _rightHand = _head;
            }
            var mapHold = Anchor("LandNav_MapHold", _leftHand, vrMapPos, vrMapEuler);
            var flagHold = Anchor("LandNav_FlagHold", _rightHand, vrFlagPos, vrFlagEuler);
            map.transform.SetParent(mapHold, false);
            map.transform.localPosition = Vector3.zero;
            map.transform.localRotation = Quaternion.identity;
            kit.SetHand(flagHold);

            _plant = Bind("<XRController>{RightHand}/{TriggerButton}");
            _cycle = Bind("<XRController>{RightHand}/{PrimaryButton}");
            _pickUp = Bind("<XRController>{RightHand}/{SecondaryButton}");
            _mapZoomVR = Bind("<XRController>{LeftHand}/{Trigger}");
            if (SessionManager.Instance)
                SessionManager.Instance.controlsHint = "Left trigger enlarge map | A switch flag | Right trigger plant | B pull up";
        }

        static InputAction Bind(string path)
        {
            var a = new InputAction(binding: path);
            a.Enable();
            return a;
        }

        void Update()
        {
            if (_head == null) return;
            if (IsVR) UpdateVR(); else UpdateFlat();
        }

        Vector3 BodyPosition => Ground(_head.position);

        void UpdateFlat()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            if (kb.mKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) _mapUp = !_mapUp;
            bool raised = _mapUp || (mouse != null && mouse.rightButton.isPressed);
            UpdateFlatMap(raised, kb, mouse);

            if (kb.digit1Key.wasPressedThisFrame) kit.Select(0);
            if (kb.digit2Key.wasPressedThisFrame) kit.Select(1);
            if (kb.digit3Key.wasPressedThisFrame) kit.Select(2);
            if (kb.digit4Key.wasPressedThisFrame) kit.Select(3);
            if (kb.eKey.wasPressedThisFrame) kit.CycleNext(1);
            if (kb.qKey.wasPressedThisFrame) kit.CycleNext(-1);

            if (kb.fKey.wasPressedThisFrame)
            {
                Vector3 fwd = _head.forward; fwd.y = 0; fwd.Normalize();
                kit.PlantSelected(Ground(_head.position + fwd * 0.9f), _head.position);
            }
            if (kb.gKey.wasPressedThisFrame) kit.PickUpNearest(BodyPosition);
#if UNITY_EDITOR
            // Editor-only testing shortcut: F5..F9 jump to each feature (compiled out of builds)
            if (FeatureRegistry.Instance != null)
            {
                var keys = new[] { kb.f5Key, kb.f6Key, kb.f7Key, kb.f8Key, kb.f9Key };
                for (int i = 0; i < keys.Length && i < FeatureRegistry.Instance.features.Count; i++)
                    if (keys[i].wasPressedThisFrame) TeleportFlat(FeatureRegistry.Instance.features[i].position);
            }
#endif
        }

#if UNITY_EDITOR
        public void TeleportFlat(Vector3 p)
        {
            var cc = flatPlayer.GetComponentInChildren<CharacterController>();
            var body = cc != null ? cc.transform : flatPlayer.transform;
            if (cc) cc.enabled = false;
            body.position = Ground(p) + Vector3.up * 0.1f;
            if (cc) cc.enabled = true;
        }
#endif

        /// <summary>
        /// Raised map: the player halts (as when reading a real map), the camera stays completely
        /// still, and the map is slid under the view instead: mouse or W/A/S/D/arrow keys move it,
        /// scroll or +/- zooms from the whole sheet up to maxZoom. Lowered map: back at the hip,
        /// normal size, and walking and looking are restored.
        /// </summary>
        void UpdateFlatMap(bool raised, Keyboard kb, Mouse mouse)
        {
            float k = 1f - Mathf.Exp(-14f * Time.deltaTime); // smooth, frame-rate independent
            var mt = map.transform;

            if (!raised)
            {
                if (_wasRaised && _walker != null) _walker.enabled = true; // walk and look again
                _wasRaised = false;
                _zoom = 1f;
                _pan = Vector2.zero;
                if (mt.parent != _mapLowered) mt.SetParent(_mapLowered, true);
                mt.localPosition = Vector3.Lerp(mt.localPosition, Vector3.zero, k);
                mt.localRotation = Quaternion.Slerp(mt.localRotation, Quaternion.identity, k);
                mt.localScale = Vector3.Lerp(mt.localScale, Vector3.one, k);
                return;
            }

            if (!_wasRaised)
            {
                _wasRaised = true;
                if (_walker != null) _walker.enabled = false; // halt: no walking, no camera movement
                _pan = new Vector2(0f, -map.BoardCentreOffset); // start centred on the whole board
                mt.SetParent(_head, true);
            }

            // zoom: mouse wheel (any platform scale) or +/- keys
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (scroll > 0.01f) _zoom *= 1.15f;
            else if (scroll < -0.01f) _zoom /= 1.15f;
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) _zoom *= 1.25f;
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) _zoom /= 1.25f;
            _zoom = Mathf.Clamp(_zoom, 1f, maxZoom);

            // size so the whole board fits the screen at zoom 1
            float fov = _cam != null ? _cam.fieldOfView : 60f;
            float aspect = _cam != null ? _cam.aspect : 16f / 9f;
            float visH = 2f * flatMapDistance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float visW = visH * aspect;
            float fit = Mathf.Min(flatMapFit * visH / map.BoardSize.y, flatMapFit * visW / map.BoardSize.x);
            float scale = fit * _zoom;

            // slide: mouse moves the map 1:1 on screen; keys at a steady speed
            Vector2 slide = Vector2.zero;
            if (mouse != null) slide += mouse.delta.ReadValue() * (visH / Mathf.Max(1, Screen.height));
            Vector2 keys = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) keys.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) keys.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keys.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keys.x -= 1;
            slide += keys * (mapPanKeySpeed * visH * Time.deltaTime);
            _pan += slide / scale; // moving "up" brings the upper part of the map into view

            // keep the board on screen: no sliding past its edges, and centred when it already fits
            Vector2 half = map.BoardSize * 0.5f;
            Vector2 view = new Vector2(visW, visH) * (0.5f / scale);
            float boardMidY = -map.BoardCentreOffset;
            float limX = Mathf.Max(0f, half.x - view.x);
            float limY = Mathf.Max(0f, half.y - view.y);
            _pan.x = Mathf.Clamp(_pan.x, -limX, limX);
            _pan.y = Mathf.Clamp(_pan.y, boardMidY - limY, boardMidY + limY);

            // the camera never moves; the map moves so the chosen point sits in the centre of view
            Vector3 target = new Vector3(-_pan.x * scale, -_pan.y * scale, flatMapDistance);
            mt.localPosition = Vector3.Lerp(mt.localPosition, target, k);
            mt.localRotation = Quaternion.Slerp(mt.localRotation, Quaternion.identity, k);
            mt.localScale = Vector3.Lerp(mt.localScale, Vector3.one * scale, k);
        }

        void UpdateVR()
        {
            // squeeze the left trigger to bring the map up large; release to return it to hand size
            float squeeze = _mapZoomVR != null ? _mapZoomVR.ReadValue<float>() : 0f;
            float vrScale = Mathf.Lerp(1f, vrMaxZoom, Mathf.Clamp01(squeeze));
            map.transform.localScale = Vector3.Lerp(map.transform.localScale, Vector3.one * vrScale, 1f - Mathf.Exp(-14f * Time.deltaTime));

            if (_cycle.WasPressedThisFrame()) { kit.CycleNext(1); Haptic(XRNode.RightHand, 0.2f, 0.05f); }
            if (_plant.WasPressedThisFrame())
            {
                // plant beneath the right hand, clamped to arm's reach of the body
                Vector3 p = _rightHand.position;
                Vector3 off = p - _head.position; off.y = 0;
                if (off.magnitude > kit.maxPlantReach) p = _head.position + off.normalized * kit.maxPlantReach;
                if (kit.PlantSelected(Ground(p), _head.position) != null) Haptic(XRNode.RightHand, 0.6f, 0.15f);
            }
            if (_pickUp.WasPressedThisFrame() && kit.PickUpNearest(_rightHand.position) != null)
                Haptic(XRNode.RightHand, 0.4f, 0.1f);
        }

        static void Haptic(XRNode node, float amp, float dur)
        {
            var dev = InputDevices.GetDeviceAtXRNode(node);
            if (dev.isValid) dev.SendHapticImpulse(0u, amp, dur);
        }

        static Vector3 Ground(Vector3 p)
        {
            var t = Terrain.activeTerrain;
            if (t != null) p.y = t.SampleHeight(p) + t.transform.position.y;
            return p;
        }

        void OnDestroy()
        {
            _plant?.Dispose(); _cycle?.Dispose(); _pickUp?.Dispose(); _mapZoomVR?.Dispose();
        }
    }
}
