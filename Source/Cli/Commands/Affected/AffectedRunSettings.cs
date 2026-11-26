// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Options for selecting and running tests.
/// </summary>
public sealed class AffectedRunSettings : AffectedSelectSettings
{
    /// <summary>
    /// Gets or sets the folder test results are written to.
    /// </summary>
    [CommandOption("--results-directory <PATH>")]
    [Description("Where TRX result files are written. Default: affected-results")]
    [DefaultValue("affected-results")]
    public string ResultsDirectory { get; set; } = "affected-results";

    /// <summary>
    /// Gets or sets the coverage collector executable.
    /// </summary>
    [CommandOption("--collector <EXECUTABLE>")]
    [Description("The coverage collector used in merge mode to attribute coverage to each test unit: dotnet-coverage or coverlet. Default: dotnet-coverage")]
    [DefaultValue("dotnet-coverage")]
    public string Collector { get; set; } = "dotnet-coverage";

    /// <summary>
    /// Gets or sets how many coverage runs may run at once.
    /// </summary>
    [CommandOption("--parallelism <COUNT>")]
    [Description("How many coverage runs run at once in merge mode. dotnet-coverage only; coverlet always runs one. Default: 1")]
    [DefaultValue(1)]
    public int Parallelism { get; set; } = 1;
}
