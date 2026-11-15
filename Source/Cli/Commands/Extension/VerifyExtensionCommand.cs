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
[CliExample("extension", "verify", "./Stripe", "--baseline", "./previous", "--strict", "-o", "json")]
[LlmDescription("Checks an extension package folder or a package a registry serves before it is published or loaded. Checks: contract (contract.play compiles with no errors or warnings), manifest (display name, description, icon, requirements; icon and images exist, are png, jpg, webp or svg and within size limits; image captions), contents (everything the contract declares is shipped and nothing else; a package folder lists what it does not ship in .extensionignore), placeholders (static HTML: no script, no event handler attributes, no javascript: URLs, no external references, within a size limit), frontend (Scene conformance kit) and backend (protocol handshake), digests (digest and contract hash against screenplay.lock) and compatibility (requires screenplay against the compiler; --baseline for a drop-in provider). A check that applies but could not run is reported as skipped, never as passed. Exit codes: 0 correct, 5 a check failed (or, with --strict, warned or was skipped) or the target folder does not exist, 1 reference could not be resolved, 2 registry unreachable, 4 credentials refused.")]
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
            WriteCheck(check, writer);
        }

        writer.WriteLine(Verdict(report));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, VerifyExtensionSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var current = Directory.GetCurrentDirectory();
            var root = Path.GetFullPath(ScreenplayMcpRoot.Resolve(settings.Root, null, null, current, Environment.GetEnvironmentVariable) ?? current);
            var request = new VerifyRequest(settings.Target ?? current, settings.Range, root, new ExtensionVerifyOptions(null, null, LoadBaseline(settings.Baseline), CompilerVersion()));
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("cratis-cli");
            var (packages, _) = await OpenPackages(settings, root, http, cancellationToken);
            var report = await Verify(request, CreateVerification(packages), cancellationToken);
            OutputFormatter.WriteObject(format, Describe(report, settings.Strict), _ => Render(report, Console.Out));
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

    /// <summary>
    /// Describes a report as the object the JSON output holds.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="strict">Whether warnings and skipped checks fail the report.</param>
    /// <returns>The object to write.</returns>
    internal static object Describe(ExtensionVerifyReport report, bool strict) => new
    {
        target = report.Target,
        passed = strict ? report.PassedStrictly : report.Passed,
        checks = report.Checks.Select(check => new
        {
            check = check.Check,
            status = Lower(check.Status),
            diagnostics = check.Diagnostics.Select(DescribeDiagnostic)
        })
    };

    static object DescribeDiagnostic(VerifyDiagnostic diagnostic) => new
    {
        check = diagnostic.Check,
        code = diagnostic.Code,
        severity = Lower(diagnostic.Severity),
        message = diagnostic.Message,
        path = diagnostic.Path,
        line = diagnostic.Line
    };

    static string Lower(Enum value) => value.ToString().ToLowerInvariant();

    static void WriteCheck(VerifyCheckResult check, TextWriter writer)
    {
        writer.WriteLine($"  {Mark(check.Status)} {check.Check}{Note(check.Status)}");
        foreach (var diagnostic in check.Diagnostics)
        {
            writer.WriteLine($"      {diagnostic.Code} {Lower(diagnostic.Severity)} {Where(diagnostic)}{diagnostic.Message}");
        }
    }

    static string Mark(VerifyStatus status) => status switch
    {
        VerifyStatus.Passed => "✓",
        VerifyStatus.Failed => "✗",
        VerifyStatus.Skipped => "○",
        _ => "-"
    };

    static string Note(VerifyStatus status) => status switch
    {
        VerifyStatus.Skipped => " (could not run)",
        VerifyStatus.NotApplicable => " (not applicable)",
        _ => string.Empty
    };

    static string Verdict(ExtensionVerifyReport report)
    {
        var errors = report.Diagnostics.Count(diagnostic => diagnostic.Severity == VerifySeverity.Error);
        var warnings = report.Diagnostics.Count(diagnostic => diagnostic.Severity == VerifySeverity.Warning);
        return report.Passed ? $"Correct ({warnings} warning(s))." : $"Not correct: {errors} error(s), {warnings} warning(s).";
    }

    static string Where(VerifyDiagnostic diagnostic)
    {
        if (diagnostic.Path is null) return string.Empty;
        return diagnostic.Line is { } line ? $"{diagnostic.Path}:{line}: " : $"{diagnostic.Path}: ";
    }

    static ExtensionPackageVerification CreateVerification(ExtensionPackages packages) => new(
        RuntimeChecks.CreateVerifier(),
        (reference, range, locked, token) => locked is null
            ? packages.Resolve(new("verify", new(reference, range, new(0, 0))), null, token)
            : packages.Fetch(locked, token));

    static Task<ExtensionVerifyReport> Verify(VerifyRequest request, ExtensionPackageVerification verification, CancellationToken cancellationToken)
    {
        var target = request.Target;
        if (Directory.Exists(target)) return verification.VerifyFolder(Path.GetFullPath(target), request.Options, cancellationToken);
        if (File.Exists(target) && Path.GetFileName(target) == ExtensionVerifier.ContractFile) return verification.VerifyFolder(Path.GetDirectoryName(Path.GetFullPath(target))!, request.Options, cancellationToken);
        if (LooksLikeAPath(target)) throw new DirectoryNotFoundException($"'{target}' is not a folder.");

        var locked = LockedEntry(request.Root, target);
        return verification.VerifyReference(target, request.Range ?? locked?.Range, locked, request.Options, cancellationToken);
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

    sealed record VerifyRequest(string Target, string? Range, string Root, ExtensionVerifyOptions Options);
}
