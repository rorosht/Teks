using UnityEngine;

namespace TeksMaster.World
{
    public class TeksCharacterCatalog : ScriptableObject
    {
        public const string ResourcesPath = "TeksCharacterCatalog";
        public const string AssetPath = "Assets/Resources/TeksCharacterCatalog.asset";
        public const string PackRootPath = "Assets/100 People - Animated Characters Pack";

        public GameObject[] Prefabs = new GameObject[100];
        public Texture2D CharacterAtlasTexture;
        public RuntimeAnimatorController AnimatorController;

        private static TeksCharacterCatalog _cachedCatalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCache()
        {
            _cachedCatalog = null;
        }

        public GameObject GetPrefabByNumber(int prefabNumber)
        {
            if (Prefabs == null || Prefabs.Length == 0) return null;
            int index = Mathf.Clamp(prefabNumber - 1, 0, Prefabs.Length - 1);
            return Prefabs[index];
        }

        public static TeksCharacterCatalog Load()
        {
            if (_cachedCatalog != null) return _cachedCatalog;

            _cachedCatalog = Resources.Load<TeksCharacterCatalog>(ResourcesPath);
#if UNITY_EDITOR
            if (_cachedCatalog == null)
            {
                _cachedCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<TeksCharacterCatalog>(AssetPath);
            }
#endif
            return _cachedCatalog;
        }

        public static GameObject LoadPrefab(
            int prefabNumber,
            out Texture2D atlasTexture,
            out RuntimeAnimatorController controller)
        {
            int clampedNumber = Mathf.Clamp(prefabNumber, 1, 100);
            atlasTexture = null;
            controller = null;
            GameObject prefab = null;

            var catalog = Load();
            if (catalog != null)
            {
                prefab = catalog.GetPrefabByNumber(clampedNumber);
                atlasTexture = catalog.CharacterAtlasTexture;
                controller = catalog.AnimatorController;
            }

#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PackRootPath}/1.Prefabs/body_root_{clampedNumber}.prefab");
            }
            if (atlasTexture == null)
            {
                atlasTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"{PackRootPath}/4.Materials/Texture.png");
            }
            if (controller == null)
            {
                controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    $"{PackRootPath}/3.Animations/0.character.controller");
            }
#endif
            return prefab;
        }
    }
}
