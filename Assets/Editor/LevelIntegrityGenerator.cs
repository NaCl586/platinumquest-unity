#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class LevelIntegrityGenerator
{
    [Serializable]
    private class IntegrityEntry
    {
        public string path;
        public string hash;
    }

    [Serializable]
    private class IntegrityManifest
    {
        public List<IntegrityEntry> files = new List<IntegrityEntry>();
    }

    [MenuItem("Tools/Generate Level Integrity Manifest")]
    public static void Generate()
    {
        string dataFolder = Path.Combine(Application.streamingAssetsPath, "marble", "data");

        if (!Directory.Exists(dataFolder))
        {
            Debug.LogError($"[Integrity] Data folder not found:\n{dataFolder}");

            return;
        }

        string[] files = Directory.GetFiles(dataFolder, "*", SearchOption.AllDirectories);

        IntegrityManifest manifest = new IntegrityManifest();

        foreach (string file in files)
        {
            if (!DataIntegrityManager.IsSupportedFile(file))
                continue;

            string relativePath = Path.GetRelativePath(dataFolder, file).Replace('\\', '/');

            string hash = DataIntegrityManager.CalculateFileHash(file);

            if (string.IsNullOrEmpty(hash))
                continue;

            manifest.files.Add(new IntegrityEntry { path = relativePath, hash = hash });
        }

        manifest.files.Sort(
            (a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase)
        );

        string json = JsonUtility.ToJson(manifest, true);

        string outputPath = Path.Combine(Application.dataPath, "LevelIntegrityManifest.json");

        File.WriteAllText(outputPath, json, Encoding.UTF8);

        AssetDatabase.Refresh();

        Debug.Log(
            $"[Integrity] Generated manifest.\n"
                + $"Files: {manifest.files.Count}\n"
                + $"Output: {outputPath}"
        );
    }
}

#endif
