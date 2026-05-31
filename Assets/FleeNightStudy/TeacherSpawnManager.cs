using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace FleeNightStudy
{
    /// <summary>按难度生成老师：简单=三名巡查；普通=仅班主任（开局登场）。</summary>
    public class TeacherSpawnManager : MonoBehaviour
    {
        static readonly Vector3 HeadTeacherSpawnPosition =
            new Vector3(-29.96499f, 8.185007f, 11.19862f);

        [SerializeField] Transform gameplayRoot;

        void Start()
        {
            if (gameplayRoot == null)
            {
                var root = GameObject.Find("FleeNightStudy_Gameplay");
                if (root != null) gameplayRoot = root.transform;
            }

            var patrolRoot = TeacherPatrolSetup.EnsurePatrolRoot(gameplayRoot);
            TeacherPatrolSetup.ApplyAllPatrolRoutes();

            foreach (var teacher in FindObjectsOfType<TeacherController>())
                TeacherNpcBuilder.UpgradeLegacyCapsuleTeacher(teacher);

            var player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (GameSessionData.SpawnHeadTeacherAtLevelStart)
            {
                RemovePatrolTeachersInScene();
                SpawnHeadTeacherIfMissing();
                return;
            }

            int existing = FindObjectsOfType<TeacherController>().Count(t => !t.IsHeadTeacher);
            int need = GameSessionData.PatrolTeacherCount;
            for (int i = existing; i < need; i++)
            {
                float floorY = TeacherPatrolConfig.GetRouteDesignFloorY(i);
                var pos = TeacherPatrolConfig.SnapToNavMeshOnFloor(
                    TeacherPatrolConfig.GetRoute(i)[0], floorY);
                TeacherNpcBuilder.Spawn($"Teacher_NPC_{i + 1}", false, pos, gameplayRoot, player, i, patrolRoot);
            }
        }

        static void RemovePatrolTeachersInScene()
        {
            foreach (var teacher in FindObjectsOfType<TeacherController>())
            {
                if (teacher != null && !teacher.IsHeadTeacher)
                    Destroy(teacher.gameObject);
            }
        }

        void SpawnHeadTeacherIfMissing()
        {
            if (FindObjectsOfType<TeacherController>().Any(t => t.IsHeadTeacher))
                return;

            SpawnHeadTeacher();
        }

        void SpawnHeadTeacher()
        {
            if (FindObjectsOfType<TeacherController>().Any(t => t.IsHeadTeacher))
                return;

            var patrolRoot = TeacherPatrolSetup.EnsurePatrolRoot(gameplayRoot);
            var spawnPos = SnapToNavMesh(HeadTeacherSpawnPosition);
            var player = GameObject.FindGameObjectWithTag("Player")?.transform;

            var headTeacher = TeacherNpcBuilder.Spawn(
                "HeadTeacher_NPC", true, spawnPos, gameplayRoot, player, 0, patrolRoot);
            headTeacher?.BeginHuntPlayer();

            if (GameSessionData.SpawnHeadTeacherAtLevelStart)
                GameStateManager.Instance?.ShowMessage("班主任正在校园里巡视，小心！");
            else
            {
                GameStateManager.Instance?.ShowMessage("班主任发现有人离开了！");
                GameplayAudioManager.Instance?.PlayTeacherCough();
            }
        }

        static Vector3 SnapToNavMesh(Vector3 worldPos)
        {
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 12f, NavMesh.AllAreas))
                return hit.position;
            return worldPos;
        }
    }
}
