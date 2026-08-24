// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Cli.Commands.Render.Publication;

internal static class ArtifactPublicationStorage
{
    public const string ControlDirectoryName = ".cratis-render";
    public const string ManifestFileName = ".cratis-render.json";
    public const string JournalFileName = "journal.json";
    public const string StagingDirectoryName = "staging";
    public const string BackupDirectoryName = "backup";

    static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string ManifestPath(string destination) => Path.Combine(destination, ManifestFileName);
    public static string ControlPath(string destination) => Path.Combine(destination, ControlDirectoryName);
    public static string JournalPath(string destination) => Path.Combine(ControlPath(destination), JournalFileName);
    public static string StagingPath(string destination, string relativePath) => PathFor(Path.Combine(ControlPath(destination), StagingDirectoryName), relativePath);
    public static string BackupPath(string destination, string relativePath) => PathFor(Path.Combine(ControlPath(destination), BackupDirectoryName), relativePath);
    public static string ArtifactPath(string destination, string relativePath) => PathFor(destination, relativePath);

    public static string? ReadManifestJson(string destination) =>
        File.Exists(ManifestPath(destination)) ? File.ReadAllText(ManifestPath(destination)) : null;

    public static ArtifactManifest? ReadManifest(string destination) =>
        ReadManifestJson(destination) is { } json ? Deserialize<ArtifactManifest>(json, "ownership manifest") : null;

    public static ArtifactPublicationJournal? ReadJournal(string destination) =>
        File.Exists(JournalPath(destination))
            ? Deserialize<ArtifactPublicationJournal>(File.ReadAllText(JournalPath(destination)), "publication journal")
            : null;

    public static string Serialize<T>(T value) => $"{JsonSerializer.Serialize(value, _jsonOptions)}\n";

    public static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public static void WriteDurable(string path, string content) =>
        WriteDurable(path, new UTF8Encoding(false).GetBytes(content));

    public static void WriteDurable(string path, ReadOnlySpan<byte> bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = $"{path}.new";
        using (var stream = new FileStream(
                   temporary,
                   FileMode.Create,
                   FileAccess.Write,
                   FileShare.None,
                   4096,
                   FileOptions.WriteThrough))
        {
            stream.Write(bytes);
            stream.Flush(true);
        }

        File.Move(temporary, path, true);
    }

    public static void CopyAtomic(string source, string destination) =>
        WriteDurable(destination, File.ReadAllBytes(source));

    public static void Cleanup(string destination)
    {
        var control = ControlPath(destination);
        if (Directory.Exists(control))
        {
            Directory.Delete(control, true);
        }
    }

    public static void EnsureSafePath(string destination, string relativePath)
    {
        var root = Path.GetFullPath(destination);
        var resolved = ArtifactPath(root, relativePath);
        if (!resolved.StartsWith($"{root}{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new UnsafeArtifactPublication($"Artifact path '{relativePath}' escapes the destination.");
        }

        EnsureNotLink(root);
        var current = root;
        foreach (var segment in relativePath.Replace('\\', '/').Split('/'))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) || Directory.Exists(current))
            {
                EnsureNotLink(current);
            }
        }
    }

    static string PathFor(string root, string relativePath) =>
        Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    static T Deserialize<T>(string json, string subject)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ??
                throw new UnsafeArtifactPublication($"The {subject} is empty.");
        }
        catch (JsonException exception)
        {
            throw new UnsafeArtifactPublication($"The {subject} is malformed: {exception.Message}");
        }
    }

    static void EnsureNotLink(string path)
    {
        if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new UnsafeArtifactPublication($"Publication path '{path}' is a symbolic link or reparse point.");
        }
    }
}
