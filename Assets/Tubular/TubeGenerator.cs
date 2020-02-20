using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tubular
{
    /// <summary>
    /// TubeGenerator will create tubes following a Transform :)
    /// </summary>
    public class TubeGenerator : MonoBehaviour
    {
        
        TubeConfig Config = new TubeConfig
        {
            VertsPerLoop = 25,
            SegmentLength = 10,
            DistanceBetweenLoops = 0.1f,
        };

        private float Radius { get; set; } = 0.5f;
        private Material Material { get; set; }

        private GameObject CurrentSegment { get; set; }
        private float currentSegmentLength;

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
            Verts.Capacity = Config.VertsPerLoop * (int)(Config.SegmentLength / Config.DistanceBetweenLoops);
            Tris.Capacity = Verts.Capacity * 3;
            UVs.Capacity = Verts.Capacity;

            LoopVerts = CalculateLoopVertices();
        }

        private void Update()
        {
            if (CurrentSegment == null)
                return;

            if (Vector3.Distance(LastLoopPos, PlayerTransform.position) >= Config.DistanceBetweenLoops)
                AddLoop();
            PrevPos = PlayerTransform.position;
        }

        public void StartTube(float radius, Material material)
        {
            if (CurrentSegment != null)
                return;
            Radius = radius;
            Material = material;
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
            TubeFrontSphere = CreateSphere(PlayerTransform.position, Radius, Material);
            TubeFrontSphere.transform.SetParent(PlayerTransform);
            GameObject TubeBackSphere = CreateSphere(PlayerTransform.position, Radius, Material);
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
            Vector4[] loop = new Vector4[Config.VertsPerLoop];
            float radians = (360 / (Config.VertsPerLoop - 1)) * Mathf.Deg2Rad;
            for (int i = 0; i < loop.Length; i++)
                loop[i] = new Vector4(Radius * Mathf.Sin(i * radians), Radius * Mathf.Cos(i * radians), 0, 1);
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
            CurrentSegment.GetComponent<Renderer>().material = Material;
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

            if (currentSegmentLength >= Config.SegmentLength)
            {
                EndTubeSegment();
                StartTubeSegment();
            }                       
        }

        private void UpdateVertsAndUVs()
        {
            Mesh.Clear();
            for (int i = 0; i < Config.VertsPerLoop; i++)
            {
                Verts.Add(LoopOrientMatrix * LoopVerts[i]);
                UVs.Add(new Vector2((float)i / (Config.VertsPerLoop - 1), Mathf.Clamp01(currentSegmentLength / Config.SegmentLength)));
            }
            Mesh.SetVertices(Verts);
            Mesh.SetUVs(0, UVs);
        }

        private void UpdateTriangles()
        {         
            int vertInd = (LoopCount - 1) * Config.VertsPerLoop;
            for (int i = 0; i < Config.VertsPerLoop - 1; i++)
            {
                Tris.Add(vertInd);
                Tris.Add(vertInd + Config.VertsPerLoop);
                Tris.Add(vertInd + Config.VertsPerLoop + 1);
                Tris.Add(vertInd);
                Tris.Add(vertInd + Config.VertsPerLoop + 1);
                Tris.Add(vertInd + 1);
                vertInd++;
            }

            //seam triangles
            Tris.Add(vertInd);
            Tris.Add(vertInd + Config.VertsPerLoop);
            Tris.Add(vertInd + 1);
            Tris.Add(vertInd);
            Tris.Add(vertInd + 1);
            Tris.Add(vertInd - Config.VertsPerLoop + 1);

            Mesh.SetTriangles(Tris, 0);
            Mesh.RecalculateNormals();
        }
        #endregion
    }
}
