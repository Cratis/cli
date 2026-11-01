// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// The prompt flow of <c language="csharp">cratis prologue start</c> for a system made of several services — walks
/// the user through each named service and its own sources, then the shared OpenTelemetry proxy and the output.
/// </summary>
public static class PrologueServicesWizard
{
    /// <summary>
    /// Collects the wizard input for a system made of several named services by prompting in the terminal.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> the prompts observe.</param>
    /// <returns>The collected <see cref="PrologueWizardInput"/>, with every source on a named service.</returns>
    public static async Task<PrologueWizardInput> Collect(CancellationToken cancellationToken)
    {
        var services = new List<ServiceSourceInput>();
        var capturesTelemetry = false;
        do
        {
            var (service, telemetry) = await CollectService(services, cancellationToken);
            services.Add(service);
            capturesTelemetry |= telemetry;
        }
        while (await AnsiConsole.ConfirmAsync("Add another service?", defaultValue: true, cancellationToken));

        var openTelemetry = capturesTelemetry ? await PrologueWizard.CollectOpenTelemetry(askServiceNames: false, cancellationToken) : null;
        var output = await PrologueWizard.CollectOutput(cancellationToken);

        return new(Guid.NewGuid(), [], [], null, openTelemetry, output, services);
    }

    static async Task<(ServiceSourceInput Service, bool CapturesTelemetry)> CollectService(List<ServiceSourceInput> existing, CancellationToken cancellationToken)
    {
        OutputFormatter.WriteSection($"Service #{existing.Count + 1}");
        var name = await AnsiConsole.PromptAsync(
            new TextPrompt<string>("Service name (identifies the service in captures and names its proxy cluster):")
                .Validate(candidate => PrologueConfigurationBuilder.IsValidServiceName(candidate, existing.Select(service => service.Name))
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Use letters, digits, '-' or '_', and a name no other service has")),
            cancellationToken);
        name = name.Trim();

        var sources = await PrologueWizard.CollectSources($"Which sources belong to '{name}'?", cancellationToken);
        var sqlServer = sources.Contains(PrologueWizard.SqlServerChoice) ? await PrologueWizard.CollectSqlServer(name, cancellationToken) : [];
        var postgres = sources.Contains(PrologueWizard.PostgresChoice) ? await PrologueWizard.CollectPostgres(name, cancellationToken) : [];
        var takenBasePaths = existing.Where(service => service.Api is not null).Select(service => PrologueConfigurationBuilder.NormalizeBasePath(service.Api!.BasePath)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var api = sources.Contains(PrologueWizard.ApiChoice)
            ? await PrologueWizard.CollectApi($"/{name}", basePath => !takenBasePaths.Contains(basePath), cancellationToken)
            : null;

        var capturesTelemetry = sources.Contains(PrologueWizard.OpenTelemetryChoice);
        var telemetryServiceNames = capturesTelemetry
            ? await AnsiConsole.PromptAsync(
                new TextPrompt<string>($"OpenTelemetry service.name values of '{name}' (comma separated, empty for '{name}'):").AllowEmpty(),
                cancellationToken)
            : string.Empty;

        return (new(name, sqlServer, postgres, api, PrologueConfigurationBuilder.ParseList(telemetryServiceNames)), capturesTelemetry);
    }
}
