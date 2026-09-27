// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Checks, without touching the repository, whether 'cratis ai update' would bring in a newer Cratis AI corpus.
/// </summary>
/// <remarks>
/// The corpus has no version number. 'cratis ai update' clones the default branch of Cratis/AI and records the
/// commit it installed from in .cratis/ai.manifest.json, and 'cratis ai status' reports an update whenever that
/// commit differs from the one the source holds. This check asks GitHub for the default branch's commit - a
/// single short request - and applies the same rule, cached alongside the CLI's own update check.
/// </remarks>
public static class AiUpdateCheck
{
    /// <summary>
    /// The GitHub repository the corpus is downloaded from.
    /// </summary>
    public const string GitHubRepository = "Cratis/AI";

    /// <summary>
    /// The key the latest corpus revision is cached under.
    /// </summary>
    public const string CacheKey = $"github:{GitHubRepository}@HEAD";

    /// <summary>
    /// The environment variable 'cratis ai' reads a local corpus checkout from.
    /// </summary>
    public const string SourceEnvVar = "CRATIS_AI_SOURCE";

    const string ConfigurationPath = ".cratis/ai.json";
    const string ManifestPath = ".cratis/ai.manifest.json";
    const int ShortRevisionLength = 7;
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
    static readonly string[] _manifestCommands = ["install", "update", "uninstall"];

    /// <summary>
    /// Checks whether a newer corpus is available for the Cratis AI installation in a project.
    /// </summary>
    /// <param name="projectPath">The directory 'cratis ai update' would run in.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The update that is available, or null when there is none, nothing is installed, or the check failed.</returns>
    public static async Task<AiCorpusUpdate?> CheckForUpdate(string projectPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var installed = InstalledRevision(projectPath);
            if (!ShouldCheck(installed, Environment.GetEnvironmentVariable(SourceEnvVar)))
            {
                return null;
            }

            var latest = await UpdateChecker.Check(CacheKey, installed!, false, LatestRevision, IsNewer, cancellationToken);
            return latest is null ? null : new AiCorpusUpdate(installed!, latest);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Decides whether the check applies to a command line at all.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>False for the ai commands that rewrite the manifest, true otherwise.</returns>
    /// <remarks>
    /// Install, update and uninstall replace the revision the check compared against while they run, so an
    /// answer read before them would describe an installation that no longer exists.
    /// </remarks>
    public static bool AppliesTo(string[] args) =>
        !(args.Length > 1 &&
          string.Equals(args[0], "ai", StringComparison.OrdinalIgnoreCase) &&
          _manifestCommands.Contains(args[1], StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the hint shown after a command when a newer corpus is available.
    /// </summary>
    /// <param name="update">The available update.</param>
    /// <returns>A user-facing hint message.</returns>
    public static string GetUpdateHint(AiCorpusUpdate update) =>
        $"Cratis AI update available: {Shorten(update.InstalledRevision)} → {Shorten(update.AvailableRevision)} - run 'cratis ai update'";

    /// <summary>
    /// Decides whether a project's installation can be compared against the published corpus at all.
    /// </summary>
    /// <param name="installedRevision">The revision recorded in the manifest, or null when nothing is installed.</param>
    /// <param name="sourceOverride">The value of <see cref="SourceEnvVar"/>, when set.</param>
    /// <returns>True when a check is worth making.</returns>
    /// <remarks>
    /// A local checkout named through <see cref="SourceEnvVar"/> is what 'cratis ai update' installs from, so the
    /// published corpus says nothing about it. A revision that is not a commit came from a folder that is not a
    /// Git checkout, and cannot be compared either.
    /// </remarks>
    internal static bool ShouldCheck(string? installedRevision, string? sourceOverride) =>
        string.IsNullOrEmpty(sourceOverride) && IsCommit(installedRevision);

    /// <summary>
    /// Determines whether the published revision differs from the installed one.
    /// </summary>
    /// <param name="latest">The revision the default branch of the corpus is at.</param>
    /// <param name="installed">The revision recorded as installed.</param>
    /// <returns>True when both are commits and they differ.</returns>
    internal static bool IsNewer(string latest, string installed) =>
        IsCommit(latest) && IsCommit(installed) && !string.Equals(latest, installed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads the revision recorded as installed in a project, without changing anything.
    /// </summary>
    /// <param name="projectPath">The project directory.</param>
    /// <returns>The recorded revision, or null when the project has no Cratis AI installation.</returns>
    internal static string? InstalledRevision(string projectPath)
    {
        var configuration = Path.Combine(projectPath, ConfigurationPath);
        var manifest = Path.Combine(projectPath, ManifestPath);
        if (!File.Exists(configuration) || !File.Exists(manifest))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        return document.RootElement.ValueKind == JsonValueKind.Object &&
               document.RootElement.TryGetProperty(nameof(AiInstallationManifest.SourceRevision), out var revision) &&
               revision.ValueKind == JsonValueKind.String
            ? revision.GetString()
            : null;
    }

    static bool IsCommit(string? revision) =>
        revision is { Length: 40 } && revision.All(char.IsAsciiHexDigit);

    static string Shorten(string revision) => revision[..ShortRevisionLength];

    static async Task<string?> LatestRevision(CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = _timeout };

        // GitHub rejects requests without a user agent; the sha media type answers with the bare commit id.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Cratis.Cli");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.sha");

        var response = await http.GetAsync($"https://api.github.com/repos/{GitHubRepository}/commits/HEAD", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var revision = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        return IsCommit(revision) ? revision : null;
    }
}
