// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Lists the profiles offered by the public and engineering catalogs of the Cratis AI corpus.
/// </summary>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand(
    "list",
    "List every profile the Cratis AI corpus offers, from the public and engineering catalogs. Changes nothing.\n\n" +
    "Pick a value for 'cratis ai install --profiles'; use --output json for scripts and --source to read a local checkout.",
    Branch = typeof(AiBranch.Profiles))]
[CliExample("ai", "profiles", "list")]
[CliExample("ai", "profiles", "list", "--output", "json")]
[CliExample("ai", "profiles", "list", "--source", "../AI")]
public sealed class AiProfilesListCommand : AsyncCommand<AiProfilesListSettings>
{
    /// <summary>
    /// Writes the available profiles in the requested output format.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="profiles">The profiles to list.</param>
    /// <returns>The success exit code.</returns>
    public static int Write(string format, IReadOnlyList<AiProfile> profiles)
    {
        OutputFormatter.Write(
            format,
            profiles,
            ["Profile", "Catalog", "Languages", "Composes"],
            profile => [profile.Id, profile.Catalog, profile.Languages.Count == 0 ? "-" : string.Join(", ", profile.Languages), profile.Composes.Count.ToString(CultureInfo.InvariantCulture)],
            profile => profile.Id);
        return ExitCodes.Success;
    }

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, AiProfilesListSettings settings, CancellationToken cancellationToken)
    {
        using var corpus = AiCorpusSource.Resolve(settings.Source ?? Environment.GetEnvironmentVariable("CRATIS_AI_SOURCE"));
        return Task.FromResult(Write(settings.ResolveOutputFormat(), AiCorpusProfiles.Listed(corpus.Path)));
    }
}
