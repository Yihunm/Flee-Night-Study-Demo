using System;
using UnityEngine;
namespace FleeNightStudy
{
    /// <summary>
    /// 从相机中心发射射线，按 E 与门交互：支持 <see cref="SlidingDoorInteractable"/>（推拉）与 <see cref="HingeDoorInteractable"/>（铰链）。
    /// 会跳过挂在同一角色上的自身碰撞体；沿射线<strong>遍历</strong>所有命中直到找到门（避免门框/墙挡在前面一侧能开、另一侧不能开）。
    /// 可选近距兜底：MeshCollider 常<strong>单面</strong>，从门背面射线打不中时可仍检测到门。
    /// </summary>
    public class DoorInteractor : MonoBehaviour
    {
        [SerializeField] Transform rayOrigin;
        [SerializeField] float interactDistance = 3.2f;
        [SerializeField] KeyCode interactKey = KeyCode.E;
        [SerializeField] LayerMask raycastMask = ~0;

        [Tooltip("Ignore：不打 Is Trigger 的碰撞体。门 Collider 若勾了 Trigger 却按 E 没反应，请改成 Collide。")]
        [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Tooltip("沿视线反方向平移射线起点（米），避免相机贴在门/墙内侧时射线从碰撞体内部发出导致打不中。")]
        [SerializeField] float rayStartBackoff = 0.15f;

        [Tooltip("父链上 GetComponentInParent 找不到时，沿父链向上逐层用子树搜索；层数过大可能搜到远处其它门，请按预制深度调整")]
        [SerializeField] int maxAncestorSearchDepth = 18;

        [Header("背面 / 遮挡兜底")]
        [Tooltip("MeshCollider 常单面，从门后射线打不中。>0 时在视线前方做 OverlapSphere 再找门；走廊多门时可略减小或设为 0 关闭")]
        [SerializeField] float proximityFallbackRadius = 0.85f;

        [Tooltip("兜底球体中心沿相机前方向前移的距离（米）")]
        [SerializeField] float proximityFallbackForward = 0.45f;

        [Tooltip("相机前向与「相机→门」夹角余弦下限，避免背后远处的门被误触")]
        [SerializeField] float proximityFacingDotMin = 0.2f;

        [Tooltip("勾选后输出按键、命中与查找结果")]
        [SerializeField] bool debugLog;

        void Reset()
        {
            if (rayOrigin == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) rayOrigin = cam.transform;
            }
        }

        void Update()
        {
            if (GameplayPauseUI.IsPaused)
                return;

            bool gameEnded = GameStateManager.Instance != null && GameStateManager.Instance.GameEnded;
            if (gameEnded) return;
            if (!Input.GetKeyDown(interactKey)) return;
            TryInteract();
        }

        void TryInteract()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
            {
                if (debugLog)
                    UnityEngine.Debug.Log("[DoorInteractor] 已忽略：GameStateManager.GameEnded", this);
                return;
            }

            if (rayOrigin == null)
            {
                if (debugLog && Input.GetKeyDown(interactKey))
                    UnityEngine.Debug.LogWarning("[DoorInteractor] Ray Origin 未赋值。", this);
                return;
            }

            Vector3 dir = rayOrigin.forward;
            Vector3 origin = rayOrigin.position - dir * rayStartBackoff;
            var ray = new Ray(origin, dir);
            float maxDist = interactDistance + rayStartBackoff;

            bool canInteract = TryResolveDoor(ray, maxDist, out SlidingDoorInteractable sliding, out HingeDoorInteractable hinge, out TextbookGatedDoorInteractable gated, out RaycastHit usedHit, out bool usedProximity);

            if (!canInteract)
            {
                if (debugLog)
                    UnityEngine.Debug.Log("[DoorInteractor] 射线/近距均未找到可交互门（可能距离过远或 LayerMask 排除）。", this);
                return;
            }

