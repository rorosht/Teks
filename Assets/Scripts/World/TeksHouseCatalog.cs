using UnityEngine;

namespace TeksMaster.World
{
    public class TeksHouseCatalog : ScriptableObject
    {
        public const string ResourcesPath = "TeksHouseCatalog";
        public const string AssetPath = "Assets/Resources/TeksHouseCatalog.asset";
        public const string PackRootPath = "Assets/Quaint Game Studio/Low Poly City Pack Collection Low-Poly 3D Model-Demo";

        public static readonly string[] PrefabPaths =
        {
            PackRootPath + "/Prefebs/House_1.prefab",
            PackRootPath + "/Prefebs/House_2.prefab",
            PackRootPath + "/Prefebs/House_4.prefab"
        };

        public GameObject[] HousePrefabs = new GameObject[3];

        private static TeksHouseCatalog _cachedCatalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCache()
        {
            _cachedCatalog = null;
        }

        public GameObject GetPrefabByIndex(int variantIndex)
        {
            if (HousePrefabs == null || HousePrefabs.Length == 0) return null;
            int idx = ((variantIndex % HousePrefabs.Length) + HousePrefabs.Length) % HousePrefabs.Length;
            return HousePrefabs[idx];
        }

        public static TeksHouseCatalog Load()
        {
            if (_cachedCatalog != null) return _cachedCatalog;

            _cachedCatalog = Resources.Load<TeksHouseCatalog>(ResourcesPath);
#if UNITY_EDITOR
            if (_cachedCatalog == null)
            {
                _cachedCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<TeksHouseCatalog>(AssetPath);
            }
#endif
            return _cachedCatalog;
        }

        public static GameObject LoadPrefab(int variantIndex)
        {
            GameObject prefab = null;
            var catalog = Load();
            if (catalog != null)
            {
                prefab = catalog.GetPrefabByIndex(variantIndex);
            }

#if UNITY_EDITOR
            if (prefab == null && PrefabPaths.Length > 0)
            {
                int idx = ((variantIndex % PrefabPaths.Length) + PrefabPaths.Length) % PrefabPaths.Length;
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[idx]);
            }
#endif
            return prefab;
        }
    }
}
