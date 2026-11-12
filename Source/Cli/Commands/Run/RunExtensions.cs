// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Environments;
using Cratis.Cli.Commands.Extension;
using Cratis.Cli.Extensions;
using Cratis.Environments;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The result of admitting a model's extensions before a run.
/// </summary>
/// <param name="ExitCode">Success, or why nothing may start.</param>
/// <param name="Workspace">The model's workspace, or null when it names no extension.</param>
/// <param name="Keys">The signing keys of the publishers of the locked packages.</param>
public record RunExtensionAdmitted(int ExitCode, ExtensionWorkspace? Workspace, IReadOnlyDictionary<string, IReadOnlyList<string>> Keys)
{
    /// <summary>
    /// Gets the result for a model without extensions.
    /// </summary>
    public static RunExtensionAdmitted None { get; } = new(ExitCodes.Success, null, new Dictionary<string, IReadOnlyList<string>>());
}

/// <summary>
/// Brings a model's extensions up for <c language="text">cratis run</c>: decides whether the deployment allows the packages the lock pins, fills the
/// digest-keyed cache from the lock, and plans what the Stage is given. A range is never resolved here; that is <c language="text">cratis extension restore</c>.
/// </summary>
public static class RunExtensions
{
    /// <summary>
    /// Gets the host folder the Stage keeps downloaded packages in, by digest, so a package is pulled once across runs.
    /// </summary>
    public static string PackageCache => Path.Combine(Path.GetDirectoryName(ExtensionCache.Default.Root.TrimEnd(Path.DirectorySeparatorChar))!, "packages");

    /// <summary>
    /// Gets whether a model names an extension.
    /// </summary>
    /// <param name="application">The compiled model.</param>
    /// <returns><see langword="true"/> when a system is realized by an extension.</returns>
    public static bool Names(ApplicationSyntax? application) => application?.Systems.Any(system => system.Extension is not null) == true;

    /// <summary>
    /// Refuses a model whose extension is not pinned or not allowed, then makes sure the cache holds what the lock pins.
    /// </summary>
    /// <param name="settings">The run settings.</param>
    /// <param name="modelFolder">The folder the model is read from.</param>
    /// <param name="folderTarget">Whether the run target is a folder; a single file cannot carry the lock the Stage reads packages from.</param>
    /// <param name="format">The output format.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Whether the run may go on.</returns>
    public static async Task<RunExtensionAdmitted> Admit(RunSettings settings, string modelFolder, bool folderTarget, string format, CancellationToken cancellationToken)
    {
        ExtensionWorkspace workspace;
        try
        {
            workspace = ExtensionWorkspace.Load(modelFolder);
        }
        catch (InvalidDataException)
        {
            // A model or lock that cannot be read is reported, with its diagnostics, by the compile that follows.
            return RunExtensionAdmitted.None;
        }

        if (workspace.References.Count == 0) return RunExtensionAdmitted.None;
        if (!folderTarget)
        {
            return Refused(format, [.. workspace.References.DistinctBy(reference => reference.Reference).Select(reference => new RunExtensionRefusal(reference.Reference, ExtensionFailureKind.NotLocked, $"A single file cannot carry {ExtensionLock.FileName}, which the Stage reads the package from. Run the folder that holds the model and the lock."))], ExitCodes.ValidationError);
        }

        try
        {
            var refusals = RunExtensionAdmission.Check(workspace.References, workspace.Lock, ExtensionAllowList.Load(workspace.ProjectRoot));
            if (refusals.Count > 0) return Refused(format, refusals, ExitCodes.ValidationError);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("cratis-cli");
            var extensionSettings = new ExtensionSettings { Root = workspace.Root, EnvironmentShort = settings.EnvironmentShort, SelectedEnvironmentName = settings.SelectedEnvironmentName };
            var (packages, _) = await ExtensionCommandSupport.OpenPackages(extensionSettings, workspace.Root, http, cancellationToken);
            var service = new ExtensionService(packages, ExtensionCache.Default);
            var report = await service.Restore(workspace, true, cancellationToken);
            if (!report.Succeeded)
            {
                var failed = report.Outcomes.Where(outcome => outcome.Failure is not null).Select(outcome => new RunExtensionRefusal(outcome.Reference, outcome.Failure!.Value, outcome.Message ?? outcome.Failure.ToString()!)).ToArray();
                return Refused(format, failed, ExtensionCommandSupport.ExitCodeOf(report));
            }

            return new(ExitCodes.Success, workspace, await service.PublisherKeys(workspace, cancellationToken));
        }
        catch (ExtensionFailure failure)
        {
            return Refused(format, [new(failure.Reference, failure.Kind, failure.Message)], ExtensionCommandSupport.ExitCodeOf(new([new(failure.Reference, "failed", Failure: failure.Kind)], [], false)));
        }
        catch (InvalidPassphraseException exception)
        {
            OutputFormatter.WriteError(format, exception.Message, null, ExitCodes.AuthenticationErrorCode);
            return new(ExitCodes.AuthenticationError, null, RunExtensionAdmitted.None.Keys);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or SecretNotAvailableException or EnvironmentException)
        {
            OutputFormatter.WriteError(format, exception.Message, null, ExitCodes.ValidationErrorCode);
            return new(ExitCodes.ValidationError, null, RunExtensionAdmitted.None.Keys);
        }
    }

    /// <summary>
    /// Plans what the Stage is given for the extensions of a model.
    /// </summary>
    /// <param name="requirements">What the model needs, with the settings of its extensions.</param>
    /// <param name="environment">The resolved environment.</param>
    /// <param name="admitted">The admitted extensions.</param>
    /// <param name="modelFolder">The folder the model is read from.</param>
    /// <param name="folderTarget">Whether the target is a folder rather than one file.</param>
    /// <returns>The plan, or the keys that have no value.</returns>
    public static (StageExtensions? Plan, IReadOnlyList<string> Missing) Plan(ModelRequirements requirements, RunEnvironment environment, RunExtensionAdmitted admitted, string modelFolder, bool folderTarget)
    {
        var values = RunExtensionValues.Resolve(requirements.Application, requirements.Requirements, environment.Values);
        if (values.Missing.Count > 0) return (null, values.Missing);

        var replaces = folderTarget && File.Exists(ExtensionAllowList.PathFor(modelFolder));
        return (StageExtensions.Create(values, ExtensionAllowList.Load(admitted.Workspace!.ProjectRoot), admitted.Keys, environment.Values, PackageCache, replaces), []);
    }

    static RunExtensionAdmitted Refused(string format, IReadOnlyList<RunExtensionRefusal> refusals, int exitCode)
    {
        var lines = string.Join(Environment.NewLine, refusals.Select(refusal => $"  {refusal.Reference} ({refusal.Kind}): {refusal.Message}"));
        OutputFormatter.WriteError(
            format,
            $"Nothing was started - {refusals.Count} extension(s) may not run:{Environment.NewLine}{lines}",
            "Pin and allow them with 'cratis extension restore', then commit screenplay.lock and .cratis/extensions.json",
            exitCode == ExitCodes.ValidationError ? ExitCodes.ValidationErrorCode : exitCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new(exitCode, null, RunExtensionAdmitted.None.Keys);
    }
}
