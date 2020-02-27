using System.Collections;
using UnityEngine;


namespace Example
{

    [System.Serializable]
    struct PlayerInputControls
    {
        public KeyCode LeftKey;
        public KeyCode RightKey;
        public KeyCode ForwardKey;
        public KeyCode JumpKey;
    }

    internal enum VerticalMovementDirection
    {
        up,
        down,
        none,
    }

    
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField]
        [Range(3f, 10.0f)]
        private float speed = 3f;

        [SerializeField]
        [Range(1f, 10.0f)]
        private float turnSpeed = 1f;

        [SerializeField]
        private AnimationCurve jumpCurve = null;

        [SerializeField]
        private float jumpHeight = 3;

        [SerializeField]
        private float jumpDuration = 1.0f;

        [SerializeField]
        private float jumpForwardSpeedModifier = 2.0f;

        [SerializeField]
        private PlayerInputControls Controls = new PlayerInputControls
        {
            LeftKey = KeyCode.LeftArrow,
            RightKey = KeyCode.RightArrow,
            ForwardKey = KeyCode.UpArrow,
            JumpKey = KeyCode.Space,
        };

        
        private float StartY { get; set; } = 0f;
        private bool OnGround { get; set; } = true;    
        private VerticalMovementDirection VerticalMoveDir { get; set; } = VerticalMovementDirection.none;

        private void Awake()
        {
            StartY = transform.position.y;
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
                {
                    Jump();
                }                  
            }
        }

        public void Jump()
        {
            VerticalMoveDir = VerticalMovementDirection.up;
            OnGround = false;
            StartCoroutine(CurveInterp(Vector3.zero, new Vector3(0, jumpHeight, 0), jumpDuration, jumpCurve));
        }

        private void Land()
        {
            VerticalMoveDir = VerticalMovementDirection.down;
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
                forwardSpeed = (VerticalMoveDir == VerticalMovementDirection.up) ? Mathf.Lerp(speed, speed * jumpForwardSpeedModifier, timer / duration) : Mathf.Lerp(speed * jumpForwardSpeedModifier, speed, timer / duration);
                transform.position = transform.position + (transform.forward * forwardSpeed * Time.smoothDeltaTime);
                    
                yield return null;
            }

            if (VerticalMoveDir == VerticalMovementDirection.down)
            {
                transform.position = new Vector3(transform.position.x, StartY, transform.position.z);
                OnGround = true;
            }
           

            if (VerticalMoveDir == VerticalMovementDirection.up)
            {
                transform.position = new Vector3(transform.position.x, jumpHeight + StartY, transform.position.z);
                VerticalMoveDir = VerticalMovementDirection.none;
                Land();
                yield break;
            }

            VerticalMoveDir = VerticalMovementDirection.none;
            yield break;
              
        }
    }
}

