using UnityEngine;

namespace FleeNightStudy
{
    /// <summary>确保 UI 物体带有 RectTransform。</summary>
    public static class UiRectUtil
    {
        public static RectTransform GetRectTransform(GameObject go)
        {
            if (go == null)
                return null;

            return go.transform as RectTransform ?? go.GetComponent<RectTransform>();
        }

        public static RectTransform GetRectTransform(Transform tr)
        {
            if (tr == null)
                return null;

            return tr as RectTransform ?? tr.GetComponent<RectTransform>();
        }

        public static RectTransform EnsureRectTransform(GameObject go)
        {
            var rect = GetRectTransform(go);
            if (rect != null)
                return rect;

            var canvas = go.GetComponentInParent<Canvas>() ?? Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                int sibling = go.transform.GetSiblingIndex();
                go.transform.SetParent(canvas.transform, false);
                go.transform.SetSiblingIndex(sibling);
                rect = GetRectTransform(go);
            }

            return rect;
        }

        public static GameObject CreateUiObject(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform CreateUiChild(Transform parent, string name)
        {
            var go = CreateUiObject(parent, name);
            return GetRectTransform(go);
        }
    }
}
