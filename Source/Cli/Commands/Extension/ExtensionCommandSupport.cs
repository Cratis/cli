// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Environments;
using Cratis.Cli.Extensions;
using Cratis.Environments;
using Cratis.Screenplay.Extensions;
using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Builds the extension service from command settings and turns its report into output and an exit code.
/// </summary>
public static class ExtensionCommandSupport
{
    /// <summary>
    /// Gets the exit code for a report: success, or the code of the most serious failure.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <returns>The exit code.</returns>
    public static int ExitCodeOf(ExtensionReport report)
    {
        var kinds = report.Outcomes.Where(outcome => outcome.Failure is not null).Select(outcome => outcome.Failure!.Value).ToArray();
        if (kinds.Length == 0) return ExitCodes.Success;
        if (kinds.Contains(ExtensionFailureKind.Unauthorized)) return ExitCodes.AuthenticationError;
        if (kinds.Contains(ExtensionFailureKind.Unreachable)) return ExitCodes.ConnectionError;
        return kinds.All(kind => kind == ExtensionFailureKind.Unresolvable) ? ExitCodes.NotFound : ExitCodes.ValidationError;
    }

    /// <summary>
    /// Runs an extension operation and writes its report.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <param name="operation">The operation.</param>
    /// <param name="render">Writes the report as plain text.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> Run(ExtensionSettings settings, Func<ExtensionRuntime, CancellationToken, Task<ExtensionReport>> operation, Action<ExtensionReport> render, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        try
        {
            var current = Directory.GetCurrentDirectory();
            var root = ScreenplayMcpRoot.Resolve(settings.Root, null, null, current, Environment.GetEnvironmentVariable) ?? current;
            var workspace = ExtensionWorkspace.Load(root);
            var environment = new Lazy<ProjectEnvironment?>(() => OpenEnvironment(settings));
            var credentials = new ConfiguredRegistryCredentials(async (key, token) => environment.Value is { } open && open.GetEntry(key) is not null ? await open.GetValueAsync(key, token) : null);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("cratis-cli");
            var registry = new RegistryHttp(http, credentials);
            var npm = environment.Value is { } configured && configured.GetEntry("extensions.npm.registry") is not null ? await configured.GetValueAsync("extensions.npm.registry", cancellationToken) : null;
            var origin = new RoutingExtensionRegistry(new OciExtensionRegistry(registry), new NpmExtensionRegistry(registry, npm ?? NpmExtensionRegistry.DefaultRegistry));
            var trust = new ExtensionTrust(ExtensionAllowList.Load(workspace.ProjectRoot), workspace.ProjectRoot, new ConsolePublisherPrompt());
            var packages = new ExtensionPackages(origin, OpenSource(settings.Source, registry), new VendoredExtensionStore(workspace.VendorFolder), trust);
            var report = await operation(new(workspace, new ExtensionService(packages, ExtensionCache.Default), registry), cancellationToken);
            OutputFormatter.WriteObject(format, report, render);
            return ExitCodeOf(report);
        }
        catch (ExtensionFailure failure)
        {
            OutputFormatter.WriteError(format, failure.Message, null, failure.Kind.ToString());
            return ExitCodeOf(new([new(failure.Reference, "failed", Failure: failure.Kind)], [], false));
        }
        catch (InvalidPassphraseException exception)
        {
            OutputFormatter.WriteError(format, exception.Message, null, ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or SecretNotAvailableException or EnvironmentException)
        {
            OutputFormatter.WriteError(format, exception.Message, null, ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }

    /// <summary>
    /// Opens a package source from its option value.
    /// </summary>
    /// <param name="value">A folder, or <c>oci://host/prefix</c>.</param>
    /// <param name="http">The registry client.</param>
    /// <returns>The source, or <see langword="null"/> when none was named.</returns>
    public static IExtensionRegistry? OpenSource(string? value, RegistryHttp http) =>
        string.IsNullOrWhiteSpace(value) ? null : value.StartsWith("oci://", StringComparison.Ordinal) ? Mirror(value, http) : new VendoredExtensionStore(Path.GetFullPath(value));

    /// <summary>
    /// Opens a destination from its argument value.
    /// </summary>
    /// <param name="value">A folder, or <c>oci://host/prefix</c>.</param>
    /// <param name="http">The registry client.</param>
    /// <returns>The destination.</returns>
    public static IExtensionDestination OpenDestination(string value, RegistryHttp http) =>
        value.StartsWith("oci://", StringComparison.Ordinal) ? Mirror(value, http) : new VendoredExtensionStore(Path.GetFullPath(value));

    /// <summary>
    /// Writes one outcome line.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    public static void WriteOutcome(ExtensionOutcome outcome)
    {
        var detail = outcome.Failure is null ? $"{outcome.Version} {outcome.Digest}".Trim() : $"{outcome.Failure}: {outcome.Message}";
        Console.WriteLine($"{outcome.Status,-10} {outcome.Reference} {detail}".TrimEnd());
    }

    static OciExtensionRegistry Mirror(string value, RegistryHttp http)
    {
        var uri = new Uri(value);
        return new(http, uri.AbsolutePath.Trim('/'), uri.Authority);
    }

    static ProjectEnvironment? OpenEnvironment(ExtensionSettings settings)
    {
        try
        {
            return EnvironmentCommandSupport.Open(settings, EnvironmentCommandSupport.Project());
        }
        catch (EnvironmentNotFoundException)
        {
            return null;
        }
    }
}
