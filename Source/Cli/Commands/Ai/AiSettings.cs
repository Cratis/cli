// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Common settings for Cratis AI corpus commands.</summary>
public class AiSettings : GlobalSettings
{
    /// <summary>Gets or sets a local checkout of Cratis/AI. It makes installs deterministic in CI and offline environments.</summary>
    [CommandOption("--source <PATH>")]
    public string? Source { get; set; }

    /// <summary>Gets or sets whether locally modified Cratis-managed files may be replaced or removed.</summary>
    [CommandOption("-f|--force")]
    public bool Force { get; set; }
}

/// <summary>Settings used to create a Cratis AI configuration.</summary>
public class AiInstallSettings : AiSettings
{
    /// <summary>Gets or sets selected harnesses as a comma-separated list.</summary>
    [CommandOption("--harnesses <NAMES>")]
    public string? Harnesses { get; set; }

    /// <summary>Gets or sets selected profiles as a comma-separated list.</summary>
    [CommandOption("--profiles <NAMES>")]
    public string? Profiles { get; set; }

    /// <summary>Gets or sets selected languages as a comma-separated list.</summary>
    [CommandOption("--languages <NAMES>")]
    public string? Languages { get; set; }
}

/// <summary>Settings used to uninstall Cratis AI content.</summary>
public class AiUninstallSettings : AiSettings;
