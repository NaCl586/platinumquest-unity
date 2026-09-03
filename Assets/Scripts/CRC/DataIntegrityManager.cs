using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Server.DTOs.Responses;
using UnityEngine;

public static class DataIntegrityManager
{
    public static bool IsSupportedFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return extension.Equals(".mis", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".dif", StringComparison.OrdinalIgnoreCase);
    }

    // --------------------------------------------------
    // MISSION FILES
    // --------------------------------------------------

    public static List<string> GetMissionIntegrityFiles(string missionPath)
    {
        List<string> files = new List<string>();

        string fullMissionPath = ResolveMissionPath(missionPath);

        if (!File.Exists(fullMissionPath))
        {
            Debug.LogError($"[Integrity] Mission file does not exist:\n" + $"{fullMissionPath}");

            return files;
        }

        // Always verify the .mis itself.
        files.Add(NormalizeIntegrityPath(fullMissionPath));

        string missionText = File.ReadAllText(fullMissionPath);

        List<string> interiorPaths = FindReferencedInteriorFiles(missionText);

        foreach (string interiorPath in interiorPaths)
        {
            string fullInteriorPath = ResolveDataPath(interiorPath);

            files.Add(NormalizeIntegrityPath(fullInteriorPath));
        }

        return files;
    }

    // --------------------------------------------------
    // SERVER HASH VERIFICATION
    // --------------------------------------------------

    public static List<string> VerifyAgainstServer(IntegrityResponse response)
    {
        List<string> invalidFiles = new List<string>();

        if (response == null || response.Files == null)
        {
            Debug.LogError("[Integrity] Server returned an invalid integrity response.");

            invalidFiles.Add("Integrity response unavailable");

            return invalidFiles;
        }

        foreach (IntegrityFileResponse file in response.Files)
        {
            string fullPath = ResolveDataPath(file.Path);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[Integrity] Protected file is missing:\n" + $"{file.Path}");

                invalidFiles.Add(file.Path);

                continue;
            }

            string actualHash = CalculateFileHash(fullPath);

            if (!string.Equals(actualHash, file.Hash, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError(
                    $"[Integrity] PROTECTED FILE MODIFIED!\n"
                        + $"File: {file.Path}\n"
                        + $"Expected SHA-256: {file.Hash}\n"
                        + $"Actual SHA-256:   {actualHash}"
                );

                invalidFiles.Add(file.Path);
            }
        }

        return invalidFiles;
    }

    // --------------------------------------------------
    // .MIS PARSER
    // --------------------------------------------------

    private static List<string> FindReferencedInteriorFiles(string missionText)
    {
        List<string> result = new List<string>();

        Regex regex = new Regex(
            @"(?:interiorFile|interiorResource)\s*=\s*""([^""]+\.dif)""",
            RegexOptions.IgnoreCase
        );

        MatchCollection matches = regex.Matches(missionText);

        foreach (Match match in matches)
        {
            string path = match.Groups[1].Value;

            path = NormalizeInteriorPath(path);

            // IMPORTANT:
            // Do NOT remove duplicates.
            // Every reference must be checked.
            result.Add(path);
        }

        return result;
    }

    // --------------------------------------------------
    // PATH RESOLUTION
    // --------------------------------------------------

    private static string NormalizeInteriorPath(string path)
    {
        path = path.Replace('\\', '/');

        if (path.StartsWith("~/data/", StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring("~/data/".Length);
        }

        if (path.StartsWith("marble/data/", StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring("marble/data/".Length);
        }

        return path.TrimStart('/');
    }

    private static string ResolveMissionPath(string missionPath)
    {
        missionPath = missionPath.Replace('\\', '/');

        if (missionPath.StartsWith("marble/data/", StringComparison.OrdinalIgnoreCase))
        {
            missionPath = missionPath.Substring("marble/data/".Length);
        }

        return ResolveDataPath(missionPath);
    }

    private static string ResolveDataPath(string relativePath)
    {
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);

        return Path.Combine(Application.streamingAssetsPath, "marble", "data", relativePath);
    }

    private static string NormalizeIntegrityPath(string filePath)
    {
        string dataFolder = Path.Combine(Application.streamingAssetsPath, "marble", "data");

        string relativePath = Path.GetRelativePath(dataFolder, filePath);

        return relativePath.Replace('\\', '/');
    }

    // --------------------------------------------------
    // SHA-256
    // --------------------------------------------------

    public static string CalculateFileHash(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[Integrity] File does not exist:\n" + $"{filePath}");

            return string.Empty;
        }

        using (SHA256 sha256 = SHA256.Create())
        using (FileStream stream = File.OpenRead(filePath))
        {
            byte[] hash = sha256.ComputeHash(stream);

            return BytesToHex(hash);
        }
    }

    private static string BytesToHex(byte[] bytes)
    {
        StringBuilder builder = new StringBuilder(bytes.Length * 2);

        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}
