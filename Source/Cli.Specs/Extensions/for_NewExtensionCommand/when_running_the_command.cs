// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Extension;
using Cratis.Cli.Extensions.Scaffolding;
using Cratis.Templating;

namespace Cratis.Cli.Extensions.for_NewExtensionCommand;

public class when_running_the_command : Specification
{
    string _templates = null!;
    string _output = null!;
    int _created;
    int _invalidName;
    int _invalidKind;
    int _noTemplate;
    int _dryRun;

    ExtensionScaffolder Scaffolder => new(new TemplatingEngine(Path.Combine(_templates, "store")));

    NewExtensionSettings Settings(string? name, Action<NewExtensionSettings>? configure = null, string? output = null)
    {
        var settings = new NewExtensionSettings { Name = name, Output = output ?? _output, TemplatePath = _templates, NoPrompts = true, Format = "json" };
        configure?.Invoke(settings);
        return settings;
    }

    async Task Because()
    {
        _templates = ExtensionTemplateFixture.Create();
        _output = Path.Combine(Path.GetTempPath(), "cratis-specs-extension-cmd", Guid.NewGuid().ToString("N"));
        _created = await NewExtensionCommand.Run(Settings("Stripe", settings => settings.Backend = "csharp"), null, Scaffolder, CancellationToken.None);
        _invalidName = await NewExtensionCommand.Run(Settings("not valid"), null, Scaffolder, CancellationToken.None);
        _invalidKind = await NewExtensionCommand.Run(Settings("Stripe", settings => settings.Kind = "bundle"), null, Scaffolder, CancellationToken.None);
        _noTemplate = await NewExtensionCommand.Run(Settings("Stripe", settings => settings.TemplatePath = Path.Combine(_templates, "package")), null, Scaffolder, CancellationToken.None);
        var dry = Path.Combine(_output, "dry");
        _dryRun = await NewExtensionCommand.Run(Settings("Stripe", settings => settings.DryRun = true, dry), null, Scaffolder, CancellationToken.None);
        DryRunWroteNothing = !Directory.Exists(dry) || Directory.GetFiles(dry, "*", SearchOption.AllDirectories).Length == 0;
    }

    bool DryRunWroteNothing { get; set; }

    [Fact] void should_succeed_for_a_valid_request() => _created.ShouldEqual(ExitCodes.Success);
    [Fact] void should_write_the_contract() => File.Exists(Path.Combine(_output, "contract.play")).ShouldBeTrue();
    [Fact] void should_report_an_invalid_name_as_a_request_that_could_not_run() => _invalidName.ShouldEqual(NewExtensionCommand.CouldNotRun);
    [Fact] void should_report_an_unknown_kind_as_a_request_that_could_not_run() => _invalidKind.ShouldEqual(NewExtensionCommand.CouldNotRun);
    [Fact] void should_report_a_missing_template_as_a_request_that_could_not_run() => _noTemplate.ShouldEqual(NewExtensionCommand.CouldNotRun);
    [Fact] void should_succeed_for_a_dry_run() => _dryRun.ShouldEqual(ExitCodes.Success);
    [Fact] void should_write_nothing_for_a_dry_run() => DryRunWroteNothing.ShouldBeTrue();
}