            if (debugLog)
            {
                string via = usedProximity ? "近距兜底" : $"命中 {usedHit.collider.name} path={BuildPath(usedHit.collider.transform)} dist={usedHit.distance:F2}";
                UnityEngine.Debug.Log($"[DoorInteractor] {via}", this);
            }

            if (sliding != null)
            {
                var schoolExit = FindSchoolExitDoor(sliding);
                if (schoolExit != null)
                {
                    if (schoolExit.TryToggle())
                        PlayDoorOpen();
                    else if (debugLog)
                        UnityEngine.Debug.Log($"[DoorInteractor] 校门未解锁：{schoolExit.GetLockedHint()}", this);
                    return;
                }

                if (gated == null)
                    gated = FindGatedDoor(sliding);
                if (gated != null)
                {
                    if (gated.TryToggle())
                        PlayDoorOpen();
                    else if (debugLog)
                        UnityEngine.Debug.Log($"[DoorInteractor] 课本不足，未开门：{gated.GetLockedHint()}", this);
                    return;
                }

                sliding.Toggle();
                PlayDoorOpen();
                return;
            }

            if (hinge != null)
            {
                var schoolExit = FindSchoolExitDoor(hinge);
                if (schoolExit != null)
                {
                    if (schoolExit.TryToggle())
                        PlayDoorOpen();
                    else if (debugLog)
                        UnityEngine.Debug.Log($"[DoorInteractor] 校门未解锁：{schoolExit.GetLockedHint()}", this);
                    return;
                }

                hinge.Toggle();
                PlayDoorOpen();
            }
        }

        static void PlayDoorOpen() => GameplayAudioManager.Instance?.PlayDoorOpen();

        static SchoolExitVictoryDoorInteractable FindSchoolExitDoor(Component doorComponent)
        {
            if (doorComponent == null) return null;
            var exit = doorComponent.GetComponent<SchoolExitVictoryDoorInteractable>();
            if (exit == null)
                exit = doorComponent.GetComponentInParent<SchoolExitVictoryDoorInteractable>();
            return exit;
        }

        static TextbookGatedDoorInteractable FindGatedDoor(SlidingDoorInteractable sliding)
        {
            if (sliding == null) return null;
            var gated = sliding.GetComponent<TextbookGatedDoorInteractable>();
            if (gated == null)
                gated = sliding.GetComponentInParent<TextbookGatedDoorInteractable>();
            return gated;
        }

        /// <summary>沿射线依次尝试每个非自身命中；若无门且开启兜底，再在相机前小球内找最近且朝向大致一致的门。</summary>
        bool TryResolveDoor(Ray ray, float maxDist, out SlidingDoorInteractable sliding, out HingeDoorInteractable hinge, out TextbookGatedDoorInteractable gated, out RaycastHit usedHit, out bool usedProximity)
        {
            sliding = null;
            hinge = null;
            gated = null;
            usedHit = default;
            usedProximity = false;

            var hits = Physics.RaycastAll(ray, maxDist, raycastMask, triggerInteraction);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (IsColliderOnThisCharacter(h.collider))
                    continue;

                if (TryGetDoorFromHit(h, out sliding, out hinge, out gated))
                {
                    usedHit = h;
                    return true;
                }
            }

            if (proximityFallbackRadius > 0f && TryProximityFallback(maxDist, out sliding, out hinge, out gated))
            {
                usedProximity = true;
                return true;
            }

            sliding = null;
            hinge = null;
            gated = null;
            return false;
        }

        bool TryGetDoorFromHit(RaycastHit h, out SlidingDoorInteractable sliding, out HingeDoorInteractable hinge, out TextbookGatedDoorInteractable gated)
        {
            sliding = h.collider.GetComponentInParent<SlidingDoorInteractable>();
            if (sliding == null)
                sliding = FindDoorInAncestorSubtrees<SlidingDoorInteractable>(h);
            if (sliding != null)
            {
                hinge = null;
                gated = FindGatedDoor(sliding);
                return true;
            }

            hinge = h.collider.GetComponentInParent<HingeDoorInteractable>();
            if (hinge == null)
                hinge = FindDoorInAncestorSubtrees<HingeDoorInteractable>(h);
            gated = null;
            return hinge != null;
        }

        bool TryProximityFallback(float maxDist, out SlidingDoorInteractable sliding, out HingeDoorInteractable hinge, out TextbookGatedDoorInteractable gated)
        {
            sliding = null;
            hinge = null;
            gated = null;
            if (rayOrigin == null)
                return false;

            Vector3 probeCenter = rayOrigin.position + rayOrigin.forward * Mathf.Min(proximityFallbackForward, maxDist * 0.5f);
            var cols = Physics.OverlapSphere(probeCenter, proximityFallbackRadius, raycastMask, triggerInteraction);
            Vector3 from = rayOrigin.position;
            Vector3 fwd = rayOrigin.forward;

            float bestS = float.MaxValue;
            SlidingDoorInteractable bestSliding = null;
            float bestH = float.MaxValue;
            HingeDoorInteractable bestHinge = null;

            foreach (var col in cols)
            {
                if (col == null || IsColliderOnThisCharacter(col))
                    continue;

                Vector3 toDoor = col.bounds.center - from;
                float mag = toDoor.magnitude;
                if (mag > 1e-4f)
                {
                    Vector3 nd = toDoor / mag;
                    if (Vector3.Dot(fwd, nd) < proximityFacingDotMin)
                        continue;
                }

                Vector3 refPt = col.ClosestPoint(from);
                var s = col.GetComponentInParent<SlidingDoorInteractable>();
                if (s == null)
                    s = FindDoorInAncestorSubtreesFromTransform<SlidingDoorInteractable>(col.transform, refPt);

                if (s != null)
                {
                    float d = (s.transform.position - from).sqrMagnitude;
                    if (d < bestS)
                    {
                        bestS = d;
                        bestSliding = s;
                    }
                }

                var g = col.GetComponentInParent<HingeDoorInteractable>();
                if (g == null)
                    g = FindDoorInAncestorSubtreesFromTransform<HingeDoorInteractable>(col.transform, refPt);

                if (g != null)
                {
                    float d = (g.transform.position - from).sqrMagnitude;
                    if (d < bestH)
                    {
                        bestH = d;
                        bestHinge = g;
                    }
                }
            }

            if (bestSliding != null)
            {
                sliding = bestSliding;
                hinge = null;
                gated = FindGatedDoor(sliding);
                return true;
            }

            if (bestHinge != null)
            {
                hinge = bestHinge;
                gated = null;
                return true;
            }

            return false;
        }

        T FindDoorInAncestorSubtreesFromTransform<T>(Transform start, Vector3 worldRef) where T : Component
        {
            Transform t = start;
            for (int i = 0; i < maxAncestorSearchDepth && t != null; i++, t = t.parent)
            {
                var all = t.GetComponentsInChildren<T>(true);
                if (all.Length == 0) continue;
                if (all.Length == 1) return all[0];
                return PickNearest(worldRef, all);
            }
            return null;
        }

        bool IsColliderOnThisCharacter(Collider col)
        {
            if (col == null) return true;
            Transform t = col.transform;
            return t == transform || t.IsChildOf(transform);
        }

        T FindDoorInAncestorSubtrees<T>(RaycastHit hit) where T : Component
        {
            return FindDoorInAncestorSubtreesFromTransform<T>(hit.collider.transform, hit.point);
        }

        static T PickNearest<T>(Vector3 worldRef, T[] comps) where T : Component
        {
            T best = null;
            float bestD = float.MaxValue;
            foreach (var c in comps)
            {
                if (c == null) continue;
                float d = (c.transform.position - worldRef).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = c;
                }
            }
            return best;
        }

        static string BuildPath(Transform tr)
        {
            if (tr == null) return "";
            string s = tr.name;
            Transform t = tr;
            while (t.parent != null)
            {
                t = t.parent;
                s = t.name + "/" + s;
            }
            return s;
        }
    }
}
