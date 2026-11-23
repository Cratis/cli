// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// Reads and validates a release description file. Nothing is contacted and nothing is inferred: what the file does not say is rejected.
/// </summary>
public static partial class ReleaseReader
{
    static readonly JsonSerializerOptions _registrationOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [GeneratedRegex(@"^[vV]?\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex VersionPattern { get; }

    /// <summary>
    /// Checks whether text is a version: major.minor.patch with an optional leading v, prerelease and build metadata.
    /// </summary>
    /// <param name="version">The text to check.</param>
    /// <returns>True when it is a version.</returns>
    public static bool IsVersion(string? version) => version is not null && VersionPattern.IsMatch(version);

    /// <summary>
    /// Reads a release description from a file.
    /// </summary>
    /// <param name="path">The path of the release description file.</param>
    /// <param name="release">The release description when the file is valid.</param>
    /// <param name="problem">What is wrong with the file when it is not valid.</param>
    /// <returns>True when the file is a valid release description.</returns>
    public static bool TryRead(string path, out ReleaseDescription? release, out string problem)
    {
        release = null;
        problem = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            release = Parse(document.RootElement);
            return true;
        }
        catch (InvalidReleaseException ex)
        {
            problem = ex.Message;
        }
        catch (JsonException)
        {
            problem = $"The release file '{path}' is not valid JSON, or an event type registration in it has the wrong shape.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            problem = $"The release file '{path}' could not be read.";
        }

        return false;
    }

    static ReleaseDescription Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidReleaseException("The release file must contain one JSON object.");
        }

        var minimum = OptionalVersion(root, "minimumKernelVersion", string.Empty);
        var client = Client(root);
        return new ReleaseDescription(
            RequiredText(root, "release", string.Empty),
            OptionalText(root, "repository"),
            OptionalText(root, "component"),
            OptionalText(root, "environment"),
            client.Type,
            client.Version,
            client.Protocol,
            Descriptor(root),
            minimum,
            EventTypes(root));
    }

    static (string Type, string Version, uint Protocol) Client(JsonElement root)
    {
        if (!root.TryGetProperty("client", out var element) || element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidReleaseException("'client' is required and must be an object with 'type', 'version' and 'protocolVersion'.");
        }

        var type = RequiredText(element, "type", "client.");
        var version = OptionalVersion(element, "version", "client.") ?? throw new InvalidReleaseException("'client.version' is required.");
        return (type, version, ProtocolVersion(element));
    }

    static uint ProtocolVersion(JsonElement client)
    {
        var valid = client.TryGetProperty("protocolVersion", out var protocol) && protocol.ValueKind == JsonValueKind.Number;
        if (valid && protocol.TryGetUInt32(out var protocolVersion))
        {
            return protocolVersion;
        }

        throw new InvalidReleaseException("'client.protocolVersion' is required and must be a non-negative whole number.");
    }

    static ReadOnlyMemory<byte> Descriptor(JsonElement root)
    {
        var encoded = RequiredText(root, "descriptorSet", string.Empty);
        var bytes = new byte[encoded.Length];
        if (!Convert.TryFromBase64String(encoded, bytes, out var written) || written == 0)
        {
            throw new InvalidReleaseException("'descriptorSet' must be the release's own wire contract descriptor set as non-empty base64.");
        }

        return bytes.AsMemory(0, written);
    }

    static List<EventTypeRegistration> EventTypes(JsonElement root)
    {
        if (!root.TryGetProperty("eventTypes", out var element) || element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidReleaseException("'eventTypes' is required and must be an array of event type registrations.");
        }

        var registrations = new List<EventTypeRegistration>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in element.EnumerateArray())
        {
            var registration = item.Deserialize<EventTypeRegistration>(_registrationOptions) ?? throw new InvalidReleaseException($"'eventTypes[{registrations.Count}]' must be an object.");
            Validate(registration, registrations.Count, ids);
            registrations.Add(registration);
        }

        return registrations;
    }

    static void Validate(EventTypeRegistration registration, int index, HashSet<string> ids)
    {
        if (string.IsNullOrWhiteSpace(registration.Type?.Id))
        {
            throw new InvalidReleaseException($"'eventTypes[{index}].type.id' is required.");
        }

        if (registration.Type.Generation < 1)
        {
            throw new InvalidReleaseException($"'eventTypes[{index}].type.generation' is required for '{registration.Type.Id}' and starts at 1.");
        }

        if (string.IsNullOrWhiteSpace(registration.Schema))
        {
            throw new InvalidReleaseException($"'eventTypes[{index}].schema' is required for '{registration.Type.Id}' and must be the JSON schema as a string.");
        }

        if (!ids.Add(registration.Type.Id))
        {
            throw new InvalidReleaseException($"Event type '{registration.Type.Id}' is listed more than once; list each event type once with all its generations.");
        }
    }

    static string? OptionalVersion(JsonElement element, string name, string prefix)
    {
        var version = OptionalText(element, name, prefix);
        return version is null || IsVersion(version) ? version : throw new InvalidReleaseException($"'{prefix}{name}' must be a version such as 1.2.3.");
    }

    static string RequiredText(JsonElement element, string name, string prefix) =>
        OptionalText(element, name, prefix) ?? throw new InvalidReleaseException($"'{prefix}{name}' is required.");

    static string? OptionalText(JsonElement element, string name, string prefix = "")
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var value = property.ValueKind == JsonValueKind.String ? property.GetString() : null;
        return string.IsNullOrWhiteSpace(value) ? throw new InvalidReleaseException($"'{prefix}{name}' must be a non-empty string.") : value;
    }

    sealed class InvalidReleaseException(string message) : Exception(message);
}
