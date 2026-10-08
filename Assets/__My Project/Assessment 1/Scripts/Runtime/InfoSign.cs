using UnityEngine;

namespace LandNav
{
    /// <summary>A post-mounted information board in the world (used at the start point).</summary>
    public class InfoSign : MonoBehaviour
    {
        public string title = "";
        [TextArea(4, 12)] public string body = "";
        public Vector2 size = new Vector2(1.4f, 1.0f);
        public float height = 1.55f;
        public Material postMaterial;

        void Start()
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            post.transform.SetParent(transform, false);
            post.transform.localScale = new Vector3(0.08f, height, 0.08f);
            post.transform.localPosition = new Vector3(0, height / 2f - 0.2f, 0.03f);
            if (postMaterial) post.GetComponent<MeshRenderer>().sharedMaterial = postMaterial;

            var p = WorldUI.CreatePanel("Board", transform, size, new Color(0.06f, 0.1f, 0.08f, 0.95f), 70, 42);
            p.root.transform.localPosition = new Vector3(0, height + size.y / 2f - 0.3f, 0);
            p.title.text = title;
            p.body.text = body;
        }
    }
}
