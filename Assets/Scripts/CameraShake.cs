using UnityEngine;

namespace AutoChess
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance;

        [SerializeField] float decaySpeed = 8f;

        Vector3 origin;
        float magnitude;
        bool originCaptured;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            CaptureOrigin();
        }

        void CaptureOrigin()
        {
            origin = transform.localPosition;
            originCaptured = true;
        }

        public static void Trigger(float amount)
        {
            if (Instance != null) Instance.DoShake(amount);
        }

        void DoShake(float amount)
        {
            if (!originCaptured) CaptureOrigin();
            magnitude = Mathf.Max(magnitude, amount);
        }

        void LateUpdate()
        {
            if (!originCaptured) return;

            if (magnitude <= 0f)
            {
                transform.localPosition = origin;
                return;
            }

            Vector2 offset = Random.insideUnitCircle * magnitude;
            transform.localPosition = origin + new Vector3(offset.x, offset.y, 0f);
            magnitude = Mathf.Max(0f, magnitude - Time.unscaledDeltaTime * decaySpeed);
        }
    }
}
