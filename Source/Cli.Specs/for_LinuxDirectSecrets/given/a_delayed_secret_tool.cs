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

    void Establish() => Behave($"printf '%s' \"$$\" > '{_folder}/pid'\ntouch '{_folder}/ready'\n# Deliberately slow writer: cancellation must kill it before this commit.\nsleep 30\nprintf replacement > '{_folder}/secret'");

    protected async Task Interrupt(bool cancel)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        await File.WriteAllTextAsync(Path.Combine(_folder, "secret"), "original");
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(_folder, "ready");
        watcher.Created += (_, _) => ready.TrySetResult();
        watcher.EnableRaisingEvents = true;
        using var cancellation = new CancellationTokenSource();
        var store = new LinuxDirectSecrets(_tool, cancel ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(2));
        var write = Catch.Exception(() => store.Write("key", "replacement", cancellation.Token));
        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (cancel)
            {
                await cancellation.CancelAsync();
            }

            _error = await write;
            var pid = int.Parse(await File.ReadAllTextAsync(Path.Combine(_folder, "pid")), System.Globalization.CultureInfo.InvariantCulture);
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
        }
    }
}
