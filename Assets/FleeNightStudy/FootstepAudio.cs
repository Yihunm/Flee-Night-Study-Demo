using UnityEngine;

namespace FleeNightStudy
{
    [RequireComponent(typeof(FirstPersonWalker))]
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] float stepInterval = 0.42f;
        CharacterController _cc;
        float _timer;

        void Awake() => _cc = GetComponent<CharacterController>();

        void Update()
        {
            if (_cc == null || GameStateManager.Instance != null && GameStateManager.Instance.GameEnded) return;
            if (!_cc.isGrounded) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f) { _timer = 0f; return; }

            _timer += Time.deltaTime;
            if (_timer >= stepInterval)
            {
                _timer = 0f;
                GameplayAudioManager.Instance?.PlayFootstep();
            }
        }
    }
}
