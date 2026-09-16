// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Configuration;

namespace Cratis.Cli.Templates;

/// <summary>
/// Renders a template's own parameters — descriptions, data types, choices, defaults and prompts —
/// derived from its symbols, for <c language="csharp">cratis new &lt;template&gt; --parameters</c> and for JSON output.
/// </summary>
public static class TemplateParameterHelp
{
    /// <summary>
    /// Describes one parameter for help output.
    /// </summary>
    /// <param name="Parameter">The parameter symbol.</param>
    /// <returns>A description record.</returns>
    public static object Describe(SymbolConfig Parameter) => new
    {
        name = Parameter.Name,
        option = $"--{Parameter.Name}",
        dataType = Parameter.DataType ?? "string",
        description = Parameter.Description ?? string.Empty,
        displayName = Parameter.DisplayName ?? Parameter.Name,
        prompt = Parameter.Prompt,
        defaultValue = Parameter.DefaultValue,
        choices = Parameter.DataType == "choice"
            ? Parameter.Choices.Select(choice => new { choice = choice.Choice, description = choice.Description ?? string.Empty })
            : null
    };

    /// <summary>
    /// Describes all parameters of a manifest in declaration order.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <returns>The parameter descriptions.</returns>
    public static IEnumerable<object> DescribeAll(TemplateConfig manifest) =>
        manifest.Symbols.Values
            .Where(symbol => symbol.Type == SymbolType.Parameter)
            .Select(Describe);

    /// <summary>
    /// Renders the human-readable parameter table.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="templateName">The template name for the header.</param>
    /// <param name="accent">The accent markup color.</param>
    /// <param name="muted">The muted markup color.</param>
    public static void Render(TemplateConfig manifest, string templateName, string accent, string muted)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{accent}]Parameters for '{templateName.EscapeMarkup()}'[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(OutputFormatter.Muted))
            .AddColumn(new TableColumn($"[{accent}]Option[/]"))
            .AddColumn(new TableColumn($"[{accent}]Type[/]"))
            .AddColumn(new TableColumn($"[{accent}]Choices[/]"))
            .AddColumn(new TableColumn($"[{accent}]Default[/]"))
            .AddColumn(new TableColumn($"[{accent}]Description[/]"));

        foreach (var symbol in manifest.Symbols.Values.Where(symbol => symbol.Type == SymbolType.Parameter))
        {
            table.AddRow(
                $"[bold]--{symbol.Name.EscapeMarkup()}[/]",
                (symbol.DataType ?? "string").EscapeMarkup(),
                symbol.DataType == "choice"
                    ? string.Join(", ", symbol.Choices.Select(choice => choice.Choice))
                    : $"[{muted}]—[/]",
                symbol.DefaultValue ?? $"[{muted}]—[/]",
                symbol.Description ?? string.Empty);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{muted}]Pass parameters as[/] [bold]--<Name> <value>[/] [{muted}]— repeat a multi-value choice parameter to accumulate values.[/]");
        AnsiConsole.WriteLine();
    }
}
