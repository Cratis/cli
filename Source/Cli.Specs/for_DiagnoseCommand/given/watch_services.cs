// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;
using Spectre.Console;

namespace Cratis.Cli.for_DiagnoseCommand.given;

public class watch_services : healthy_services
{
    protected CancellationTokenSource _cancellation;
    protected int _exitCode;
    protected int _sweeps;
    IAnsiConsole _previousConsole;
    protected StringWriter _writer;

    void Establish()
    {
        _cancellation = new CancellationTokenSource();

        // Invoke the watch loop directly without an interval so specs never wait on the clock.
        _settings.Interval = 0;
        _previousConsole = AnsiConsole.Console;
        _writer = new StringWriter();
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(_writer),
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No
        });
    }

    protected void StopAfterTwoSweeps(bool lastHealthy) => _services.Server.GetVersionInfo().Returns(async _ =>
    {
        _sweeps++;
        if (_sweeps == 2)
        {
            await _cancellation.CancelAsync();
        }

        var healthy = _sweeps == 2 ? lastHealthy : !lastHealthy;
        return healthy
            ? new ServerVersionInfo { Version = null! }
            : throw new Exception("Connection failed");
    });

    void Destroy()
    {
        AnsiConsole.Console = _previousConsole;
        _writer.Dispose();
        _cancellation.Dispose();
    }
}
