using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 5f, -9.5f);
        public float positionSmoothTime = 0.18f;
        public float baseFOV = 60f;
        public float maxFOVBonus = 9f;

        Camera cam;
        Vector3 velocity;
        Vector3 lastTargetPosition;
        bool hasLastTargetPosition;
        public Vector3 shakeOffset;

        void Awake() => cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset + shakeOffset;
            bool targetReset = hasLastTargetPosition && target.position.z < lastTargetPosition.z - 1f;
            if (targetReset)
            {
                velocity = Vector3.zero;
                shakeOffset = Vector3.zero;
                transform.position = target.position + offset;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, positionSmoothTime);
            }
            lastTargetPosition = target.position;
            hasLastTargetPosition = true;
            transform.LookAt(target.position + Vector3.up * 1.05f);

            if (GameManager.Instance != null)
            {
                float speedT = Mathf.InverseLerp(GameManager.Instance.RunStartingSpeed, GameManager.Instance.maxSpeed,
                                                 GameManager.Instance.currentSpeed);
                cam.fieldOfView = baseFOV + maxFOVBonus * speedT;
            }
        }
    }
}


