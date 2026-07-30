// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Tracks how far the Stage container has come in its startup by interpreting the lines it writes.
/// Unrecognized output leaves the phase where it is, so a change to what the container prints only costs
/// progress detail — readiness itself is established by probing the Stage API.
/// </summary>
/// <param name="image">The image reference being run, reported while Docker pulls it.</param>
public partial class StageStartup(string image)
{
    /// <summary>
    /// The markers the container writes, in the order the startup goes through them. Matching is by substring
    /// so log prefixes and message parameters (counts, names) don't matter.
    /// </summary>
    static readonly (string Marker, StagePhase Phase)[] _markers =
    [
        ("Unable to find image", StagePhase.Pulling),
        ("Pulling from", StagePhase.Pulling),
        ("Starting Chronicle", StagePhase.StartingChronicle),
        ("Waiting for Chronicle", StagePhase.StartingChronicle),
        ("Chronicle is ready", StagePhase.CompilingEventModel),
        ("Using event model from Screenplay", StagePhase.CompilingEventModel),
        ("Starting Stage...", StagePhase.StartingStage),
        ("Stage running event model", StagePhase.StartingStage),
        ("Application started", StagePhase.RegisteringReadModels),
        ("read model(s) and their projections", StagePhase.Running),
        ("has no read models with projections", StagePhase.Running),
        ("Failed to register the event model", StagePhase.Running)
    ];

    /// <summary>
    /// Gets the phase the container is currently in.
    /// </summary>
    public StagePhase Phase { get; private set; } = StagePhase.Starting;

    /// <summary>
    /// Gets the name of the event model the container compiled, once it has reported it.
    /// </summary>
    public string? EventModel { get; private set; }

    /// <summary>
    /// Gets the name of the event store the container runs the model as, once it has reported it.
    /// </summary>
    public string? EventStore { get; private set; }

    /// <summary>
    /// Gets the error the container reported before giving up, if it reported one.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Gets the progress text describing what the container is currently doing.
    /// </summary>
    public string Status => Phase switch
    {
        StagePhase.Pulling => $"Pulling {image}",
        StagePhase.StartingChronicle => "Starting Chronicle",
        StagePhase.CompilingEventModel => "Compiling Screenplay files",
        StagePhase.StartingStage => "Starting the Stage",
        StagePhase.RegisteringReadModels => "Registering read models",
        StagePhase.Running => "Waiting for the Stage API",
        _ => "Starting the Stage container"
    };

    [GeneratedRegex("event model '(?<model>[^']*)' as event store '(?<store>[^']*)'", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    static partial Regex RunningRegex { get; }

    /// <summary>
    /// Interprets a line the container wrote and advances the startup accordingly.
    /// </summary>
    /// <param name="line">The line the container wrote.</param>
    /// <returns>True when the line moved the startup to a new phase; otherwise false.</returns>
    public bool Observe(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        CaptureError(line);
        CaptureModel(line);

        var phase = Phase;
        foreach (var (marker, candidate) in _markers)
        {
            if (candidate > phase && line.Contains(marker, StringComparison.Ordinal))
            {
                phase = candidate;
            }
        }

        if (phase == Phase)
        {
            return false;
        }

        Phase = phase;

        return true;
    }

    void CaptureError(string line)
    {
        const string prefix = "ERROR:";
        if (line.StartsWith(prefix, StringComparison.Ordinal))
        {
            Error = line[prefix.Length..].Trim();
        }
    }

    void CaptureModel(string line)
    {
        var match = RunningRegex.Match(line);
        if (!match.Success)
        {
            return;
        }

        EventModel = match.Groups["model"].Value;
        EventStore = match.Groups["store"].Value;
    }
}
