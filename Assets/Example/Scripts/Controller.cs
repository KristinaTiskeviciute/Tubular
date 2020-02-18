using System.Collections;
using UnityEngine;
using Tubular;

namespace Example
{
    [System.Serializable]
    struct InputControls
    {
        public KeyCode LeftKey;
        public KeyCode RightKey;
        public KeyCode ForwardKey;
        public KeyCode JumpKey;
        public KeyCode StartTubeKey;
        public KeyCode CloseTubeKey;
        public KeyCode ClearAllTubesKey;
    }

    [RequireComponent(typeof(Tube))]
    public class Controller : MonoBehaviour
    {
        [SerializeField]
        [Range(3f, 10.0f)]
        private float speed = 3f;

        [SerializeField]
        [Range(1f, 10.0f)]
        private float turnSpeed = 1f;

        [SerializeField]
        private AnimationCurve jumpCurve;

        [SerializeField]
        private float jumpHeight = 3;

        [SerializeField]
        private float jumpDuration = 1.0f;

        [SerializeField]
        private float jumpForwardSpeedModifier = 2.0f;

        [SerializeField]
        private InputControls Controls = new InputControls
        {
            LeftKey = KeyCode.LeftArrow,
            RightKey = KeyCode.RightArrow,
            ForwardKey = KeyCode.UpArrow,
            JumpKey = KeyCode.Space,
            StartTubeKey = KeyCode.RightAlt,
            CloseTubeKey = KeyCode.RightControl,
            ClearAllTubesKey = KeyCode.Delete
        };

        private float StartY { get; set; } = 0f;

        private Tube MyTube { get; set; }

        private bool OnGround { get; set; } = true;
        private int VerticalMovementDirection { get; set; } = 0;   //-1: Going Down, 1: Going Up 0: Not Moving Vertically


        private void Awake()
        {
            if (MyTube == null)
            {
                MyTube = GetComponent<Tube>();
            }
        }

        private void Start()
        {
            StartY = transform.position.y;
            MyTube.StartTube();


        }

        void Update()
        {
            if (Input.GetKey(Controls.ForwardKey))
            {
                transform.position = transform.position + (transform.forward * speed * Time.smoothDeltaTime);
            }

            if (Input.GetKey(Controls.LeftKey))
            {
                transform.Rotate(new Vector3(0, -turnSpeed, 0), Space.Self);
            }

            if (Input.GetKey(Controls.RightKey))
            {
                transform.Rotate(new Vector3(0, turnSpeed, 0), Space.Self);
            }

            if (OnGround && (Input.GetKey(Controls.RightKey) || Input.GetKey(Controls.LeftKey)) && !Input.GetKey(Controls.ForwardKey))
            {
                transform.position = transform.position + (transform.forward * speed * Time.smoothDeltaTime);
            }


            if ((Input.GetKey(Controls.JumpKey)))
            {
                if (OnGround)
                    Jump();
            }

            if (Input.GetKey(Controls.CloseTubeKey))
            {
                MyTube.CloseTube();
            }

            if (Input.GetKey(Controls.StartTubeKey))
            {
                MyTube.StartTube();
            }

            if (Input.GetKey(Controls.ClearAllTubesKey))
            {
                MyTube.ClearTubes();
            }

        }

        public void Jump()
        {
            VerticalMovementDirection = 1;
            OnGround = false;
            StartCoroutine(CurveInterp(Vector3.zero, new Vector3(0, jumpHeight, 0), jumpDuration, jumpCurve));
        }

        private void Land()
        {
            VerticalMovementDirection = -1;
            StartCoroutine(CurveInterp(Vector3.zero, new Vector3(0, -jumpHeight, 0), jumpDuration, jumpCurve));
        }

        private IEnumerator CurveInterp(Vector3 startPos, Vector3 endPos, float duration, AnimationCurve curve)
        {
            Vector3 prevPos = startPos;
            Vector3 targetPos;
            float timer = 0;
            float curveFraction;
            float forwardSpeed = 0;

            while (timer <= duration)
            {
                curveFraction = curve.Evaluate(Mathf.Clamp01(timer / duration));
                targetPos = Vector3.LerpUnclamped(startPos, endPos, curveFraction);
                transform.Translate(targetPos - prevPos, Space.Self);
                prevPos = targetPos;
                timer += Time.smoothDeltaTime;
                forwardSpeed = (VerticalMovementDirection == 1) ? Mathf.Lerp(speed, speed * jumpForwardSpeedModifier, timer / duration) : Mathf.Lerp(speed * jumpForwardSpeedModifier, speed, timer / duration);
                transform.position = transform.position + (transform.forward * forwardSpeed * Time.smoothDeltaTime);
                yield return null;
            }

            if (VerticalMovementDirection == -1) // Going Down
            {
                transform.position = new Vector3(transform.position.x, StartY, transform.position.z);
                OnGround = true;
            }


            if (VerticalMovementDirection == 1) // Going Up
            {
                transform.position = new Vector3(transform.position.x, jumpHeight + StartY, transform.position.z);
                VerticalMovementDirection = 0;
                Land();
                yield break;
            }

            VerticalMovementDirection = 0;
            yield break;
        }

    }
}