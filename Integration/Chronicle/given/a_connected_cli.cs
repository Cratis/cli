// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Connections;

namespace Cratis.Cli.Integration.Chronicle.given;

/// <summary>
/// Base context for specs that run CLI commands against a live Chronicle server.
/// </summary>
public class a_connected_cli : Specification
{
    /// <summary>
    /// The longest a spec waits for something it wrote to become visible to a subsequent read.
    /// </summary>
    /// <remarks>
    /// Chronicle is event sourced. A command such as <c language="csharp">users add</c> returns as soon as its event is
    /// appended to the event log; the kernel reactor that projects that event into the store the matching
    /// <c language="csharp">list</c> command reads from runs afterwards. Reading the list straight after the add is therefore
    /// a read-after-write race, normally won by a few milliseconds but lost often enough on a loaded CI
    /// runner to make the specs flaky. The wait is bounded so a write that never lands still fails, and
    /// fails with something a reader of the log can act on.
    /// </remarks>
    protected static readonly TimeSpan ReadBackTimeout = TimeSpan.FromSeconds(30);

    static readonly TimeSpan _readBackPollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Gets the gRPC connection string for the Docker-hosted Chronicle server.
    /// </summary>
    protected static string ConnectionString
    {
        get
        {
            var certPath = Uri.EscapeDataString(ChronicleOutOfProcessFixtureWithLocalImage.CertificatePath);
            return $"chronicle://{ChronicleConnectionString.DevelopmentClient}:{ChronicleConnectionString.DevelopmentClientSecret}@localhost:35001/?certificatePath={certPath}&certificatePassword=TestPassword123";
        }
    }

    /// <summary>
    /// Gets the live server address with TLS options, but without embedded authentication.
    /// </summary>
    protected static string ConnectionStringWithoutCredentials
    {
        get
        {
            var certPath = Uri.EscapeDataString(ChronicleOutOfProcessFixtureWithLocalImage.CertificatePath);
            return $"chronicle://localhost:35001/?certificatePath={certPath}&certificatePassword=TestPassword123";
        }
    }

    /// <summary>
    /// Runs a CLI command against the live server without embedded credentials and with JSON output.
    /// </summary>
    /// <param name="args">The command arguments (without --server and --output flags).</param>
    /// <returns>The command execution result.</returns>
    protected static Task<CliCommandResult> RunCliWithoutCredentialsAsync(params string[] args)
    {
        var allArgs = new List<string>(args) { "--server", ConnectionStringWithoutCredentials, "--output", "json" };
        return CliCommandRunner.RunAsync([.. allArgs]);
    }

    /// <summary>
    /// Runs a CLI command against the live server with JSON output format, signing in as an administrator for the System event store.
    /// </summary>
    /// <param name="args">The command arguments (without --server and --output flags).</param>
    /// <returns>The command execution result.</returns>
    protected static Task<CliCommandResult> RunCliAsync(params string[] args)
    {
        var eventStore = args.SkipWhile(argument => argument != "--event-store").Skip(1).FirstOrDefault();
        if (string.Equals(eventStore, "system", StringComparison.OrdinalIgnoreCase))
        {
            return RunCliAsAdministratorAsync(args);
        }

        var allArgs = new List<string>(args) { "--server", ConnectionString, "--output", "json" };
        return CliCommandRunner.RunAsync([.. allArgs]);
    }

    /// <summary>
    /// Runs a CLI command that needs an administrator, such as accessing the System event store or managing users or applications.
    /// </summary>
    /// <param name="args">The command arguments (without --server and --output flags).</param>
    /// <returns>The command execution result.</returns>
    /// <remarks>
    /// Signs in as the administrator first, on every call. Specs share one CLI configuration and some of them log
    /// out, log in as another user or store invalid client credentials, so a login made once would not survive.
    /// The command itself runs without embedded credentials: embedded credentials in the server address take
    /// precedence over the stored login token.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the administrator login fails.</exception>
    protected static async Task<CliCommandResult> RunCliAsAdministratorAsync(params string[] args)
    {
        var login = await RunCliWithoutCredentialsAsync(
            "chronicle",
            "login",
            ChronicleOutOfProcessFixtureWithLocalImage.AdministratorUsername,
            "--secret",
            ChronicleOutOfProcessFixtureWithLocalImage.AdministratorPassword);

        if (login.ExitCode != ExitCodes.Success)
        {
            throw new InvalidOperationException(
                $"Logging in as the administrator failed with exit code {login.ExitCode}. Output: {login.StandardOutput} Error: {login.StandardError}");
        }

        return await RunCliWithoutCredentialsAsync(args);
    }

    /// <summary>
    /// Runs an administrator CLI command that outputs a JSON array repeatedly until an element matching a predicate shows up.
    /// </summary>
    /// <param name="description">Describes what is being waited for, used in the failure message.</param>
    /// <param name="matches">Predicate that identifies the element being waited for.</param>
    /// <param name="args">The command arguments (without --server and --output flags).</param>
    /// <returns>A copy of the matching element, safe to read after this method returns.</returns>
    /// <exception cref="ReadBackTimedOut">
    /// Thrown when no matching element shows up within <see cref="ReadBackTimeout"/>.
    /// </exception>
    protected static Task<JsonElement> WaitForElementInListAsAdministrator(string description, Func<JsonElement, bool> matches, params string[] args) =>
        WaitForElement(RunCliAsAdministratorAsync, description, matches, args);

    /// <summary>
    /// Runs a CLI command that outputs a JSON array repeatedly until an element matching a predicate shows up.
    /// </summary>
    /// <param name="description">Describes what is being waited for, used in the failure message.</param>
    /// <param name="matches">Predicate that identifies the element being waited for.</param>
    /// <param name="args">The command arguments (without --server and --output flags).</param>
    /// <returns>A copy of the matching element, safe to read after this method returns.</returns>
    /// <exception cref="ReadBackTimedOut">
    /// Thrown when no matching element shows up within <see cref="ReadBackTimeout"/>.
    /// </exception>
    /// <remarks>
    /// Polling rather than sleeping keeps the common case as fast as the server is: the first read almost
    /// always finds the element, and only a run that actually lost the race pays for another attempt.
    /// </remarks>
    protected static Task<JsonElement> WaitForElementInList(string description, Func<JsonElement, bool> matches, params string[] args) =>
        WaitForElement(RunCliAsync, description, matches, args);

    static async Task<JsonElement> WaitForElement(Func<string[], Task<CliCommandResult>> run, string description, Func<JsonElement, bool> matches, string[] args)
    {
        var elapsed = Stopwatch.StartNew();
        var attempts = 0;

        while (true)
        {
            var result = await run(args);
            attempts++;

            if (result.ExitCode == ExitCodes.Success && TryFindElement(result.StandardOutput, matches, out var element))
            {
                return element;
            }

            if (elapsed.Elapsed >= ReadBackTimeout)
            {
                throw new ReadBackTimedOut(description, args, attempts, elapsed.Elapsed, result);
            }

            await Task.Delay(_readBackPollInterval);
        }
    }

    static bool TryFindElement(string json, Func<JsonElement, bool> matches, out JsonElement element)
    {
        element = default;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var candidate in document.RootElement.EnumerateArray())
            {
                if (matches(candidate))
                {
                    // Clone so the element stays readable once the document it was parsed from is disposed.
                    element = candidate.Clone();
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // The command did not produce JSON on this attempt. Treat it as "not there yet" and let the
            // timeout report the output it did produce.
        }

        return false;
    }
}
