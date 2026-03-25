// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.ReadModels;

/// <summary>
/// Lists read model occurrences (replay history).
/// </summary>
[CliCommand("occurrences", "List read model occurrences (replay history)", Branch = typeof(ChronicleBranch.ReadModels))]
[CliExample("chronicle", "read-models", "occurrences", "MyReadModelType")]
[LlmOutputAdvice("plain", "Use plain for consistency with other listing commands.")]
[LlmOption("<READ_MODEL_TYPE>", "string", "Read model type identifier (from 'cratis read-models list') (positional)")]
[LlmOption("--generation", "uint", "Read model type generation (default: 1)")]
public class GetReadModelOccurrencesCommand : ChronicleCommand<GetReadModelOccurrencesSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, GetReadModelOccurrencesSettings settings, string format)
    {
        var response = await services.ReadModels.GetOccurrences(new GetOccurrencesRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            Type = new ReadModelType
            {
                Identifier = settings.ReadModelType,
                Generation = settings.Generation
            }
        });

        var list = (response.Occurrences ?? []).ToList();

        OutputFormatter.Write(
            format,
            list,
            ["ObserverId", "Occurred", "Type", "Container", "RevertContainer"],
            occ =>
            [
                occ.ObserverId,
                occ.Occurred?.ToString() ?? string.Empty,
                $"{occ.Type?.Identifier}+{occ.Type?.Generation}",
                occ.ContainerName,
                occ.RevertContainerName
            ]);

        return ExitCodes.Success;
    }
}
