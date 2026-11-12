// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Cli.Extensions;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// What the Stage container is given so it can load, trust and configure the extensions of a model: the settings, the secrets, the allow-list with the
/// keys of its publishers, the registry credentials and a package cache that survives the run. Stage fetches and verifies the packages itself, against the
/// digests in the lock, and starts each backend in its tier when it is first needed.
/// </summary>
/// <param name="Environment">Public configuration, passed with <c language="text">-e</c>.</param>
/// <param name="Secrets">Secret configuration: setting values and registry credentials. Never put on a command line, never logged.</param>
/// <param name="AllowListJson">The allow-list in the shape Stage reads from <c language="text">.cratis/extensions.json</c>.</param>
/// <param name="PackageCache">The host folder Stage keeps downloaded packages in, by digest.</param>
/// <param name="ReplacesModelProjectFile">Whether the model folder already holds a <c language="text">.cratis/extensions.json</c> the generated one must replace inside the container.</param>
public sealed record StageExtensions(
    IReadOnlyList<KeyValuePair<string, string>> Environment,
    IReadOnlyList<KeyValuePair<string, string>> Secrets,
    string AllowListJson,
    string PackageCache,
    bool ReplacesModelProjectFile)
{
    /// <summary>
    /// The port Stage's extension host services listen on inside the container. Setting it turns the extension protocol on.
    /// </summary>
    public const string HostPort = "50100";

    /// <summary>
    /// Where the package cache is mounted in the container.
    /// </summary>
    public const string ContainerCache = "/stage-extension-cache";

    /// <summary>
    /// The prefix of the environment keys that hold registry credentials: <c language="text">extensions.registry.&lt;host&gt;.username|password|token</c>.
    /// </summary>
    public const string RegistryPrefix = "extensions.registry.";

    /// <summary>
    /// The environment key of the npm registry.
    /// </summary>
    public const string NpmRegistryKey = "extensions.npm.registry";

    const string PackagesSection = "Stage__Extensions__Packages__";

    /// <summary>
    /// Plans what to give Stage.
    /// </summary>
    /// <param name="values">The resolved settings and secrets of the extension systems.</param>
    /// <param name="allowList">The project's allow-list.</param>
    /// <param name="keys">The signing keys of the locked packages' publishers.</param>
    /// <param name="environment">The environment's values by key, of which only registry credentials and the npm registry are read.</param>
    /// <param name="packageCache">The host folder for downloaded packages.</param>
    /// <param name="replacesModelProjectFile">Whether the model folder holds a project file the generated one replaces.</param>
    /// <returns>The plan.</returns>
    public static StageExtensions Create(
        RunExtensionValues values,
        ExtensionAllowList allowList,
        IReadOnlyDictionary<string, IReadOnlyList<string>> keys,
        IReadOnlyDictionary<string, string> environment,
        string packageCache,
        bool replacesModelProjectFile)
    {
        List<KeyValuePair<string, string>> publicValues =
        [
            new("Stage__Extensions__HostPort", HostPort),
            new($"{PackagesSection}CacheDirectory", ContainerCache),
            .. values.Settings
        ];
        List<KeyValuePair<string, string>> secrets = [.. values.Secrets];
        if (environment.TryGetValue(NpmRegistryKey, out var npm)) publicValues.Add(new($"{PackagesSection}NpmRegistry", npm));
        secrets.AddRange(RegistryCredentials(environment));
        return new(publicValues, secrets, AllowListDocument(allowList, keys), packageCache, replacesModelProjectFile);
    }

    /// <summary>
    /// Renders the secrets as a Docker environment file, one <c language="text">NAME=value</c> line each.
    /// </summary>
    /// <returns>The file text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a value spans lines, which an environment file cannot hold. Only the name is reported.</exception>
    public string RenderSecretsFile()
    {
        var builder = new StringBuilder();
        foreach (var (name, value) in Secrets)
        {
            if (value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The value of '{name}' spans several lines, which cannot be handed to the Stage as an environment variable.");
            }

            builder.Append(name).Append('=').Append(value).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds the <c language="text">docker run</c> arguments. The secrets file is the only place a secret appears.
    /// </summary>
    /// <param name="files">The files written by <see cref="Write"/>.</param>
    /// <param name="modelMountPath">Where the model is mounted in the container.</param>
    /// <returns>The arguments.</returns>
    public IReadOnlyList<string> BuildArguments(StageExtensionFiles files, string modelMountPath)
    {
        var (secretsFile, allowListFile) = (files.SecretsFile, files.AllowListFile);
        List<string> arguments = [.. Environment.SelectMany(pair => new[] { "-e", $"{pair.Key}={pair.Value}" })];
        if (Secrets.Count > 0) arguments.AddRange(["--env-file", secretsFile]);
        arguments.AddRange(new ContainerMount(PackageCache, ContainerCache, ReadOnly: false).BuildArguments());
        arguments.AddRange(new ContainerMount(allowListFile, "/.cratis/extensions.json").BuildArguments());
        if (ReplacesModelProjectFile) arguments.AddRange(new ContainerMount(allowListFile, $"{modelMountPath}/.cratis/extensions.json").BuildArguments());
        return arguments;
    }

    /// <summary>
    /// Writes the files the arguments name into a directory only the user can read.
    /// </summary>
    /// <param name="directory">The run directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The files, to hand to <see cref="BuildArguments"/>.</returns>
    public async Task<StageExtensionFiles> Write(string directory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(PackageCache);
        var files = new StageExtensionFiles(Path.Combine(directory, "stage-extension-secrets.env"), Path.Combine(directory, "extensions.json"));
        await WriteOwnerOnly(files.SecretsFile, RenderSecretsFile(), cancellationToken);
        await WriteOwnerOnly(files.AllowListFile, AllowListJson, cancellationToken);
        return files;
    }

    static async Task WriteOwnerOnly(string path, string text, CancellationToken cancellationToken)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        await using var stream = new FileStream(path, options);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(text), cancellationToken);
    }

    static IEnumerable<KeyValuePair<string, string>> RegistryCredentials(IReadOnlyDictionary<string, string> environment)
    {
        foreach (var (key, value) in environment.Where(pair => pair.Key.StartsWith(RegistryPrefix, StringComparison.Ordinal)).OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var rest = key[RegistryPrefix.Length..];
            var dot = rest.LastIndexOf('.');
            if (dot <= 0) continue;
            var host = rest[..dot];
            var field = rest[(dot + 1)..];
            var setting = field switch
            {
                "username" => "Username",
                "password" or "token" => "Password",
                _ => null
            };
            if (setting is not null) yield return new($"{PackagesSection}Registries__{host}__{setting}", value);
        }
    }

    static string AllowListDocument(ExtensionAllowList allowList, IReadOnlyDictionary<string, IReadOnlyList<string>> keys)
    {
        var allow = new JsonObject { ["publishers"] = new JsonArray([.. allowList.Publishers.Select(entry => (JsonNode)JsonValue.Create(entry.Publisher)!)]) };
        var restricted = allowList.Publishers.All(entry => entry.References is { Count: > 0 });
        if (restricted && allowList.Publishers.Count > 0)
        {
            var references = allowList.Publishers.SelectMany(entry => entry.References!).Distinct(StringComparer.Ordinal);
            allow["references"] = new JsonArray([.. references.Select(reference => (JsonNode)JsonValue.Create(reference)!)]);
        }

        var publishers = new JsonObject();
        foreach (var (publisher, publisherKeys) in keys.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            publishers[publisher] = new JsonObject { ["keys"] = new JsonArray([.. publisherKeys.Select(key => (JsonNode)JsonValue.Create(key)!)]) };
        }

        return new JsonObject { ["allow"] = allow, ["publishers"] = publishers }.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }
}

/// <summary>
/// The files written for the Stage's extensions.
/// </summary>
/// <param name="SecretsFile">The Docker environment file holding the secrets. Readable by the user only; deleted with the run directory.</param>
/// <param name="AllowListFile">The allow-list in Stage's shape.</param>
public sealed record StageExtensionFiles(string SecretsFile, string AllowListFile);
