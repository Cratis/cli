// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// One key a model needs, and whether the environment has it. The value is never part of it.
/// </summary>
/// <param name="Key">The configuration key.</param>
/// <param name="Source">What asks for it: an authentication provider, onboarding, a named secret or a system setting.</param>
/// <param name="Owner">The provider, secret or system it belongs to.</param>
/// <param name="Secret">Whether it is a secret, stored protected and never printed.</param>
/// <param name="Description">What it is for.</param>
/// <param name="Set">Whether the environment holds a value for it.</param>
public record RequiredKeyStatus(string Key, string Source, string Owner, bool Secret, string Description, bool Set);

/// <summary>
/// Lists what a model's extensions and identity providers need from the selected environment, and which of it is missing.
/// </summary>
public static class ConfigRequirementsReport
{
    /// <summary>
    /// Compares a model's requirements with an environment. Only whether a key is set is read, never its value.
    /// </summary>
    /// <param name="requirements">The model's requirements.</param>
    /// <param name="document">The environment.</param>
    /// <returns>One status for each required key.</returns>
    public static IReadOnlyList<RequiredKeyStatus> Compare(ModelRequirements requirements, EnvironmentDocument document) =>
    [
        .. requirements.Requirements.Keys
            .DistinctBy(key => key.Key, StringComparer.Ordinal)
            .Select(key => new RequiredKeyStatus(key.Key, key.Source.ToString(), key.Owner, key.IsSecret, key.Description, document.Config.ContainsKey(key.Key)))
    ];

    /// <summary>
    /// Writes the statuses and the references whose contract could not be read, and returns the exit code.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="requirements">The model's requirements.</param>
    /// <param name="statuses">The statuses.</param>
    /// <returns>Success when every key is set, otherwise the validation error code.</returns>
    public static int Write(string format, string environment, ModelRequirements requirements, IReadOnlyList<RequiredKeyStatus> statuses)
    {
        if (statuses.Count == 0 && requirements.Unresolved.Count == 0)
        {
            OutputFormatter.WriteMessage(format, "The model needs nothing from an environment");
            return ExitCodes.Success;
        }

        OutputFormatter.Write(
            format,
            statuses.ToArray(),
            ["Key", "Kind", "Status", "Description"],
            status => [status.Key, status.Secret ? "secret" : "setting", status.Set ? "set" : "missing", status.Description],
            status => status.Key);

        foreach (var reference in requirements.Unresolved)
        {
            OutputFormatter.WriteMessage(format, $"The extension '{reference}' is not restored, so its own settings cannot be listed. Run: cratis extension restore");
        }

        var missing = statuses.Count(status => !status.Set);
        if (missing > 0)
        {
            OutputFormatter.WriteMessage(format, $"Environment '{environment}' is missing {missing} of {statuses.Count} key(s). Set them with: cratis config set <key> [--secret]");
        }

        return missing > 0 || requirements.Unresolved.Count > 0 ? ExitCodes.ValidationError : ExitCodes.Success;
    }
}
