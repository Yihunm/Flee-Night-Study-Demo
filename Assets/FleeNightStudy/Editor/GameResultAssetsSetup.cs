using System.IO;
using UnityEditor;
using UnityEngine;

namespace FleeNightStudy.EditorTools
{
    /// <summary>生成胜负 UI 资源（仅 Resources，供运行时加载）。</summary>
    public static class GameResultAssetsSetup
    {
        const string Res = "Assets/FleeNightStudy/Resources/FleeNightStudy";
        const string VictoryBg = Res + "/VictoryBackground.png";
        const string DefeatBg = Res + "/DefeatBackground.png";
        const string VictoryWav = Res + "/Victory.wav";
        const string DefeatWav = Res + "/Defeat.wav";

        public static void CreateAndBindResultAssetsSilent(GameOverUI ui, GameResultAudio audio)
        {
            if (ui == null || audio == null) return;
            CreateAssetsIfNeeded();
            Bind(ui, audio);
        }

        static void CreateAssetsIfNeeded()
        {
            EnsureFolder(Res);
            if (!File.Exists(VictoryBg)) GenerateVictoryBackground(VictoryBg);
            if (!File.Exists(DefeatBg)) GenerateDefeatBackground(DefeatBg);
            if (!File.Exists(VictoryWav)) GenerateToneWav(VictoryWav, new[] { 523.25f, 659.25f, 783.99f }, 0.18f, 0.22f);
            if (!File.Exists(DefeatWav)) GenerateToneWav(DefeatWav, new[] { 220f, 185f, 147f }, 0.28f, 0.26f);

            AssetDatabase.Refresh();
            ConfigureTextureImporter(VictoryBg);
            ConfigureTextureImporter(DefeatBg);
            ConfigureAudioImporter(VictoryWav);
            ConfigureAudioImporter(DefeatWav);
            AssetDatabase.Refresh();
        }

        static void Bind(GameOverUI ui, GameResultAudio audio)
        {
            var victorySprite = LoadSprite(VictoryBg);
            var defeatSprite = LoadSprite(DefeatBg);
            var victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>(VictoryWav);
            var defeatClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefeatWav);

            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("victoryBackgroundSprite").objectReferenceValue = victorySprite;
            uiSo.FindProperty("defeatBackgroundSprite").objectReferenceValue = defeatSprite;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            ApplySpriteToBackgroundImage(ui, victorySprite, true);
            ApplySpriteToBackgroundImage(ui, defeatSprite, false);

            var audioSo = new SerializedObject(audio);
            audioSo.FindProperty("victoryClip").objectReferenceValue = victoryClip;
            audioSo.FindProperty("defeatClip").objectReferenceValue = defeatClip;
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(audio);
        }

        static void ApplySpriteToBackgroundImage(GameOverUI ui, Sprite sprite, bool victory)
        {
            if (ui == null || sprite == null) return;
            var so = new SerializedObject(ui);
            var prop = victory ? so.FindProperty("victoryBackgroundImage") : so.FindProperty("defeatBackgroundImage");
            var img = prop.objectReferenceValue as UnityEngine.UI.Image;
            if (img == null) return;
            Undo.RecordObject(img, "Apply result background");
            img.sprite = sprite;
            img.color = Color.white;
        }

        static void GenerateVictoryBackground(string path)
        {
            WriteGradientPng(path, new Color(0.05f, 0.12f, 0.22f), new Color(0.15f, 0.55f, 0.35f), true);
        }

        static void GenerateDefeatBackground(string path)
        {
            WriteGradientPng(path, new Color(0.04f, 0.02f, 0.08f), new Color(0.18f, 0.04f, 0.06f), false);
        }

        static void WriteGradientPng(string path, Color bottom, Color top, bool goldGlow)
        {
            const int w = 1920, h = 1080;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var rng = new System.Random(42);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w;
                    var c = Color.Lerp(bottom, top, t);
                    if (goldGlow)
                    {
                        float glow = Mathf.Exp(-Mathf.Pow(u - 0.5f, 2f) * 8f) * Mathf.Exp(-Mathf.Pow(t - 0.55f, 2f) * 6f);
                        c = Color.Lerp(c, new Color(0.95f, 0.82f, 0.35f), glow * 0.45f);
                    }
                    else if (rng.NextDouble() < 0.0025)
                        c = Color.Lerp(c, Color.white, 0.35f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void GenerateToneWav(string path, float[] frequencies, float noteDuration, float gap)
        {
            const int sampleRate = 44100;
            var samples = new System.Collections.Generic.List<float>();
            foreach (float freq in frequencies)
            {
                int noteSamples = Mathf.RoundToInt(sampleRate * noteDuration);
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = i / (float)sampleRate;
                    float env = Mathf.Clamp01(1f - i / (float)noteSamples);
                    env *= env;
                    samples.Add(Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f);
                }
                int gapSamples = Mathf.RoundToInt(sampleRate * gap);
                for (int i = 0; i < gapSamples; i++) samples.Add(0f);
            }
            WriteWav(path, samples.ToArray(), sampleRate);
        }

        static void WriteWav(string path, float[] samples, int sampleRate)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            int byteRate = sampleRate * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(samples.Length * 2);
            foreach (float s in samples)
                writer.Write((short)Mathf.Clamp(s * 32767f, -32767f, 32767f));
            File.WriteAllBytes(path, stream.ToArray());
        }

        static void ConfigureTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        static void ConfigureAudioImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) return;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        static Sprite LoadSprite(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite sp) return sp;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
