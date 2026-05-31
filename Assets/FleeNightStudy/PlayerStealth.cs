using System.Collections;
using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>隐身校服：老师无法发现，玩家半透明且关闭碰撞检测失败。</summary>
    public class PlayerStealth : MonoBehaviour
    {
        [SerializeField] Renderer[] renderers;
        [SerializeField] Collider[] catchColliders;

        bool _stealthActive;
        Coroutine _routine;

        public bool IsStealthActive => _stealthActive;

        void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();
            if (catchColliders == null || catchColliders.Length == 0)
            {
                var cc = GetComponent<CharacterController>();
                if (cc != null) catchColliders = new[] { cc };
            }
        }

        public void ActivateStealth(float duration)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(StealthRoutine(duration));
        }

        IEnumerator StealthRoutine(float duration)
        {
            _stealthActive = true;
            SetVisualAlpha(0.35f);
            SetCatchColliders(false);
            yield return new WaitForSeconds(duration);
            _stealthActive = false;
            SetVisualAlpha(1f);
            SetCatchColliders(true);
            _routine = null;
        }

        void SetVisualAlpha(float a)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        var c = mat.color;
                        c.a = a;
                        mat.color = c;
                    }
                }
            }
        }

        void SetCatchColliders(bool enabled)
        {
            if (catchColliders == null) return;
            foreach (var c in catchColliders)
            {
                if (c != null && !(c is CharacterController))
                    c.enabled = enabled;
            }
        }
    }
}
