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

        [Header("VR placement (relative to the controllers)")]
        public Vector3 vrMapPos = new Vector3(0.0f, 0.06f, 0.12f);
        public Vector3 vrMapEuler = new Vector3(55f, 0f, 0f);
        public Vector3 vrFlagPos = new Vector3(0f, 0f, 0.02f);
        public Vector3 vrFlagEuler = new Vector3(30f, 0f, 0f);

        public bool IsVR { get; private set; }

        Transform _head, _leftHand, _rightHand;
        Transform _mapLowered, _mapRaised;
        bool _mapUp;
        InputAction _plant, _cycle, _pickUp;

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
            _mapLowered = Anchor("LandNav_MapLowered", _head, flatMapLoweredPos, flatMapLoweredEuler);
            _mapRaised = Anchor("LandNav_MapRaised", _head, flatMapRaisedPos, Vector3.zero);
            var flagHand = Anchor("LandNav_FlagHand", _head, flatFlagPos, flatFlagEuler);
            map.transform.SetParent(_mapLowered, false);
            kit.SetHand(flagHand);
            if (SessionManager.Instance)
                SessionManager.Instance.controlsHint = "M map | 1-4 or Q/E choose flag | F plant | G pull up";
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
            if (SessionManager.Instance)
                SessionManager.Instance.controlsHint = "A switch flag | Trigger plant at your hand | B pull up a flag";
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
            var target = raised ? _mapRaised : _mapLowered;
            if (map.transform.parent != target) map.transform.SetParent(target, true);
            map.transform.localPosition = Vector3.Lerp(map.transform.localPosition, Vector3.zero, 12f * Time.deltaTime);
            map.transform.localRotation = Quaternion.Slerp(map.transform.localRotation, Quaternion.identity, 12f * Time.deltaTime);

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

        void UpdateVR()
        {
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
            _plant?.Dispose(); _cycle?.Dispose(); _pickUp?.Dispose();
        }
    }
}
