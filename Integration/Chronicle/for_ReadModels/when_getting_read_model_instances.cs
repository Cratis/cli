// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_ReadModels.when_getting_read_model_instances.context;

namespace Cratis.Cli.Integration.Chronicle.for_ReadModels;

[Collection(ChronicleCollection.Name)]
public class when_getting_read_model_instances(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult ListResult = null!;
        public CliCommandResult InstancesResult = null!;
        public bool HasReadModels;

        async Task Because()
        {
            ListResult = await RunCliAsync("chronicle", "read-models", "list", "--event-store", "system");
            var items = JsonDocument.Parse(ListResult.StandardOutput).RootElement;
            HasReadModels = items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0;

            if (HasReadModels)
            {
                var containerName = items.EnumerateArray().First().GetProperty("containerName").GetString()!;
                InstancesResult = await RunCliAsync("chronicle", "read-models", "instances", containerName, "--event-store", "system");
            }
        }
    }

    [Fact] void should_return_success_for_list() => Context.ListResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_list_output() => (Context.ListResult.StandardOutput.Length > 0).ShouldBeTrue();

    [Fact] void should_have_no_list_errors() => Context.ListResult.StandardError.ShouldEqual(string.Empty);
}
