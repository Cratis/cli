// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_LinuxDirectSecrets.given;

public class a_delayed_secret_tool : a_fake_secret_tool
{
    protected Exception? _error;
    protected bool _exited;
    protected string? _remaining;

    string _signal = null!;

    async Task Establish()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        _signal = Path.Combine(_folder, "started");
        using var fifo = Process.Start(new ProcessStartInfo("mkfifo") { UseShellExecute = false, ArgumentList = { _signal } })!;
        await fifo.WaitForExitAsync();
        fifo.ExitCode.ShouldEqual(0);

        // Signal before any external command or input read can delay startup.
        await File.WriteAllTextAsync(_tool, $"#!/bin/sh\nprintf '%s\\n' \"$$\" > '{_signal}'\n# Deliberately slow writer: cancellation must kill it before this commit.\nsleep 30\nprintf replacement > '{_folder}/secret'\n");
        File.SetUnixFileMode(_tool, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    protected async Task Interrupt(bool cancel, bool lookup = false)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        await File.WriteAllTextAsync(Path.Combine(_folder, "secret"), "original");

        // The FIFO writer blocks until cat opens it; redirected output retains the PID until we read it.
        using var reader = Process.Start(new ProcessStartInfo("/bin/cat") { UseShellExecute = false, RedirectStandardOutput = true, ArgumentList = { _signal } })!;
        using var cancellation = new CancellationTokenSource();
        var store = new LinuxDirectSecrets(_tool, cancel ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(2));
        var write = Catch.Exception(async () =>
        {
            if (lookup)
            {
                await store.Read("key", cancellation.Token);
            }
            else
            {
                await store.Write("key", "replacement", cancellation.Token);
            }
        });
        try
        {
            var signal = await reader.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            var pid = int.Parse(signal!, System.Globalization.CultureInfo.InvariantCulture);
            if (cancel)
            {
                await cancellation.CancelAsync();
            }

            _error = await write;
            try
            {
                using var process = Process.GetProcessById(pid);
                _exited = process.HasExited;
            }
            catch (ArgumentException)
            {
                _exited = true;
            }

            _remaining = await File.ReadAllTextAsync(Path.Combine(_folder, "secret"));
        }
        finally
        {
            await cancellation.CancelAsync();
            await write;
            if (!reader.HasExited)
            {
                reader.Kill();
            }

            await reader.WaitForExitAsync();
        }
    }
}
