// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.ReadModels;

/// <summary>
/// Gets a single read model instance by key.
/// </summary>
[CliCommand("get", "Get a single read model instance by key", Branch = typeof(ChronicleBranch.ReadModels), DynamicCompletion = "read-models")]
[CliExample("chronicle", "read-models", "get", "MyReadModel", "abc-123")]
[CliExample("chronicle", "read-models", "get", "MyReadModel", "abc-123", "-o", "json")]
[LlmOutputAdvice("json", "JSON contains the full read model document. Use JSON for structured parsing.")]
[LlmOption("<READ_MODEL>", "string", "Read model container name (from 'cratis read-models list') (positional)")]
[LlmOption("<KEY>", "string", "Read model instance key (typically an event source ID) (positional)")]
public class GetReadModelByKeyCommand : ChronicleCommand<ReadModelKeySettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ReadModelKeySettings settings, string format)
    {
        var response = await services.ReadModels.GetInstanceByKey(new GetInstanceByKeyRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ReadModelIdentifier = settings.ReadModel,
            EventSequenceId = settings.EventSequenceId,
            ReadModelKey = settings.Key
        });

        if (string.IsNullOrEmpty(response.ReadModel))
        {
            OutputFormatter.WriteError(format, $"No instance found for key '{settings.Key}' in read model '{settings.ReadModel}'", errorCode: ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                response.ReadModel,
                response.ProjectedEventsCount,
                response.LastHandledEventSequenceNumber
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]ProjectedEvents:[/] {data.ProjectedEventsCount}");
                AnsiConsole.MarkupLine($"[bold]LastHandled#:[/]    {data.LastHandledEventSequenceNumber}");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine(data.ReadModel);
            });

        return ExitCodes.Success;
    }
}
