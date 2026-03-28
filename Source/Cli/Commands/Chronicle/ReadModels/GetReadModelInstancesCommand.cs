// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.Commands.Chronicle.ReadModels;

/// <summary>
/// Lists read model instances with pagination.
/// </summary>
[CliCommand("instances", "List read model instances", Branch = typeof(ChronicleBranch.ReadModels), DynamicCompletion = "read-models")]
[CliExample("chronicle", "read-models", "instances", "MyReadModel")]
[CliExample("chronicle", "read-models", "instances", "MyReadModel", "--page", "2")]
[LlmOutputAdvice("plain", "Both formats are comparable; use plain for consistency.")]
[LlmOption("<READ_MODEL>", "string", "Read model container name (positional)")]
[LlmOption("--page", "int", "Page number, 0-based (default: 0)")]
[LlmOption("--page-size", "int", "Items per page (default: 20)")]
public class GetReadModelInstancesCommand : ChronicleCommand<GetReadModelInstancesSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, GetReadModelInstancesSettings settings, string format)
    {
        GetInstancesResponse response;
        try
        {
            response = await services.ReadModels.GetInstances(new GetInstancesRequest
            {
                EventStore = settings.ResolveEventStore(),
                Namespace = settings.ResolveNamespace(),
                ReadModel = settings.ReadModel,
                Page = settings.Page,
                PageSize = settings.PageSize
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            OutputFormatter.WriteError(
                format,
                $"Read model '{settings.ReadModel}' not found",
                "Use 'cratis chronicle read-models list' to see available read models",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var instances = (response.Instances ?? []).ToList();

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            OutputFormatter.WriteObject(
                format,
                new
                {
                    response.TotalCount,
                    response.Page,
                    response.PageSize,
                    Instances = instances
                });
        }
        else if (instances.Count == 0)
        {
            Console.WriteLine($"Total: {response.TotalCount} | Page: {response.Page} | PageSize: {response.PageSize}");
            Console.WriteLine("(no instances)");
        }
        else
        {
            // Parse all instances as JsonElement to extract dynamic column names
            var parsed = new List<JsonElement>();
            foreach (var raw in instances)
            {
                try
                {
                    parsed.Add(JsonSerializer.Deserialize<JsonElement>(raw));
                }
                catch
                {
                    // If not valid JSON, skip structured rendering
                }
            }

            if (parsed.Count == 0)
            {
                // Fallback to plain output
                Console.WriteLine($"Total: {response.TotalCount} | Page: {response.Page} | PageSize: {response.PageSize}");
                foreach (var instance in instances)
                {
                    Console.WriteLine(instance);
                }
            }
            else
            {
                // Collect columns from first element
                var columns = parsed[0].EnumerateObject().Select(p => p.Name).ToArray();

                OutputFormatter.Write(
                    format,
                    parsed,
                    columns,
                    element => columns.Select(col =>
                    {
                        if (element.TryGetProperty(col, out var prop))
                        {
                            return prop.ValueKind == JsonValueKind.String
                                ? prop.GetString() ?? string.Empty
                                : prop.ToString();
                        }
                        return string.Empty;
                    }).ToArray());
            }
        }

        return ExitCodes.Success;
    }
}
