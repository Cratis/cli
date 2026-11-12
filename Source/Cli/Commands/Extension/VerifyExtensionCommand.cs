// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Cli.Extensions;
using Cratis.Cli.Extensions.Verification;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Settings for verifying an extension package.
/// </summary>
public class VerifyExtensionSettings : ExtensionSettings
{
    /// <summary>
    /// Gets or sets what to verify.
    /// </summary>
    [CommandArgument(0, "[TARGET]")]
    [Description("A package folder, or a package reference: a catalog short name, an OCI reference or an npm package. Defaults to the current directory.")]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the version range of a reference.
    /// </summary>
    [CommandOption("--range <RANGE>")]
    [Description("The version range of a reference. Defaults to the version screenplay.lock pins, else the latest release.")]
    public string? Range { get; set; }

    /// <summary>
    /// Gets or sets the folder holding the previous release.
    /// </summary>
    [CommandOption("--baseline <FOLDER>")]
    [Description("A folder holding the contract of the release this one replaces. The package must offer everything the baseline did.")]
    public string? Baseline { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether warnings and checks that could not run fail the command.
    /// </summary>
    [CommandOption("--strict")]
    [Description("Fail on warnings and on checks that could not run, not only on errors.")]
    public bool Strict { get; set; }
}

/// <summary>
/// Says whether an extension package is correct, with exact diagnostics.
/// </summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand("verify", "Check an extension package: its contract, manifest, contents, placeholders, digests and compatibility", Branch = typeof(ExtensionBranch))]
[CliExample("extension", "verify")]
[CliExample("extension", "verify", "./Stripe")]
[CliExample("extension", "verify", "ghcr.io/acme/extension-stripe", "--range", "^2")]
[CliExample("extension", "verify", "./Stripe", "--baseline", "./previous", "--strict", "--format", "json")]
[LlmDescription("Checks an extension package folder or a package a registry serves before it is published or loaded. Checks: contract (contract.play compiles with no errors or warnings), manifest (display name, description, icon, requirements; icon and images exist, are png, jpg, webp or svg and within size limits; image captions), contents (everything the contract declares is shipped and nothing else; a package folder lists what it does not ship in .extensionignore), placeholders (static HTML: no script, no event handler attributes, no javascript: URLs, no external references, within a size limit), frontend (Scene conformance kit) and backend (protocol handshake), digests (digest and contract hash against screenplay.lock) and compatibility (requires screenplay against the compiler; --baseline for a drop-in provider). A check that applies but could not run is reported as skipped, never as passed. Exit codes: 0 correct, 5 a check failed (or, with --strict, warned or was skipped), 1 target not found, 2 registry unreachable, 4 credentials refused.")]
[LlmOption("--baseline", "string", "Folder holding the previous release's contract; every ability, trigger, component, property and signal it offered must still be offered.")]
[LlmOption("--strict", "bool", "Fail on warnings and skipped checks.")]
[LlmOption("--range", "string", "Version range of a reference.")]
[LlmOutputAdvice("json", "JSON holds target, passed, and for each check its status and diagnostics (check, code, severity, message, path, line).")]
public class VerifyExtensionCommand : AsyncCommand<VerifyExtensionSettings>
{
    /// <summary>
    /// Gets the exit code for a report.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="strict">Whether warnings and skipped checks fail the report.</param>
    /// <returns>The exit code.</returns>
    public static int ExitCodeOf(ExtensionVerifyReport report, bool strict) =>
        (strict ? report.PassedStrictly : report.Passed) ? ExitCodes.Success : ExitCodes.ValidationError;

