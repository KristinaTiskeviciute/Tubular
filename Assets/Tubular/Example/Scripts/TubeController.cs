using UnityEngine;
using Tubular;

namespace Example
{

    [System.Serializable]
    struct TubeInputControls
    {

        public KeyCode StartTubeKey;
        public KeyCode CloseTubeKey;
        public KeyCode ClearAllTubesKey;
    }

    [RequireComponent(typeof(TubeGenerator))]
    public class TubeController : MonoBehaviour
    {
        [SerializeField]
        private float tubeRadius = 0.5f;

        [SerializeField]
        private Material tubeMaterial = null;

        [SerializeField]
        private TubeInputControls Controls = new TubeInputControls
        {
            StartTubeKey = KeyCode.RightAlt,
            CloseTubeKey = KeyCode.RightControl,
            ClearAllTubesKey = KeyCode.Delete
        };

        private TubeGenerator MyTubeGenerator { get; set; }

        private void Awake()
        {
            if (MyTubeGenerator == null)
            {
                MyTubeGenerator = GetComponent<TubeGenerator>();
            }
        }

        private void Start()
        {
            MyTubeGenerator.StartTube(tubeRadius, tubeMaterial);
        }

        void Update()
        {
            if (Input.GetKey(Controls.CloseTubeKey))
            {
                MyTubeGenerator.CloseTube();
            }

            if (Input.GetKey(Controls.StartTubeKey))
            {
                MyTubeGenerator.StartTube(tubeRadius, tubeMaterial);
            }

            if (Input.GetKey(Controls.ClearAllTubesKey))
            {
                MyTubeGenerator.ClearTubes();
            }
        }
    }
}

