// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Observers.when_clearing_partition_quarantine.context;

namespace Cratis.Cli.Integration.Chronicle.for_Observers;

/// <summary>
/// Against a real observer with no failed partition of the given key the kernel reports the partition as not found.
/// A real quarantined partition is hard to set up, so the success path is covered by the Chronicle end-to-end spec.
/// </summary>
/// <param name="context">The <see cref="context"/> this specification runs in.</param>
[Collection(ChronicleCollection.Name)]
public class when_clearing_partition_quarantine(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public string ObserverId = string.Empty;
        public CliCommandResult Result = null!;

        async Task Because()
        {
            var listResult = await RunCliAsync("chronicle", "observers", "list", "--event-store", "system");
            var items = JsonDocument.Parse(listResult.StandardOutput).RootElement;
            ObserverId = items.EnumerateArray().First().GetProperty("id").GetString()!;

            Result = await RunCliAsync("chronicle", "observers", "clear-partition-quarantine", ObserverId, "test-partition-key", "--event-store", "system", "--yes");
        }
    }

    [Fact] void should_return_a_validation_error_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.ValidationError);

    [Fact] void should_say_the_partition_was_not_among_the_failures() =>
        Context.Result.StandardError.ShouldContain("is not among the failed partitions");
}
