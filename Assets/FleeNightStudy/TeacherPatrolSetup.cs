using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace FleeNightStudy
{
    public static class TeacherPatrolSetup
    {
        public static Transform EnsurePatrolRoot(Transform gameplayRoot)
        {
            var patrolRoot = GameObject.Find("PatrolPoints");
            if (patrolRoot != null)
                return patrolRoot.transform;

            patrolRoot = new GameObject("PatrolPoints");
            if (gameplayRoot != null)
                patrolRoot.transform.SetParent(gameplayRoot, false);
            return patrolRoot.transform;
        }

        public static Transform[] AssignPatrolRoute(
            TeacherController teacher,
            int routeIndex,
            Transform patrolRoot)
        {
            if (teacher == null || patrolRoot == null)
                return null;

            var route = TeacherPatrolConfig.GetRoute(routeIndex);
            float designFloorY = TeacherPatrolConfig.GetRouteDesignFloorY(routeIndex);
            var points = new Transform[route.Length];
            string prefix = teacher.gameObject.name;

            for (int i = 0; i < route.Length; i++)
            {
                string pointName = $"{prefix}_Patrol_{i}";
                var existing = patrolRoot.Find(pointName);
                GameObject pointGo;
                if (existing != null)
                    pointGo = existing.gameObject;
                else
                {
                    pointGo = new GameObject(pointName);
                    pointGo.transform.SetParent(patrolRoot, false);
                }

                pointGo.transform.position = TeacherPatrolConfig.SnapToNavMeshOnFloor(route[i], designFloorY);
                points[i] = pointGo.transform;
            }

            teacher.SetPatrolPoints(points);
            teacher.SetPatrolRouteIndex(routeIndex);
            teacher.SetHomeFloorY(designFloorY);

            var start = points[0].position;
            teacher.transform.position = start;

            var agent = teacher.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.Warp(start);

            return points;
        }

        public static void ApplyAllPatrolRoutes()
        {
            var gameplayRoot = GameObject.Find("FleeNightStudy_Gameplay")?.transform;
            var patrolRoot = EnsurePatrolRoot(gameplayRoot);

            var teachers = Object.FindObjectsOfType<TeacherController>(true)
                .Where(t => t != null && !t.IsHeadTeacher)
                .OrderBy(t => t.name)
                .ToList();

            for (int i = 0; i < teachers.Count; i++)
                AssignPatrolRoute(teachers[i], i % TeacherPatrolConfig.RouteCount, patrolRoot);
        }
    }
}
