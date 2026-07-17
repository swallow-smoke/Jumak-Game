using System.IO;
using _001_Scripts._003_Object._000_Structure.Cooker;
using UnityEditor;
using UnityEngine;

namespace _001_Scripts._900_Tools.Editor
{
    public static class CookingEffectsInstaller
    {
        private const string GrillAudioPath = "Assets/002_Resources/006_Audio/grill_sound.mp3";
        private const string InteractionAudioPath = "Assets/002_Resources/006_Audio/interactionSound.mp3";

        private static readonly string[] CookingPrefabPaths =
        {
            "Assets/003_Prefabs/Structure/boiler.prefab",
            "Assets/003_Prefabs/Structure/buncul.prefab",
            "Assets/003_Prefabs/Structure/zulimde.prefab"
        };

        [MenuItem("Joomak/Kitchen/Install Cooking Effects And Audio")]
        public static void Install()
        {
            AudioClip grill = AssetDatabase.LoadAssetAtPath<AudioClip>(GrillAudioPath);
            AudioClip interaction = AssetDatabase.LoadAssetAtPath<AudioClip>(InteractionAudioPath);
            if (grill == null || interaction == null)
            {
                Debug.LogError("[CookingEffectsInstaller] 추가된 오디오 파일을 찾지 못했습니다.");
                return;
            }

            foreach (string path in CookingPrefabPaths)
            {
                InstallCookingPrefab(path, grill, interaction);
            }

            InstallAudioPrefab("Assets/003_Prefabs/Structure/SupplyBox.prefab", interaction, "storeSfx", "takeSfx");
            InstallAudioPrefab("Assets/003_Prefabs/Structure/jar.prefab", interaction, "storeSfx", "takeSfx");
            InstallAudioPrefab("Assets/003_Prefabs/Structure/unpacker_0.prefab", interaction, "unpackSfx");

            for (int i = 1; i <= 5; i++)
            {
                InstallAudioPrefab($"Assets/003_Prefabs/Customer/customer_{i}.prefab", interaction,
                    "questionSfx", "exclamationSfx");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CookingEffectsInstaller] 조리 파티클과 새 오디오 연결을 완료했습니다.");
        }

        private static void InstallCookingPrefab(string path, AudioClip grill, AudioClip interaction)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CookingStation station = root.GetComponentInChildren<CookingStation>(true);
                if (station == null)
                {
                    Debug.LogWarning($"[CookingEffectsInstaller] CookingStation 없음: {path}");
                    return;
                }

                Transform oldEffect = station.transform.Find("Cooking Particle");
                if (oldEffect != null)
                {
                    Object.DestroyImmediate(oldEffect.gameObject);
                }

                ParticleSystem particle = CreateCookingParticle(station.transform, Path.GetFileNameWithoutExtension(path) == "buncul");
                AudioSource source = station.GetComponent<AudioSource>();
                if (source == null)
                {
                    source = station.gameObject.AddComponent<AudioSource>();
                }
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;

                SerializedObject data = new(station);
                data.FindProperty("cookingParticle").objectReferenceValue = particle;
                data.FindProperty("interactionSfx").objectReferenceValue = interaction;
                data.FindProperty("cookingLoopSfx").objectReferenceValue = grill;
                data.FindProperty("cookingAudioSource").objectReferenceValue = source;
                data.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ParticleSystem CreateCookingParticle(Transform parent, bool sparks)
        {
            GameObject effect = new("Cooking Particle");
            effect.transform.SetParent(parent, false);
            effect.transform.localPosition = new Vector3(0f, 0.55f, -0.15f);

            ParticleSystem particle = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particle.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, 0.62f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.maxParticles = 48;
            main.startColor = sparks
                ? new ParticleSystem.MinMaxGradient(new Color32(255, 119, 35, 220), new Color32(255, 220, 120, 200))
                : new ParticleSystem.MinMaxGradient(new Color32(255, 250, 235, 190), new Color32(190, 205, 210, 145));

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.rateOverTime = sparks ? 14f : 9f;
            ParticleSystem.ShapeModule shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = sparks ? 0.34f : 0.24f;

            ParticleSystem.ColorOverLifetimeModule fade = particle.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.82f, 0.62f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.16f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 120;
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particle;
        }

        private static void InstallAudioPrefab(string path, AudioClip clip, params string[] propertyNames)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                    {
                        continue;
                    }

                    SerializedObject data = new(behaviour);
                    bool changed = false;
                    foreach (string propertyName in propertyNames)
                    {
                        SerializedProperty property = data.FindProperty(propertyName);
                        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            property.objectReferenceValue = clip;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
