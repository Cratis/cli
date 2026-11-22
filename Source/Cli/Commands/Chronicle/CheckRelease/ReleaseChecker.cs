// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Cli.Commands.ReleaseNotes;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// Checks a release against a Chronicle target using only read-only calls: the server version, the wire compatibility check
/// and the validate-only event type registration check. It registers nothing and repairs nothing.
/// </summary>
public static class ReleaseChecker
{
    const string SchemaRemedy = "Make the schema backward compatible, add a generation with a migration, or follow the reviewed schema registry process; do not disable schema validation.";

    /// <summary>
    /// Checks a release.
    /// </summary>
    /// <param name="services">The Chronicle services of the target.</param>
    /// <param name="release">The release description.</param>
    /// <param name="eventStore">The event store the registrations are checked against.</param>
    /// <param name="readAt">When the target is read.</param>
    /// <returns>The compatibility result, with exactly the three checks in a stable order.</returns>
    /// <remarks>
    /// A server that does not offer an operation, or that refuses it, yields an unknown check. Connection and authentication failures are
    /// left to the caller, which owns the credential refresh and the exit code mapping.
    /// </remarks>
    public static async Task<CompatibilityResult> Check(IServices services, ReleaseDescription release, string eventStore, DateTimeOffset readAt)
    {
        var kernel = await CheckKernel(services, release);
        var contracts = await CheckContracts(services, release);
        var schemas = await CheckSchemas(services, release, eventStore);
        return new CompatibilityResult(release, eventStore, readAt, [kernel, contracts, schemas]);
    }

    /// <summary>
    /// Builds the result for a target that could not be read at all: every check is unknown, never compatible.
    /// </summary>
    /// <param name="release">The release description.</param>
    /// <param name="eventStore">The event store that was to be checked.</param>
    /// <param name="readAt">When the read was attempted.</param>
    /// <param name="reason">Why the target could not be read.</param>
    /// <param name="action">What to do about it, when known.</param>
    /// <returns>The result, with all three checks unknown.</returns>
    public static CompatibilityResult Unreachable(ReleaseDescription release, string eventStore, DateTimeOffset readAt, string reason, string? action)
    {
        var remedy = string.IsNullOrWhiteSpace(action) ? "Resolve the problem reaching the Chronicle server, then run the check again." : action;
        return new CompatibilityResult(
            release,
            eventStore,
            readAt,
            [
                CompatibilityCheck.Undetermined(CompatibilityCheck.Kernel, ExpectedKernel(release), reason, remedy),
                CompatibilityCheck.Undetermined(CompatibilityCheck.Contracts, ExpectedContracts(release), reason, remedy),
                CompatibilityCheck.Undetermined(CompatibilityCheck.Schemas, ExpectedSchemas(release, eventStore), reason, remedy)
            ]);
    }

    static string ExpectedKernel(ReleaseDescription release) =>
        release.MinimumKernelVersion is null ? "no minimum kernel version specified" : $"kernel {release.MinimumKernelVersion} or newer";

    static string ExpectedContracts(ReleaseDescription release) =>
        $"{release.ClientType} {release.ClientVersion}, protocol {release.ProtocolVersion}";

    static string ExpectedSchemas(ReleaseDescription release, string eventStore) =>
        $"{release.EventTypes.Count} event type registration(s) accepted by event store '{eventStore}'";

    static async Task<CompatibilityCheck> CheckKernel(IServices services, ReleaseDescription release)
    {
        var expected = ExpectedKernel(release);
        var (info, failure) = await Read(services.Server.GetVersionInfo);
        if (info is null)
        {
            return CompatibilityCheck.Undetermined(CompatibilityCheck.Kernel, expected, failure, "Make sure the target Chronicle server supports reading its version, then run the check again.");
        }

        var actual = $"kernel {info.Version}, protocol {info.ProtocolVersion}";
        if (!ReleaseReader.IsVersion(info.Version))
        {
            return CompatibilityCheck.Undetermined(CompatibilityCheck.Kernel, expected, actual, "The server reported a version that is not recognized; check which Chronicle version the target runs.");
        }

        return release.MinimumKernelVersion is not null && ReleaseNoteVersion.Compare(info.Version, release.MinimumKernelVersion) < 0
            ? CompatibilityCheck.Failed(CompatibilityCheck.Kernel, expected, actual, $"Promote the target Chronicle server to {release.MinimumKernelVersion} or newer before releasing {release.Release}.")
            : CompatibilityCheck.Passed(CompatibilityCheck.Kernel, expected, actual);
    }

