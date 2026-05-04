// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli;

if (args.Length == 0 && !Console.IsOutputRedirected && !GlobalSettings.IsAiAgentEnvironment())
{
    Banner.Render();
    FirstRunDetector.ShowIfNeeded();

    // Show static context status so the user immediately sees where the CLI is pointed.
    // This reads from config only — no connection attempt, instant output.
    var config = CliConfiguration.Load();
    var ctx = config.GetCurrentContext();
    var server = ctx.Server ?? "chronicle://localhost:35000/?disableTls=true";
    var muted = OutputFormatter.Muted.ToMarkup();
    var accent = OutputFormatter.Accent.ToMarkup();
    AnsiConsole.MarkupLine($"  [{muted}]Context:[/] [{accent}]{config.ActiveContextName.EscapeMarkup()}[/] [{muted}]→[/] {server.EscapeMarkup()}");
    AnsiConsole.WriteLine();
}

return await CliApp.Create().RunAsync(args);

