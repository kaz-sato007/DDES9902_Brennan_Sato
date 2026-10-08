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

        Transform _needle;
        WorldUI.Panel _status;

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

            _status = WorldUI.CreatePanel("Status", transform, new Vector2(mapSize.x - 0.05f, 0.13f), new Color(0.08f, 0.08f, 0.08f, 0.95f), 15, 11);
            _status.root.transform.localPosition = new Vector3(-0.035f, -(mapSize.y / 2 + 0.075f), -0.004f);
        }

        public void SetStatus(string title, string body)
        {
            if (_status == null) return;
            _status.title.text = title;
            _status.body.text = body;
        }

        void LateUpdate()
        {
            if (_needle == null) return;
            // point the needle at world north (+z), measured in the plane of the board
            Vector3 n = transform.InverseTransformDirection(Vector3.forward);
            float angle = Mathf.Atan2(-n.x, n.y) * Mathf.Rad2Deg;
            _needle.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
