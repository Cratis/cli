// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts;
using Cratis.Cli.Commands.Chronicle.Workbench;
using Cratis.Cli.given;
using SharpConsoleUI;
using SharpConsoleUI.Configuration;
using SharpConsoleUI.Drivers;
using SharpConsoleUI.Panel;

namespace Cratis.Cli.for_WorkbenchRefreshLoop;

[Collection(CliSpecsCollection.Name)]
public class when_refreshing_with_an_expired_login : a_temp_config_directory
{
    WorkbenchRefreshLoop _loop = null!;
    StatusTextElement _panelText = null!;
    ConsoleWindowSystem _windowSystem = null!;
    IWorkbenchView _view = null!;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        new CliConfiguration
        {
            Contexts = new Dictionary<string, CliContext>
            {
                ["default"] = new() { LoggedInUser = "admin", TokenServer = "localhost:35000", AccessToken = "expired-token", TokenExpiry = DateTimeOffset.UtcNow.AddHours(-1).ToString("O") }
            }
        }.Save();
        var settings = new WorkbenchSettings { Server = "chronicle://localhost:35000" };
        _panelText = new StatusTextElement(string.Empty);
        _windowSystem = new ConsoleWindowSystem(new HeadlessConsoleDriver(200, 50),
            options: new ConsoleWindowSystemOptions(TopPanelConfig: panel => panel.Left(_panelText)));
        _view = Substitute.For<IWorkbenchView>();
        var navigation = new WorkbenchNavigation(_windowSystem, new WorkbenchTheme(_windowSystem), [_view], settings, () => null, () => null, _ => { }, _ => { }, () => { }, () => null);
        _loop = new WorkbenchRefreshLoop(new WorkbenchDataService(Substitute.For<IServices>(), settings), settings, [_view], navigation, _windowSystem, () => null, () => null);
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because()
    {
        await _loop.FetchAndUpdate(CancellationToken.None);

        // SharpConsoleUI's test hook drains queued UI work without starting a render loop or waiting on a clock.
        typeof(ConsoleWindowSystem).GetMethod("DrainUiThreadQueueForTests", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(_windowSystem, null);
    }

    protected override void CleanUp()
    {
        Console.SetError(_previousError);
        _error.Dispose();
        _windowSystem.Shutdown();
        base.CleanUp();
    }

    [Fact] void should_show_the_login_expired_panel() => _panelText.Text.ShouldEqual("Login expired — run cratis chronicle login again");
    [Fact] void should_not_replace_the_snapshot() => _loop.CurrentData.ShouldBeNull();
    [Fact] void should_not_publish_data_to_views() => _view.DidNotReceive().UpdateData(Arg.Any<WorkbenchData>());
    [Fact] void should_not_write_over_the_tui() => _error.ToString().ShouldBeEmpty();
}
