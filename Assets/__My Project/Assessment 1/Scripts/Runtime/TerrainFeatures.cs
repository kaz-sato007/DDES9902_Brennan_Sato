using System;
using System.Collections.Generic;
using UnityEngine;

namespace LandNav
{
    public enum FlagColour { None = 0, Blue = 1, Red = 2, Yellow = 3, Green = 4 }

    /// <summary>A named landform with a circular acceptance zone measured on the ground plane.</summary>
    [Serializable]
    public class TerrainFeature
    {
        public string id;
        public string displayName;
        public Vector3 position;      // world position of the feature's defining point
        public float radius = 16f;    // metres; a flag counts as "on" the feature inside this
        public FlagColour flag;       // which flag belongs here (None = distractor feature)
        [TextArea(2, 5)] public string teaching;
    }
}
