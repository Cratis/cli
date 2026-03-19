// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Opens a prefilled GitHub issue for reporting errors or feedback.
/// </summary>
[Description("Open a prefilled GitHub issue to report an error or provide feedback")]
public class ReportErrorCommand : AsyncCommand<ReportErrorSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, ReportErrorSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        var title = settings.Title;
        var body = settings.Body;

        if (string.IsNullOrWhiteSpace(title) && !settings.Quiet)
        {
            title = AnsiConsole.Prompt(new TextPrompt<string>("  [bold]Issue title:[/]"));
        }

        if (string.IsNullOrWhiteSpace(body) && !settings.Quiet)
        {
            body = AnsiConsole.Prompt(new TextPrompt<string>("  [bold]Description:[/]"));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Bug report";
        }

        var titleEncoded = Uri.EscapeDataString(title);
        var bodyEncoded = body is not null ? Uri.EscapeDataString(body) : string.Empty;
        var url = $"https://github.com/cratis/chronicle/issues/new?title={titleEncoded}&body={bodyEncoded}";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            OutputFormatter.WriteMessage(format, $"Opening browser to file issue: {url}");
        }
        catch (Exception ex)
        {
            OutputFormatter.WriteError(format, $"Failed to open browser: {ex.Message}");
            Console.WriteLine($"Please open this URL manually: {url}");
        }

        return Task.FromResult(ExitCodes.Success);
    }
}
