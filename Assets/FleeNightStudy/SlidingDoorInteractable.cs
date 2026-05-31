using System;
using System.Collections.Generic;
using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>
    /// 推拉门：在父物体本地空间内平滑移动门扇的 <c>localPosition</c>。
    /// 支持<strong>多扇</strong>（双开门左右各一片），每片可设不同 <see cref="SlideLeaf.slideLocalDelta"/>。
    /// 若 <see cref="slideLeaves"/> 为空，则使用旧的 <see cref="panel"/> + <see cref="slideLocalDelta"/> 单扇逻辑。
    /// </summary>
    public class SlidingDoorInteractable : MonoBehaviour
    {
        [Serializable]
        public struct SlideLeaf
        {
            public Transform target;
            [Tooltip("该门扇从「关」到「开」在父空间下的 localPosition 增量")]
            public Vector3 slideLocalDelta;
        }

        [Tooltip("多扇门扇；非空时优先使用，忽略下方单 panel 字段")]
        [SerializeField] SlideLeaf[] slideLeaves;

        [Header("单扇（兼容旧版）")]
        [Tooltip("沿轨道滑动的门扇；为空则用本物体 Transform")]
        [SerializeField] Transform panel;

        [Tooltip("单扇时的位移增量")]
        [SerializeField] Vector3 slideLocalDelta = new Vector3(0.85f, 0f, 0f);

        [Tooltip("门扇每秒沿 local 空间移动的最大距离（MoveTowards）")]
        [SerializeField] float moveSpeed = 2.5f;

        [Header("通行")]
        [Tooltip("开门时禁用门物体及子物体上的非 Trigger 碰撞体（Snaps 门框 MeshCollider 常挡在门洞上）")]
        [SerializeField] bool disableBlockingCollidersWhenOpen = true;

        [Tooltip("留空则自动收集本物体子树内全部非 Trigger Collider")]
        [SerializeField] Collider[] blockingColliders;

        LeafRuntime[] _leaves;
        bool _isOpen;
        Collider[] _resolvedBlockingColliders;

        struct LeafRuntime
        {
            public Transform Target;
            public Vector3 ClosedLocal;
            public Vector3 OpenLocal;
            public Vector3 TargetLocal;
        }

        void Awake()
        {
            BuildRuntimeLeaves();
            ResolveBlockingColliders();
            foreach (var lw in _leaves)
            {
                if (lw.Target == null) continue;
                if (lw.Target.GetComponentInParent<Animator>() != null)
                    UnityEngine.Debug.LogWarning($"[SlidingDoorInteractable] {name} 的门扇 {lw.Target.name} 父级上存在 Animator，可能会每帧覆盖 localPosition；若门不动请禁用该 Animator 或取消 Apply Root Motion。", this);
                if (lw.Target.gameObject.isStatic)
                    UnityEngine.Debug.LogWarning($"[SlidingDoorInteractable] 门扇「{lw.Target.name}」勾选了 Static（Snaps 预制体常为全 Static）。运行时改位置可能画面不动；请在 Inspector 右上角取消 Static，至少关掉 Batching Static。", lw.Target);
            }
            ApplyColliderState(_isOpen);
        }

        void BuildRuntimeLeaves()
        {
            if (slideLeaves != null && slideLeaves.Length > 0)
            {
                var normalized = NormalizeTwinLeafDeltas(slideLeaves);
                _leaves = new LeafRuntime[normalized.Length];
                for (int i = 0; i < normalized.Length; i++)
                {
                    if (normalized[i].target == null)
                        UnityEngine.Debug.LogWarning($"[SlidingDoorInteractable] {name} 的 slideLeaves[{i}] 未指定 target，已回退为根物体，可能带动整面墙；请拖入实际门扇 Transform。", this);
                    var t = normalized[i].target != null ? normalized[i].target : transform;
                    _leaves[i] = new LeafRuntime
                    {
                        Target = t,
                        ClosedLocal = t.localPosition,
                        OpenLocal = t.localPosition + normalized[i].slideLocalDelta
                    };
                    _leaves[i].TargetLocal = _leaves[i].ClosedLocal;
                }
                return;
            }

            Transform single = panel != null ? panel : transform;
            _leaves = new[]
            {
                new LeafRuntime
                {
                    Target = single,
                    ClosedLocal = single.localPosition,
                    OpenLocal = single.localPosition + slideLocalDelta,
                    TargetLocal = single.localPosition
                }
            };
        }

        /// <summary>双开门两片 slideLocalDelta 同向时，按 localPosition.x 自动让左右门扇反向滑开。</summary>
        static SlideLeaf[] NormalizeTwinLeafDeltas(SlideLeaf[] leaves)
        {
            if (leaves.Length != 2 || leaves[0].target == null || leaves[1].target == null)
                return leaves;

            var a = leaves[0].slideLocalDelta;
            var b = leaves[1].slideLocalDelta;
            if (Mathf.Abs(a.x) < 1e-4f || Mathf.Abs(b.x) < 1e-4f)
                return leaves;
            if (Mathf.Sign(a.x) == Mathf.Sign(b.x))
            {
                var copy = (SlideLeaf[])leaves.Clone();
                float mag = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(b.x));
                float leftX = copy[0].target.localPosition.x;
                float rightX = copy[1].target.localPosition.x;
                if (leftX <= rightX)
                {
                    copy[0].slideLocalDelta = new Vector3(-mag, a.y, a.z);
                    copy[1].slideLocalDelta = new Vector3(mag, b.y, b.z);
                }
                else
                {
                    copy[0].slideLocalDelta = new Vector3(mag, a.y, a.z);
                    copy[1].slideLocalDelta = new Vector3(-mag, b.y, b.z);
                }
                UnityEngine.Debug.Log($"[SlidingDoorInteractable] 双开门 {copy[0].target.root.name} 两片同向滑动，已自动改为左右对开。", copy[0].target);
                return copy;
            }

            return leaves;
        }

        void ResolveBlockingColliders()
        {
            if (blockingColliders != null && blockingColliders.Length > 0)
            {
                _resolvedBlockingColliders = blockingColliders;
                return;
            }

            var list = new List<Collider>();
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                if (col != null && !col.isTrigger)
                    list.Add(col);
            }
            _resolvedBlockingColliders = list.ToArray();
        }

        void LateUpdate()
        {
            if (_leaves == null) return;
            float step = moveSpeed * Time.deltaTime;
            for (int i = 0; i < _leaves.Length; i++)
            {
                var lw = _leaves[i];
                if (lw.Target == null) continue;
                lw.Target.localPosition = Vector3.MoveTowards(lw.Target.localPosition, lw.TargetLocal, step);
                _leaves[i] = lw;
            }
        }

        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        public void SetOpen(bool open)
        {
            _isOpen = open;
            for (int i = 0; i < _leaves.Length; i++)
            {
                var lw = _leaves[i];
                if (lw.Target == null) continue;
                lw.TargetLocal = _isOpen ? lw.OpenLocal : lw.ClosedLocal;
                _leaves[i] = lw;
            }
            ApplyColliderState(_isOpen);
        }

        void ApplyColliderState(bool isOpen)
        {
            if (!disableBlockingCollidersWhenOpen || _resolvedBlockingColliders == null)
                return;

            bool enable = !isOpen;
            foreach (var col in _resolvedBlockingColliders)
            {
                if (col != null)
                    col.enabled = enable;
            }
        }
    }
}
