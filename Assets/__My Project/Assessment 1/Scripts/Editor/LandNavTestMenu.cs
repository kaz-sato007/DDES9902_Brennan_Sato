using UnityEditor;
using UnityEngine;

namespace LandNav.EditorTools
{
    /// <summary>Play-mode test helpers for checking the feedback logic without walking the course.</summary>
    public static class LandNavTestMenu
    {
        static bool Ready(out FlagKit kit, out LandNavPlayerBridge bridge)
        {
            kit = Object.FindFirstObjectByType<FlagKit>();
            bridge = Object.FindFirstObjectByType<LandNavPlayerBridge>();
            return Application.isPlaying && kit != null && bridge != null && FeatureRegistry.Instance != null;
        }

        static void PlantAt(int flagIndex, int featureIndex)
        {
            if (!Ready(out var kit, out var bridge)) { Debug.LogWarning("[LandNav] Enter Play mode first."); return; }
            var f = FeatureRegistry.Instance.features[featureIndex];
            var viewer = f.position + new Vector3(0, 1.7f, -3f);
            bridge.TeleportFlat(f.position + new Vector3(0, 0, -3f));
            if (kit.Flags[flagIndex].state == Flag.State.Planted) kit.PickUpNearest(kit.Flags[flagIndex].transform.position);
            kit.Select(flagIndex, true);
            var planted = kit.PlantSelected(f.position, viewer);
            Debug.Log($"[LandNav TEST] {planted?.colour} on {f.displayName}: correct={planted?.plantedCorrectly}, on={planted?.lastFeature?.displayName ?? "none"}");
        }

        [MenuItem("Land Nav/Test (Play mode)/Plant Blue on Valley (wrong)")] static void T1() => PlantAt(0, 3);
        [MenuItem("Land Nav/Test (Play mode)/Plant Green on Valley (right)")] static void T2() => PlantAt(3, 3);
        [MenuItem("Land Nav/Test (Play mode)/Plant Yellow on East Knoll (wrong)")] static void T3() => PlantAt(2, 4);
        [MenuItem("Land Nav/Test (Play mode)/Plant all four correctly")]
        static void T4() { PlantAt(0, 0); PlantAt(1, 1); PlantAt(2, 2); PlantAt(3, 3); }
    }
}
