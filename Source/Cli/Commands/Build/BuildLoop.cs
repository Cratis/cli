// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The builder's side of Studio's build loop: connect, ask for work, build each claimed slice while renewing its lease, and
/// report one outcome. Studio decides everything else: what is offered, retries and the slice's status. A lost lease stops
/// the build and nothing is reported for it.
/// </summary>
/// <param name="client">The builder client, authenticated.</param>
/// <param name="build">Builds an order; cancelled when the lease is lost.</param>
/// <param name="log">Where the loop writes what it is doing.</param>
/// <param name="delay">Waits between retries; replaceable in specifications.</param>
public sealed class BuildLoop(StudioBuilderClient client, Func<BuildOrder, BuildGitInstructions, CancellationToken, Task<JsonObject>> build, Action<string> log, Func<TimeSpan, CancellationToken, Task>? delay = null)
{
    readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;

    /// <summary>
    /// Connects, waiting while a maintainer has not yet approved the builder.
    /// </summary>
    /// <param name="manifest">What the builder can build.</param>
    /// <param name="cancellationToken">Stops waiting.</param>
    /// <returns>What Studio agreed.</returns>
    /// <exception cref="StudioBuilderRefused">Studio refused the builder for a reason waiting cannot change.</exception>
    public async Task<StudioBuilderWelcome> Connect(JsonObject manifest, CancellationToken cancellationToken)
    {
        var shown = false;
        while (true)
        {
            try
            {
                return await client.Connect(manifest, cancellationToken);
            }
            catch (StudioBuilderRefused refused) when (refused.AwaitsApproval)
            {
                if (!shown) log($"Waiting for a maintainer to approve this builder in Studio under Settings > Builders with the code {refused.Code}.");
                shown = true;
                await _delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Runs the loop until cancelled, or until one slice was built when <paramref name="once"/> is set.
    /// </summary>
    /// <param name="manifest">What the builder can build.</param>
    /// <param name="once">Whether to stop after one slice.</param>
    /// <param name="cancellationToken">Stops the loop; the builder then says goodbye.</param>
    /// <returns>The number of slices built.</returns>
    /// <exception cref="StudioBuilderRefused">Studio no longer accepts the builder.</exception>
    public async Task<int> Run(JsonObject manifest, bool once, CancellationToken cancellationToken)
    {
        var welcome = await Connect(manifest, cancellationToken);
        if (welcome.NeedsUpgrade) log("Studio speaks a newer builder protocol; update the Cratis CLI.");
        log($"Connected to Studio as {welcome.Name}. Waiting for slices to build.");
        var built = 0;
        var failures = 0;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                StudioBuilderWork? work;
                try
                {
                    work = await client.Work(cancellationToken);
                    failures = 0;
                }
                catch (StudioBuilderRefused refused) when (refused.Status == HttpStatusCode.Conflict)
                {
                    await Connect(manifest, cancellationToken);
                    continue;
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
                {
                    failures++;
                    log($"Studio could not be reached ({exception.Message}); trying again.");
                    await _delay(TimeSpan.FromSeconds(Math.Min(60, 2 << Math.Min(failures, 5))), cancellationToken);
                    continue;
                }

                if (work is null) continue;
                if (await Build(work, welcome.LeaseSeconds, cancellationToken)) built++;
                if (once) break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            try
            {
                await client.Disconnect(CancellationToken.None);
            }
            catch (HttpRequestException)
            {
            }
        }

        return built;
    }

    async Task<bool> Build(StudioBuilderWork work, int leaseSeconds, CancellationToken cancellationToken)
    {
        var slices = string.Join(", ", work.Order.Slices.Select(slice => slice.Name));
        log($"Building the {work.Order.Part} of {slices} (attempt {work.Order.Attempt}).");
        var lease = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = Heartbeat(work, leaseSeconds, lease);
        JsonObject outcome;
        try
        {
            outcome = await build(work.Order, work.Git, lease.Token);
        }
        catch (OperationCanceledException) when (lease.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            log($"The lease on {slices} was lost; the build stopped and nothing was reported.");
            return false;
        }
        finally
        {
            await lease.CancelAsync();
            await heartbeat;
            lease.Dispose();
        }

        return await Report(work, outcome, slices, cancellationToken);
    }

    async Task<bool> Report(StudioBuilderWork work, JsonObject outcome, string slices, CancellationToken cancellationToken)
    {
        try
        {
            var settled = await client.Report(work.SliceId, outcome, cancellationToken);
            log($"Reported {outcome["kind"]} for {slices}; Studio settled it as {settled}.");
            return outcome["kind"]?.GetValue<string>() == BuildOutcomes.Built;
        }
        catch (StudioBuilderRefused refused) when (refused.Status == HttpStatusCode.BadRequest)
        {
            log($"Studio refused the outcome: {refused.Message}");
            var failure = BuildOutcomes.Failure(work.Order, $"The builder's outcome was refused: {string.Join(' ', refused.Problems)}", true);
            await client.Report(work.SliceId, failure, cancellationToken);
            return false;
        }
        catch (StudioBuilderRefused refused) when (refused.Status == HttpStatusCode.Conflict)
        {
            log($"Studio did not take the outcome: {refused.Reason}");
            return false;
        }
    }

    async Task Heartbeat(StudioBuilderWork work, int leaseSeconds, CancellationTokenSource lease)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, leaseSeconds / 3));
        try
        {
            while (!lease.IsCancellationRequested)
            {
                await _delay(interval, lease.Token);
                bool held;
                try
                {
                    held = await client.Renew(work.SliceId, work.Order.OrderId, lease.Token);
                }
                catch (HttpRequestException exception)
                {
                    log($"Renewing the lease failed ({exception.Message}); trying again.");
                    continue;
                }

                if (!held)
                {
                    await lease.CancelAsync();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