    /// <summary>
    /// Writes a report as plain text.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="writer">Where to write.</param>
    public static void Render(ExtensionVerifyReport report, TextWriter writer)
    {
        writer.WriteLine($"Verifying {report.Target}");
        foreach (var check in report.Checks)
        {
            var mark = check.Status switch { VerifyStatus.Passed => "✓", VerifyStatus.Failed => "✗", VerifyStatus.Skipped => "○", _ => "-" };
            var note = check.Status switch { VerifyStatus.Skipped => " (could not run)", VerifyStatus.NotApplicable => " (not applicable)", _ => string.Empty };
            writer.WriteLine($"  {mark} {check.Check}{note}");
            foreach (var diagnostic in check.Diagnostics)
            {
                writer.WriteLine($"      {diagnostic.Code} {diagnostic.Severity.ToString().ToLowerInvariant()} {Where(diagnostic)}{diagnostic.Message}");
            }
        }

        var errors = report.Diagnostics.Count(diagnostic => diagnostic.Severity == VerifySeverity.Error);
        var warnings = report.Diagnostics.Count(diagnostic => diagnostic.Severity == VerifySeverity.Warning);
        writer.WriteLine(report.Passed ? $"Correct ({warnings} warning(s))." : $"Not correct: {errors} error(s), {warnings} warning(s).");
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, VerifyExtensionSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var current = Directory.GetCurrentDirectory();
            var root = Path.GetFullPath(ScreenplayMcpRoot.Resolve(settings.Root, null, null, current, Environment.GetEnvironmentVariable) ?? current);
            var options = new ExtensionVerifyOptions(null, null, LoadBaseline(settings.Baseline), CompilerVersion());
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("cratis-cli");
            var (packages, _) = await OpenPackages(settings, root, http, cancellationToken);
            var verification = new ExtensionPackageVerification(
                new ExtensionVerifier(
                    new UnavailableRuntimeCheck(ExtensionVerifyCodes.FrontendCheck, ExtensionVerifyCodes.FrontendNotRun, "The components were not registered or run against the Scene conformance kit (@cratis/scene.extensions): the CLI does not run frontends yet. Run the kit in the package's own specifications."),
                    new UnavailableRuntimeCheck(ExtensionVerifyCodes.BackendCheck, ExtensionVerifyCodes.BackendNotRun, "The backend was not started and no protocol handshake was made against the contract: the CLI does not run backends yet. Stage performs the handshake when it loads the package.")),
                (reference, range, locked, token) => locked is null
                    ? packages.Resolve(new("verify", new(reference, range, new(0, 0))), null, token)
                    : packages.Fetch(locked, token));

            var report = await Verify(settings, verification, options, root, cancellationToken);
            OutputFormatter.WriteObject(format, new { target = report.Target, passed = settings.Strict ? report.PassedStrictly : report.Passed, checks = report.Checks.Select(check => new { check = check.Check, status = check.Status.ToString().ToLowerInvariant(), diagnostics = check.Diagnostics.Select(diagnostic => new { check = diagnostic.Check, code = diagnostic.Code, severity = diagnostic.Severity.ToString().ToLowerInvariant(), message = diagnostic.Message, path = diagnostic.Path, line = diagnostic.Line }) }) }, _ => Render(report, Console.Out));
            return ExitCodeOf(report, settings.Strict);
        }
        catch (ExtensionFailure failure)
        {
            OutputFormatter.WriteError(format, failure.Message, null, failure.Kind.ToString());
            return ExtensionCommandSupport.ExitCodeOf(new([new(failure.Reference, "failed", Failure: failure.Kind)], [], false));
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or DirectoryNotFoundException)
        {
            OutputFormatter.WriteError(format, exception.Message, null, ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }

    static string Where(VerifyDiagnostic diagnostic)
    {
        if (diagnostic.Path is null) return string.Empty;
        return diagnostic.Line is { } line ? $"{diagnostic.Path}:{line}: " : $"{diagnostic.Path}: ";
    }

    static Task<ExtensionVerifyReport> Verify(VerifyExtensionSettings settings, ExtensionPackageVerification verification, ExtensionVerifyOptions options, string root, CancellationToken cancellationToken)
    {
        var target = settings.Target ?? Directory.GetCurrentDirectory();
        if (Directory.Exists(target)) return verification.VerifyFolder(Path.GetFullPath(target), options, cancellationToken);
        if (File.Exists(target) && Path.GetFileName(target) == ExtensionVerifier.ContractFile) return verification.VerifyFolder(Path.GetDirectoryName(Path.GetFullPath(target))!, options, cancellationToken);
        if (LooksLikeAPath(target)) throw new DirectoryNotFoundException($"'{target}' is not a folder.");

        var locked = LockedEntry(root, target);
        return verification.VerifyReference(target, settings.Range ?? locked?.Range, locked, options, cancellationToken);
    }

    static bool LooksLikeAPath(string target) => target.StartsWith('.') || target.StartsWith('/') || target.StartsWith('~') || (target.Length > 1 && target[1] == ':');

    static ExtensionLockEntry? LockedEntry(string root, string reference)
    {
        var path = Path.Combine(root, ExtensionLock.FileName);
        return File.Exists(path) ? ExtensionLock.Parse(File.ReadAllText(path), path).Value?.Find(reference) : null;
    }

    static ExtensionContract? LoadBaseline(string? folder)
    {
        if (folder is null) return null;
        var root = ExtensionVerifier.FindRoot(folder) ?? throw new DirectoryNotFoundException($"The baseline '{folder}' holds no {ExtensionVerifier.ContractFile}.");
        return ExtensionContractParser.Parse(File.ReadAllText(Path.Combine(root, ExtensionVerifier.ContractFile)), ExtensionVerifier.ContractFile).Value
            ?? throw new InvalidDataException($"The baseline contract in '{folder}' does not compile.");
    }

    static string CompilerVersion()
    {
        var version = typeof(ExtensionContract).Assembly.GetName().Version!;
        return $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }

    static Task<(ExtensionPackages Packages, RegistryHttp Registry)> OpenPackages(ExtensionSettings settings, string root, HttpClient http, CancellationToken cancellationToken) =>
        ExtensionCommandSupport.OpenPackages(settings, root, http, cancellationToken);
}
