namespace FleeNightStudy
{
    /// <summary>
    /// 老师角色视觉资源（Resources/FleeNightStudy/Teachers/）。
    /// 巡查：三楼 Raana、二楼 Tomori、一楼 Anon；班主任 = 素世 Soyo。
    /// </summary>
    public static class TeacherNpcModelRegistry
    {
        /// <summary>TeacherPatrolConfig.Routes 索引：三楼。</summary>
        public const int PatrolRouteThirdFloor = 0;

        /// <summary>二楼。</summary>
        public const int PatrolRouteSecondFloor = 1;

        /// <summary>一楼。</summary>
        public const int PatrolRouteFirstFloor = 2;

        public const string AnonPatrolResource = "FleeNightStudy/Teachers/Anon_Patrol";
        public const string TomoriPatrolResource = "FleeNightStudy/Teachers/Tomori_Patrol";
        public const string RaanaPatrolResource = "FleeNightStudy/Teachers/Raana_Patrol";
        public const string SoyoHeadResource = "FleeNightStudy/Teachers/Soyo_HeadTeacher";

        public const string FallbackPatrolResource = "FleeNightStudy/Teachers/PatrolTeacherVisual";
        public const string FallbackHeadResource = "FleeNightStudy/Teachers/HeadTeacherVisual";

        public const string AnonSourceFolder = "Assets/FleeNightStudy/Characters/Source/Anon";
        public const string TomoriSourceFolder = "Assets/FleeNightStudy/Characters/Source/Tomori";
        public const string RaanaSourceFolder = "Assets/FleeNightStudy/Characters/Source/Raana";
        public const string SoyoSourceFolder = "Assets/FleeNightStudy/Characters/Source/Soyo";

        public const string AnonMyGoSourceFolder = "Assets/FleeNightStudy/Characters/Source/Anon/Anon_MyGO";
        public const string TomoriMyGoSourceFolder = "Assets/FleeNightStudy/Characters/Source/Tomori/Tomori_MyGO";
        public const string RaanaMyGoSourceFolder = "Assets/FleeNightStudy/Characters/Source/Raana/Raana_MyGO";
        public const string SoyoMyGoSourceFolder = "Assets/FleeNightStudy/Characters/Source/Soyo/Soyo_MyGO";

        public const string AnonBodyFbxAsset = "Assets/FleeNightStudy/Characters/Source/Anon/Anon_MyGO/CH_037_cos_live_default_Body.fbx";
        public const string AnonHeadFbxAsset = "Assets/FleeNightStudy/Characters/Source/Anon/Anon_MyGO/CH_037_cos_live_default_Head.fbx";

        public const string SoyoBodyFbxAsset = "Assets/FleeNightStudy/Characters/Source/Soyo/Soyo_MyGO/CH_039_cos_live_default_Body.fbx";
        public const string SoyoHeadFbxAsset = "Assets/FleeNightStudy/Characters/Source/Soyo/Soyo_MyGO/CH_039_cos_live_default_Head.fbx";

        public const string AnonPatrolPrefabAsset = "Assets/FleeNightStudy/Characters/Teachers/Anon_Patrol.prefab";
        public const string TomoriPatrolPrefabAsset = "Assets/FleeNightStudy/Characters/Teachers/Tomori_Patrol.prefab";
        public const string RaanaPatrolPrefabAsset = "Assets/FleeNightStudy/Characters/Teachers/Raana_Patrol.prefab";
        public const string SoyoHeadTeacherPrefabAsset = "Assets/FleeNightStudy/Characters/Teachers/Soyo_HeadTeacher.prefab";

        public static string GetPatrolResourceForRoute(int routeIndex)
        {
            switch (routeIndex)
            {
                case PatrolRouteThirdFloor:
                    return RaanaPatrolResource;
                case PatrolRouteSecondFloor:
                    return TomoriPatrolResource;
                case PatrolRouteFirstFloor:
                default:
                    return AnonPatrolResource;
            }
        }
    }
}
