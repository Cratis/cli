// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Owns what a run with authentication adds to the Stage: the directory of files, the Docker network and the containers beside the Stage.
/// </summary>
/// <remarks>
/// Everything it creates is removed by <see cref="Stop"/>, in the reverse of the order it was started in, whether the run ended by itself, failed or was interrupted.
/// </remarks>
public sealed class RunStackHost
{
    static readonly TimeSpan _stopTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan _readyTimeout = TimeSpan.FromMinutes(3);

    readonly string _runDirectory;
    readonly List<(PlannedContainer Container, StageSession Session)> _started = [];

    RunStackHost(RunStackPlan plan, string runDirectory)
    {
        Plan = plan;
        _runDirectory = runDirectory;
    }

    /// <summary>
    /// Gets the plan.
    /// </summary>
    public RunStackPlan Plan { get; }

    /// <summary>
    /// Gets what went wrong when the stack did not come up.
    /// </summary>
    public string? Failure { get; private set; }

    /// <summary>
    /// Gets the last lines the container that failed wrote.
    /// </summary>
    public IReadOnlyList<string> FailureOutput { get; private set; } = [];

    /// <summary>
    /// Writes the files and creates the network.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <param name="runDirectory">The run directory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The host, or no host and the reason when the network could not be created.</returns>
    public static async Task<(RunStackHost? Host, string? Error)> Prepare(RunStackPlan plan, string runDirectory, CancellationToken cancellationToken)
    {
        var host = new RunStackHost(plan, runDirectory);
        await RunStackFiles.Write(runDirectory, plan, cancellationToken);
        var (exitCode, error) = await DockerCli.Execute(plan.CreateNetworkArguments, cancellationToken);
        return exitCode == 0 ? (host, null) : (null, $"Failed to create the Docker network '{plan.Network}': {error}");
    }

    /// <summary>
    /// Starts the containers that follow the Stage.
    /// </summary>
    /// <param name="verbose">Whether to stream their output, each line led by the name of the container.</param>
    /// <param name="capture">Whether the output of the containers is captured rather than left to the console.</param>
    /// <returns>True when every container was started.</returns>
    public bool Start(bool verbose, bool capture)
    {
        foreach (var container in Plan.Containers)
        {
            try
            {
                var session = StageSession.Start(container.Name, container.Image, container.Arguments, captureOutput: capture || verbose, verbose ? $"[{container.Role}]" : null);
                if (session is null)
                {
                    Failure = $"Failed to start {container.Role}";
                    return false;
                }

                _started.Add((container, session));
            }
            catch (Win32Exception)
            {
                Failure = "Failed to start Docker";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Waits until AuthProxy answers, or a container of the stack stops.
    /// </summary>
    /// <param name="onProgress">Called on every poll.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the application answers through AuthProxy.</returns>
    public async Task<bool> WaitUntilReady(Action onProgress, CancellationToken cancellationToken)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true }) { Timeout = StageReadiness.ProbeTimeout };
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        while (System.Diagnostics.Stopwatch.GetElapsedTime(started) < _readyTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Stopped() is { } stopped)
            {
                Describe(stopped);
                return false;
            }

            if (await Answers(client, Plan.EntryUrl, cancellationToken))
            {
                return true;
            }

            onProgress();
            await Task.Delay(StageReadiness.PollInterval, cancellationToken);
        }

        Failure = $"AuthProxy did not answer on {Plan.EntryUrl} within {_readyTimeout.TotalMinutes:F0} minutes";
        return false;
    }

    /// <summary>
    /// Waits until a container of the stack exits on its own.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when one has.</returns>
    public async Task WaitForAnyExit(CancellationToken cancellationToken) =>
        await Task.WhenAny(_started.Select(_ => _.Session.WaitForExit(cancellationToken)));

    /// <summary>
    /// Describes the first container of the stack that has stopped, if any.
    /// </summary>
    /// <returns>A description of why the stack is no longer whole, or null.</returns>
    public string? DescribeStopped()
    {
        if (Stopped() is not { } stopped)
        {
            return null;
        }

        Describe(stopped);
        return Failure;
    }

    /// <summary>
    /// Stops the containers in reverse start order, removes the network and deletes the files.
    /// </summary>
    /// <param name="stage">The Stage, which is stopped with the rest because the network can only be removed once nothing is on it.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Stop(StageSession? stage)
    {
        // Ante shares AuthProxy's network stack, so it is stopped before AuthProxy; the rest go together.
        var ante = _started.Where(_ => _.Container.Role == "ante").ToArray();
        var others = _started.Where(_ => _.Container.Role != "ante").ToArray();
        await Task.WhenAll(ante.Select(_ => _.Session.Stop()));
        await Task.WhenAll(others.Select(_ => _.Session.Stop()).Concat(stage is null ? [] : [stage.Stop()]));

        var deadline = System.Diagnostics.Stopwatch.GetTimestamp();
        while (System.Diagnostics.Stopwatch.GetElapsedTime(deadline) < _stopTimeout)
        {
            var running = await Task.WhenAll(_started.Select(_ => _.Session.IsRunning()).Concat(stage is null ? [] : [stage.IsRunning()]));
            if (!running.Any(_ => _))
            {
                break;
            }

            await Task.Delay(StageReadiness.PollInterval, CancellationToken.None);
        }

        await RemoveNetwork();
        foreach (var (_, session) in _started)
        {
            session.Dispose();
        }

        RunStackFiles.Delete(_runDirectory);
    }

    static async Task<bool> Answers(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    (PlannedContainer Container, StageSession Session)? Stopped() =>
        _started.Where(_ => _.Session.HasExited).Select(_ => ((PlannedContainer, StageSession)?)_).FirstOrDefault();

    void Describe((PlannedContainer Container, StageSession Session) stopped)
    {
        Failure = stopped.Session.Startup.Error ?? $"The {stopped.Container.Role} container stopped before the application was ready";
        FailureOutput = stopped.Session.Output.Tail(RunOutput.FailureOutputLines);
    }

    async Task RemoveNetwork()
    {
        // A container that was just stopped may still be detaching, which makes Docker refuse to remove the network for a moment.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var (exitCode, _) = await DockerCli.Execute(Plan.RemoveNetworkArguments);
            if (exitCode == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
        }
    }
}
