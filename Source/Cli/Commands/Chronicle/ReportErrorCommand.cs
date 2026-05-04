// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Opens a prefilled GitHub issue for reporting errors or feedback.
/// </summary>
[LlmDescription("Opens a pre-filled GitHub issue in the browser to report a CLI or server error. Use when encountering unexpected errors.")]
[CliCommand("report-error", "Open a GitHub issue to report an error or provide feedback", Branch = typeof(ChronicleBranch))]
[CliExample("chronicle", "report-error")]
[CliExample("chronicle", "report-error", "--title", "Bug: X fails when Y")]
[Description("Open a prefilled GitHub issue to report an error or provide feedback")]
public class ReportErrorCommand : AsyncCommand<ReportErrorSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, ReportErrorSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        var title = settings.Title;
        var body = settings.Body;

        if (string.IsNullOrWhiteSpace(title) && !settings.Quiet)
        {
            title = await AnsiConsole.PromptAsync(new TextPrompt<string>("  [bold]Issue title:[/]"));
        }

        if (string.IsNullOrWhiteSpace(body) && !settings.Quiet)
        {
            body = await AnsiConsole.PromptAsync(new TextPrompt<string>("  [bold]Description:[/]"));
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
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else
            {
                Process.Start("xdg-open", url);
            }

            OutputFormatter.WriteMessage(format, $"Opening browser to file issue: {url}");
        }
        catch (Exception ex)
        {
            OutputFormatter.WriteError(format, $"Failed to open browser: {ex.Message}");
            Console.WriteLine($"Please open this URL manually: {url}");
        }

        return ExitCodes.Success;
    }
}
