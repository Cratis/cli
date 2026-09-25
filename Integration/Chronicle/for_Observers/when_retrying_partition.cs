// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Observers.when_retrying_partition.context;

namespace Cratis.Cli.Integration.Chronicle.for_Observers;

/// <summary>
/// Against a real observer with no failed partition of the given key, this is what #186 was about: the command
/// used to await the call, ignore what came back, and print "Retry started" regardless. The key here was never a
/// real failure, so the kernel declines - and the point of the fix is that the command now says so instead of
/// claiming success for a retry that never started.
/// </summary>
/// <param name="context">The <see cref="context"/> this specification runs in.</param>
[Collection(ChronicleCollection.Name)]
public class when_retrying_partition(context context) : CliGiven<context>(context)
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

            Result = await RunCliAsync("chronicle", "observers", "retry-partition", ObserverId, "test-partition-key", "--event-store", "system", "--yes");
        }
    }

    [Fact] void should_not_return_success_exit_code() => Context.Result.ExitCode.ShouldNotEqual(ExitCodes.Success);

    [Fact] void should_return_a_validation_error_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.ValidationError);

    [Fact] void should_not_claim_a_retry_started() => Context.Result.StandardOutput.ShouldNotContain("Retry started");

    [Fact] void should_say_the_partition_was_not_among_the_failures() =>
        Context.Result.StandardError.ShouldContain("is not among the failed partitions");
}
