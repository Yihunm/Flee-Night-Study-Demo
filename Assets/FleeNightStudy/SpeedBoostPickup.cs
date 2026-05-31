using System.Collections;
using System.Reflection;
using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>短时加速 FirstPersonWalker（或场景里的 SimpleFPController）。</summary>
    public class SpeedBoostPickup : MonoBehaviour
    {
        [SerializeField] float multiplier = 1.6f;
        [SerializeField] float duration = 5f;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var root = other.transform.root;
            var walker = root.GetComponentInChildren<FirstPersonWalker>();
            if (walker != null)
            {
                walker.StartCoroutine(BoostWalker(walker, multiplier, duration));
                Destroy(gameObject);
                return;
            }

            var fp = FindSimpleFp(root);
            if (fp != null)
            {
                fp.StartCoroutine(BoostSimpleFpRefl(fp, multiplier, duration));
                Destroy(gameObject);
            }
        }

        static MonoBehaviour FindSimpleFp(Transform root)
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null && mb.GetType().Name == "SimpleFPController")
                    return mb;
            }
            return null;
        }

        static IEnumerator BoostWalker(FirstPersonWalker w, float mult, float dur)
        {
            w.SpeedMultiplier = mult;
            yield return new WaitForSeconds(dur);
            if (w != null) w.SpeedMultiplier = 1f;
        }

        static IEnumerator BoostSimpleFpRefl(MonoBehaviour fp, float mult, float dur)
        {
            var t = fp.GetType();
            var wField = t.GetField("walkSpeed", BindingFlags.Instance | BindingFlags.Public);
            var rField = t.GetField("runSpeed", BindingFlags.Instance | BindingFlags.Public);
            if (wField == null || rField == null) yield break;

            float w = (float)wField.GetValue(fp);
            float r = (float)rField.GetValue(fp);
            wField.SetValue(fp, w * mult);
            rField.SetValue(fp, r * mult);
            yield return new WaitForSeconds(dur);
            if (fp != null)
            {
                wField.SetValue(fp, w);
                rField.SetValue(fp, r);
            }
        }
    }
}
