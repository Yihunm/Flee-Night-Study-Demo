using UnityEngine;

namespace FleeNightStudy
{
    public class AlarmClockProjectile : MonoBehaviour
    {
        bool _landed;

        public void Launch(Vector3 forward)
        {
            Destroy(gameObject, 8f);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (_landed) return;
            _landed = true;
            var teachers = FindObjectsOfType<TeacherController>();
            TeacherController nearest = null;
            float best = float.MaxValue;
            foreach (var t in teachers)
            {
                float d = Vector3.Distance(t.transform.position, transform.position);
                if (d < best) { best = d; nearest = t; }
            }
            if (nearest != null)
                nearest.LureTo(transform.position, 6f);
            Destroy(gameObject, 0.5f);
        }
    }

    public class ChalkBombProjectile : MonoBehaviour
    {
        void OnCollisionEnter(Collision collision)
        {
            var teacher = collision.collider.GetComponentInParent<TeacherController>();
            if (teacher != null)
            {
                teacher.Stun(2f);
                teacher.SetBlinded(2f);
            }
            Destroy(gameObject);
        }
    }
}
