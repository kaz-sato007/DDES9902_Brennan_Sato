using System.Collections.Generic;
using UnityEngine;

namespace LandNav
{
    /// <summary>Holds the scene's terrain features and answers "what feature is this spot on?"</summary>
    public class FeatureRegistry : MonoBehaviour
    {
        public List<TerrainFeature> features = new List<TerrainFeature>();

        public static FeatureRegistry Instance { get; private set; }

        void Awake() { Instance = this; }

        public TerrainFeature TargetFor(FlagColour colour)
        {
            foreach (var f in features)
                if (f.flag == colour) return f;
            return null;
        }

        /// <summary>Feature whose zone contains the point (closest relative to its radius), or null.</summary>
        public TerrainFeature Classify(Vector3 point)
        {
            TerrainFeature best = null;
            float bestScore = 1f;
            foreach (var f in features)
            {
                float score = HorizontalDistance(point, f.position) / f.radius;
                if (score <= bestScore) { bestScore = score; best = f; }
            }
            return best;
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0; b.y = 0;
            return Vector3.Distance(a, b);
        }

        /// <summary>Eight-point compass direction from a to b (world +z is north).</summary>
        public static string CompassDirection(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float bearing = (Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg + 360f) % 360f;
            string[] names = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
            return names[Mathf.RoundToInt(bearing / 45f) % 8];
        }

        void OnDrawGizmos()
        {
            foreach (var f in features)
            {
                Gizmos.color = f.flag switch
                {
                    FlagColour.Blue => Color.blue,
                    FlagColour.Red => Color.red,
                    FlagColour.Yellow => Color.yellow,
                    FlagColour.Green => Color.green,
                    _ => Color.grey
                };
                Gizmos.DrawWireSphere(f.position, f.radius);
            }
        }
    }
}