    static async Task<CompatibilityCheck> CheckContracts(IServices services, ReleaseDescription release)
    {
        var expected = ExpectedContracts(release);
        var (response, failure) = await Read(() => services.Connections.CheckCompatibility(new CompatibilityRequest
        {
            ClientType = release.ClientType,
            ClientVersion = release.ClientVersion,
            ProtocolVersion = release.ProtocolVersion.ToString(CultureInfo.InvariantCulture),
            DescriptorSet = release.DescriptorSet.ToArray()
        }));
        if (response is null)
        {
            return CompatibilityCheck.Undetermined(CompatibilityCheck.Contracts, expected, failure, "Make sure the target Chronicle server supports the compatibility check, then run the check again.");
        }

        var actual = $"server {response.ServerVersion}, protocol {response.ServerProtocolVersion}";
        if (response.IsCompatible)
        {
            return CompatibilityCheck.Passed(CompatibilityCheck.Contracts, expected, actual);
        }

        var incompatibilities = string.Join("; ", response.Incompatibilities ?? []);
        return CompatibilityCheck.Failed(
            CompatibilityCheck.Contracts,
            expected,
            incompatibilities.Length == 0 ? actual : $"{actual}; {incompatibilities}",
            $"Upgrade the target Chronicle server to a version that supports the wire contracts of {release.Release}, or release a build whose contracts the target supports.");
    }

    static async Task<CompatibilityCheck> CheckSchemas(IServices services, ReleaseDescription release, string eventStore)
    {
        var expected = ExpectedSchemas(release, eventStore);
        if (release.EventTypes.Count == 0)
        {
            return CompatibilityCheck.Passed(CompatibilityCheck.Schemas, "no event type registrations in the release", "nothing to check");
        }

        var (result, failure) = await Read(() => services.EventTypes.CheckEventTypeRegistrations(
            new CheckEventTypeRegistrationsRequest { EventStore = eventStore, Types = [.. release.EventTypes] },
            default));
        if (result is null)
        {
            return CompatibilityCheck.Undetermined(CompatibilityCheck.Schemas, expected, failure, "Make sure the target Chronicle server supports validating event type registrations and the credentials may read the event store, then run the check again.");
        }

        if (!result.IsSuccess)
        {
            return CompatibilityCheck.Undetermined(CompatibilityCheck.Schemas, expected, DescribeFailure(result.ExceptionMessages, result.AuthorizationFailureReason, result.ValidationResults.Select(_ => _.Message)), "Resolve the server problem or the authorization for the event store, then run the check again.");
        }

        var results = result.Response?.Results?.ToList() ?? [];
        var rejected = results.Where(_ => !_.Accepted).ToList();
        if (rejected.Count > 0)
        {
            return CompatibilityCheck.Failed(CompatibilityCheck.Schemas, expected, string.Join("; ", rejected.Select(Describe)), SchemaRemedy);
        }

        var missing = release.EventTypes
            .Where(registration => !results.Exists(_ => string.Equals(_.EventType, registration.Type.Id, StringComparison.Ordinal) && _.Generation == registration.Type.Generation))
            .Select(_ => $"{_.Type.Id} generation {_.Type.Generation}")
            .ToList();
        return missing.Count == 0
            ? CompatibilityCheck.Passed(CompatibilityCheck.Schemas, expected, $"{results.Count} of {release.EventTypes.Count} accepted")
            : CompatibilityCheck.Undetermined(
                CompatibilityCheck.Schemas,
                expected,
                $"the server returned no result for {string.Join(", ", missing)}",
                "The server did not answer for every event type; run the check again, and report it if it persists.");
    }

    static async Task<(T? Value, string Failure)> Read<T>(Func<Task<T>> read)
        where T : class
    {
        try
        {
            return (await read(), string.Empty);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unimplemented or StatusCode.NotFound or StatusCode.PermissionDenied)
        {
            return (null, $"the server answered {ex.StatusCode}");
        }
    }

    static string DescribeFailure(IEnumerable<string> exceptionMessages, string? authorizationFailure, IEnumerable<string> validationMessages)
    {
        var messages = exceptionMessages.Concat(validationMessages).ToArray();
        if (messages.Length > 0)
        {
            return string.Join("; ", messages);
        }

        return authorizationFailure is { Length: > 0 } ? authorizationFailure : "the server rejected the operation";
    }

    static string Describe(EventTypeCheckResult rejection)
    {
        var where = string.IsNullOrEmpty(rejection.Path) ? string.Empty : $" at {rejection.Path}";
        var keyword = string.IsNullOrEmpty(rejection.Keyword) ? string.Empty : $" ({rejection.Keyword})";
        return $"{rejection.EventType} generation {rejection.Generation} rejected{where}{keyword}: {rejection.Reason}";
    }
}
