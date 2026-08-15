// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Probes the Stage API to establish when the container can actually accept requests.
/// </summary>
public static class StageReadiness
{
    /// <summary>
    /// The time to wait between probes.
    /// </summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// How long a single probe is given to answer.
    /// </summary>
    public static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long to keep waiting for the container to report its read models after the API starts answering,
    /// before considering it ready regardless.
    /// </summary>
    public static readonly TimeSpan ReadModelRegistrationGrace = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Determines whether the Stage API answers on the given host port. Any HTTP response counts — the Stage
    /// only serves the routes its event model defines, so the root answers 404 even when it is fully up.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> to probe with.</param>
    /// <param name="port">The host port the Stage API is published on.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> for canceling the probe.</param>
    /// <returns>True when the API answered; otherwise false.</returns>
    public static async Task<bool> IsServing(HttpClient client, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(new Uri($"http://localhost:{port}/"), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return true;
        }
        catch (HttpRequestException)
        {
            // Nothing is listening on the container side of the published port yet.
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The probe itself timed out - the container is accepting connections but not answering yet.
            return false;
        }
    }
}
