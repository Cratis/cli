// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Well-known output format identifiers for the CLI.
/// </summary>
public static class OutputFormats
{
    /// <summary>
    /// JSON output format.
    /// </summary>
    public const string Json = "json";

    /// <summary>
    /// Plain tab-separated output format.
    /// </summary>
    public const string Plain = "plain";

    /// <summary>
    /// Rich table output format with borders and colors (default for interactive terminals).
    /// </summary>
    public const string Table = "table";

    /// <summary>
    /// Automatic format detection based on terminal capabilities.
    /// </summary>
    public const string Auto = "auto";

    /// <summary>
    /// Compact (non-indented) JSON format — lower token count than <see cref="Json"/> while remaining fully machine-parseable.
    /// </summary>
    public const string JsonCompact = "json-compact";

    /// <summary>
    /// Quiet mode — outputs only key identifiers, one per line, with no headers or decoration.
    /// </summary>
    public const string Quiet = "quiet";

    /// <summary>
    /// Quiet JSON mode — outputs a JSON array of key identifiers only. Activated when both <see cref="Quiet"/> and a JSON output format are requested.
    /// </summary>
    public const string JsonQuiet = "json-quiet";
}
