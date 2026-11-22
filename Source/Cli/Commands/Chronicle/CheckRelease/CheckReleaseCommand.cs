// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// Checks, read-only, whether a release is compatible with the selected Chronicle target before it is published or deployed.
/// </summary>
[LlmDescription("Checks, read-only, whether a release is compatible with the selected Chronicle server: the kernel version, the wire contracts of the release's own descriptor set, and a validate-only check of its event type registrations. Registers nothing. Prints one cratis.compat/v1 report; exits 0 when compatible, 5 when incompatible, 3 when a check could not be answered, 2 or 4 when the server cannot be reached or refuses the credentials. Use as a gate before publishing or deploying a release.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("check-release", "Check a release is compatible with the Chronicle server without registering anything", Branch = typeof(ChronicleBranch))]
[CliExample("chronicle", "check-release", "--release", "release.json", "--event-store", "Shop")]
[CliExample("chronicle", "check-release", "--release", "release.json", "--event-store", "Shop", "-o", "json")]
[LlmOption("--release <FILE>", "string", "Path of the release description JSON file (required)")]
public class CheckReleaseCommand : ChronicleCommand<CheckReleaseSettings>
{
    const string Unresolved = "unresolved";

    ReleaseDescription? _release;
    string _eventStore = Unresolved;

    /// <inheritdoc/>
    protected override bool UseStatusSpinner => false;

    /// <inheritdoc/>
    protected override int? ExecuteOffline(CheckReleaseSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Release))
        {
            return Invalid(settings, "The --release option is required.");
        }

        if (!ReleaseReader.TryRead(settings.Release, out _release, out var problem))
        {
            return Invalid(settings, problem);
        }

        _eventStore = ResolveEventStore(settings);
        return null;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, CheckReleaseSettings settings, string format)
    {
        var result = await ReleaseChecker.Check(services, _release!, _eventStore, DateTimeOffset.UtcNow);
        ReleaseOutput.Write(format, result);
        return result.ExitCode;
    }

    /// <inheritdoc/>
    protected override void ReportFailure(string format, string error, string? suggestion = null, string? errorCode = null)
    {
        if (_release is null)
        {
            base.ReportFailure(format, error, suggestion, errorCode);
            return;
        }

        ReleaseOutput.Write(format, ReleaseChecker.Unreachable(_release, _eventStore, DateTimeOffset.UtcNow, Summarize(error, errorCode), suggestion));
    }

    static string ResolveEventStore(CheckReleaseSettings settings)
    {
        try
        {
            return settings.ResolveEventStore();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return Unresolved;
        }
    }

    static string Summarize(string error, string? errorCode) => errorCode switch
    {
        ExitCodes.ConnectionErrorCode => "the Chronicle server could not be reached",
        ExitCodes.AuthenticationErrorCode => "the Chronicle server refused or could not accept the credentials",
        _ => error.Length > 300 ? error[..300] : error
    };

    static int Invalid(CheckReleaseSettings settings, string problem)
    {
        OutputFormatter.WriteError(settings.ResolveOutputFormat(), problem, "Describe the release in a JSON file; see the check-release documentation.", ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }
}
