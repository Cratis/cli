// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Common settings for Cratis AI corpus commands.</summary>
public class AiSettings : GlobalSettings
{
    /// <summary>Gets or sets a local checkout of Cratis/AI. It makes installs deterministic in CI and offline environments.</summary>
    [Description("Path to a local checkout of Cratis/AI to read the corpus from, instead of downloading the published one. Use it to install a specific revision, to work offline, or to make a CI run deterministic. Also settable as CRATIS_AI_SOURCE")]
    [CommandOption("--source <PATH>")]
    public string? Source { get; set; }

    /// <summary>Gets or sets whether locally modified Cratis-managed files may be replaced or removed.</summary>
    [Description("Replace or remove Cratis-managed files that were edited locally. Without it, a modified managed file is reported as a conflict and nothing is written, so your edits are never lost silently")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }
}

/// <summary>Settings used to create a Cratis AI configuration.</summary>
public class AiInstallSettings : AiSettings
{
    /// <summary>Gets or sets selected harnesses as a comma-separated list.</summary>
    [Description("Comma-separated AI tools to create adapters for, for example claude,codex,copilot,cursor,opencode,pi. Each one gets its native layout pointing at the single shared corpus in .cratis/ai. Omit to choose interactively")]
    [CommandOption("--harnesses <NAMES>")]
    public string? Harnesses { get; set; }

    /// <summary>Gets or sets selected profiles as a comma-separated list.</summary>
    /// <remarks>
    /// Profiles decide which rules and skills a repository receives. cratis/application/* is for building
    /// an application on Cratis, cratis/engineering/* for contributing to a Cratis framework repository.
    /// </remarks>
    [Description("Comma-separated profiles deciding which rules and skills this repository receives. Use cratis/application/* when building an application on Cratis and cratis/engineering/* when contributing to a Cratis framework repository, plus cratis/documentation for docs work. Omit to choose interactively")]
    [CommandOption("--profiles <NAMES>")]
    public string? Profiles { get; set; }

    /// <summary>Gets or sets selected languages as a comma-separated list.</summary>
    /// <remarks>Optional. Omitting it leaves language selection unconstrained rather than empty.</remarks>
    [Description("Comma-separated languages used to narrow composed profiles, for example csharp,typescript. Optional: omit it and no language constraint is applied, so every language a selected profile composes is installed")]
    [CommandOption("--languages <NAMES>")]
    public string? Languages { get; set; }
}

/// <summary>Settings used to uninstall Cratis AI content.</summary>
public class AiUninstallSettings : AiSettings;
