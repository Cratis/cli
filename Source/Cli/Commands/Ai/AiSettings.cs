// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Common settings for Cratis AI corpus commands.</summary>
public class AiSettings : GlobalSettings
{
    /// <summary>Gets or sets a local checkout of Cratis/AI. It makes installs deterministic in CI and offline environments.</summary>
    [Description("Read the corpus from a local Cratis/AI checkout instead of downloading it. Pins a revision and works offline. Env: CRATIS_AI_SOURCE")]
    [CommandOption("--source <PATH>")]
    public string? Source { get; set; }

    /// <summary>Gets or sets whether locally modified Cratis-managed files may be replaced or removed.</summary>
    [Description("Allow replacing Cratis-managed files you edited locally. Without it such a file is reported and nothing is written")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }
}

/// <summary>Settings used to create a Cratis AI configuration.</summary>
public class AiInstallSettings : AiSettings
{
    /// <summary>Gets or sets selected harnesses as a comma-separated list.</summary>
    [Description("Which AI tools get an adapter: claude, codex, copilot, cursor, opencode, pi. Adding one you do not use is harmless. Required unless prompted interactively")]
    [CommandOption("--harnesses <NAMES>")]
    public string? Harnesses { get; set; }

    /// <summary>Gets or sets selected profiles as a comma-separated list.</summary>
    /// <remarks>
    /// Profiles decide which rules and skills a repository receives. cratis/application/* is for building
    /// an application on Cratis, cratis/engineering/* for contributing to a Cratis framework repository.
    /// </remarks>
    [Description("Which body of guidance this repository receives. See the description above. Required unless prompted interactively")]
    [CommandOption("--profiles <NAMES>")]
    public string? Profiles { get; set; }

    /// <summary>Gets or sets selected languages as a comma-separated list.</summary>
    /// <remarks>Optional. Omitting it leaves language selection unconstrained rather than empty.</remarks>
    [Description("Optional. Narrows which languages the selected profiles install, for example csharp, typescript. Omit for no constraint")]
    [CommandOption("--languages <NAMES>")]
    public string? Languages { get; set; }
}

/// <summary>Settings used to uninstall Cratis AI content.</summary>
public class AiUninstallSettings : AiSettings;
