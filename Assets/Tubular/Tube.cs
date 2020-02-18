using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tubular
{
    /// <summary>
    /// Tube will create a Tube :)
    /// </summary>
    public class Tube : MonoBehaviour
    {

        const int VERTS_PER_LOOP = 25;

        [SerializeField]
        private float segmentLength = 10;
        private GameObject CurrentSegment { get; set; }
        private float currentSegmentLength;

        [SerializeField]
        [Range(0.1f, 2.0f)]
        private float distanceBetweenLoops = 0.1f;

        [SerializeField]
        private float tubeRadius = 0.5f;

        [SerializeField]
        private Material material;

        private GameObject TubeFrontSphere { get; set; }       
        
        private GameObject CurrentTubeParent;

        private List<GameObject> Tubes { get; set; } = new List<GameObject>();
        public ReadOnlyCollection<GameObject> ReadOnlyTubes => Tubes.AsReadOnly(); 
        
        private Mesh Mesh { get; set; }
        private Vector4[] LoopVerts { get; set; }

        private List<Vector3> Verts { get; set; } = new List<Vector3>();
        private List<int> Tris { get; set; } = new List<int>();
        private List<Vector2> UVs { get; set; } = new List<Vector2>();

        private int LoopCount { get; set; }
        private Vector3 LastLoopPos { get; set; }
        private Vector3 PrevPos { get; set; }
        private Vector3 PrevVelocity { get; set; }

        
        private Vector3 VelocityVector => PrevPos == PlayerTransform.position ? PlayerTransform.forward : (PlayerTransform.position - PrevPos).normalized;
        private Vector3 UpVector => Vector3.Cross(VelocityVector, transform.right);
        private Matrix4x4 LoopOrientMatrix => Matrix4x4.TRS(PrevPos, Quaternion.LookRotation(VelocityVector, -UpVector), Vector3.one);

        private Transform playerTransform;
        private Transform PlayerTransform => (playerTransform == null) ? playerTransform = transform : playerTransform;
        

        private void Awake()
        {
            Verts.Capacity = VERTS_PER_LOOP * (int)(segmentLength / distanceBetweenLoops);
            Tris.Capacity = Verts.Capacity * 3;
            UVs.Capacity = Verts.Capacity;

            LoopVerts = CalculateLoopVertices();
        }

        private void Update()
        {
            if (CurrentSegment == null)
                return;

            if (Vector3.Distance(LastLoopPos, PlayerTransform.position) >= distanceBetweenLoops)
                AddLoop();
            PrevPos = PlayerTransform.position;
        }

        public void StartTube()
        {
            if (CurrentSegment != null)
                return;

            CurrentTubeParent = new GameObject();
            PrevPos = PlayerTransform.position;
            CreateEndCapSpheres(CurrentTubeParent);

            
            LastLoopPos = PlayerTransform.position;

            
            StartTubeSegment();
        }

        public void CloseTube()
        {
            if (CurrentSegment == null)
                return;

            AddLoop();
            EndTubeSegment();
            TubeFrontSphere.transform.SetParent(CurrentTubeParent.transform);
            Tubes.Add(CurrentTubeParent);
            CurrentTubeParent = null;
        }

        public void ClearTubes()
        {
            if (CurrentSegment != null)
                CloseTube();

            for (int i = 0; i < Tubes.Count; i++)
                Destroy(Tubes[i]);
        }

        public void ClearTubeAtIndex(int index)
        {
            if(index == Tubes.Count)
                CloseTube( );

            Destroy(Tubes[index]);
            Tubes.RemoveAt(index);
        }

        #region PRIVATES


        private void CreateEndCapSpheres(GameObject currentTubeParent)
        {
            TubeFrontSphere = CreateSphere(PlayerTransform.position, tubeRadius, material);
            TubeFrontSphere.transform.SetParent(PlayerTransform);
            GameObject TubeBackSphere = CreateSphere(PlayerTransform.position, tubeRadius, material);
            TubeBackSphere.transform.SetParent(currentTubeParent.transform);
        }

        private GameObject CreateSphere(Vector3 position, float radius, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
            sphere.transform.position = position;
            sphere.GetComponent<Renderer>().material = material;
            Destroy(sphere.GetComponent<SphereCollider>());
            return sphere;
        }

        private Vector4[] CalculateLoopVertices()
        {
            Vector4[] loop = new Vector4[VERTS_PER_LOOP];
            float radians = (360 / (VERTS_PER_LOOP - 1)) * Mathf.Deg2Rad;
            for (int i = 0; i < loop.Length; i++)
                loop[i] = new Vector4(tubeRadius * Mathf.Sin(i * radians), tubeRadius * Mathf.Cos(i * radians), 0, 1);
            return loop;
        }

        private void StartTubeSegment()
        {
            currentSegmentLength = 0;
            Verts.Clear();
            Tris.Clear();
            UVs.Clear();
          
            CurrentSegment = new GameObject();
            CurrentSegment.AddComponent<MeshFilter>();
            CurrentSegment.AddComponent<MeshRenderer>();

            Mesh = CurrentSegment.GetComponent<MeshFilter>().mesh;
            Mesh.MarkDynamic();
            CurrentSegment.GetComponent<Renderer>().material = material;
            Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            UpdateVertsAndUVs();
            Mesh.RecalculateNormals();
            LoopCount = 1;
        }

        private void EndTubeSegment()
        {
            if (CurrentSegment == null)
                return;
            Mesh.Optimize();
            CurrentSegment.transform.SetParent(CurrentTubeParent.transform);
            CurrentSegment = null;
        }

        private void AddLoop()
        {
            currentSegmentLength += (PlayerTransform.position - LastLoopPos).magnitude;
            LastLoopPos = PlayerTransform.position;

            UpdateVertsAndUVs();
            UpdateTriangles();
            LoopCount++;
            PrevVelocity = VelocityVector;

            if (currentSegmentLength >= segmentLength)
            {
                EndTubeSegment();
                StartTubeSegment();
            }                       
        }

        private void UpdateVertsAndUVs()
        {
            Mesh.Clear();
            for (int i = 0; i < VERTS_PER_LOOP; i++)
            {
                Verts.Add(LoopOrientMatrix * LoopVerts[i]);
                UVs.Add(new Vector2((float)i / (VERTS_PER_LOOP - 1), Mathf.Clamp01(currentSegmentLength / segmentLength)));
            }
            Mesh.SetVertices(Verts);
            Mesh.SetUVs(0, UVs);
        }

        private void UpdateTriangles()
        {         
            int vertInd = (LoopCount - 1) * VERTS_PER_LOOP;
            for (int i = 0; i < VERTS_PER_LOOP - 1; i++)
            {
                Tris.Add(vertInd);
                Tris.Add(vertInd + VERTS_PER_LOOP);
                Tris.Add(vertInd + VERTS_PER_LOOP + 1);
                Tris.Add(vertInd);
                Tris.Add(vertInd + VERTS_PER_LOOP + 1);
                Tris.Add(vertInd + 1);
                vertInd++;
            }

            //seam triangles
            Tris.Add(vertInd);
            Tris.Add(vertInd + VERTS_PER_LOOP);
            Tris.Add(vertInd + 1);
            Tris.Add(vertInd);
            Tris.Add(vertInd + 1);
            Tris.Add(vertInd - VERTS_PER_LOOP + 1);

            Mesh.SetTriangles(Tris, 0);
            Mesh.RecalculateNormals();
        }
        #endregion
    }
}
