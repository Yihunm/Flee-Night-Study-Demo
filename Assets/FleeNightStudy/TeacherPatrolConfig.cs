using UnityEngine;
using UnityEngine.AI;

namespace FleeNightStudy
{
    /// <summary>三名巡查老师固定巡逻线段（起点 ↔ 终点）。索引 0=三楼 Raana，1=二楼 Tomori，2=一楼 Anon。</summary>
    public static class TeacherPatrolConfig
    {
        /// <summary>各楼层典型高度（索引 0=一楼, 1=二楼, 2=三楼；与 Routes 下标无关）。</summary>
        public static readonly float[] FloorHeights =
        {
            0.185007f,
            4.185007f,
            8.185007f,
        };

        public static readonly Vector3[][] Routes =
        {
            new[]
            {
                new Vector3(28.40796f, 8.185007f, -10.50008f),
                new Vector3(-30.70179f, 8.185007f, -9.884552f),
            },
            new[]
            {
                new Vector3(28.40796f, 4.185007f, -10.50008f),
                new Vector3(-30.70179f, 4.185007f, -9.884552f),
            },
            new[]
            {
                new Vector3(28.40796f, 0.185007f, -10.50008f),
                new Vector3(-30.70179f, 0.185007f, -9.884552f),
            },
        };

        public static int RouteCount => Routes.Length;

        public static Vector3[] GetRoute(int routeIndex)
        {
            if (routeIndex < 0 || routeIndex >= Routes.Length)
                return Routes[0];
            return Routes[routeIndex];
        }

        public static int GetFloorIndex(float worldY)
        {
            int best = 0;
            float bestDelta = float.MaxValue;
            for (int i = 0; i < FloorHeights.Length; i++)
            {
                float d = Mathf.Abs(worldY - FloorHeights[i]);
                if (d < bestDelta)
                {
                    bestDelta = d;
                    best = i;
                }
            }

            return best;
        }

        public static float GetFloorHeight(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= FloorHeights.Length)
                return FloorHeights[0];
            return FloorHeights[floorIndex];
        }

        public static float GetRouteDesignFloorY(int routeIndex) => GetRoute(routeIndex)[0].y;

        /// <summary>在指定楼层高度采样 NavMesh，并锁定设计 Y（避免三楼漂到更高 NavMesh）。</summary>
        public static Vector3 SnapToNavMeshOnFloor(Vector3 pos, float floorY, float radius = 12f)
        {
            var samplePos = new Vector3(pos.x, floorY, pos.z);
            if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, radius, NavMesh.AllAreas) ||
                NavMesh.SamplePosition(samplePos + Vector3.up * 12f, out hit, radius + 8f, NavMesh.AllAreas))
            {
                var result = hit.position;
                result.y = floorY;
                return result;
            }

            return samplePos;
        }

        public static Vector3 SnapToNavMesh(Vector3 pos, float radius = 12f)
        {
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;

            if (NavMesh.SamplePosition(pos + Vector3.up * 15f, out hit, radius + 8f, NavMesh.AllAreas))
                return hit.position;

            return pos;
        }
    }
}
