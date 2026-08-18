// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Applications.when_adding_and_removing_application.context;

namespace Cratis.Cli.Integration.Chronicle.for_Applications;

[Collection(ChronicleCollection.Name)]
public class when_adding_and_removing_application(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        /// <summary>
        /// The client identifier for this run. Making it unique keeps an application left behind by an
        /// earlier failed run from being taken for the one this run adds - the server treats adding a
        /// client identifier it already knows as a no-op, so a leftover would make the add do nothing.
        /// </summary>
        public readonly string ClientId = $"integration-test-app-{Guid.NewGuid():N}";

        public CliCommandResult AddResult = null!;
        public CliCommandResult RemoveResult = null!;
        public JsonElement ListedApplication;

        async Task Because()
        {
            AddResult = await RunCliAsync("chronicle", "applications", "add", ClientId, "integration-test-secret");

            // 'applications add' returns once the ApplicationAdded event is appended; the list is read from
            // a store a kernel reactor projects that event into, so it catches up a moment later. Poll for it.
            ListedApplication = await WaitForElementInList(
                $"Application '{ClientId}'",
                application => application.TryGetProperty("clientId", out var clientId) && clientId.GetString() == ClientId,
                "chronicle",
                "applications",
                "list");

            var appId = ListedApplication.GetProperty("id").GetString()!;
            RemoveResult = await RunCliAsync("chronicle", "applications", "remove", appId);
        }
    }

    [Fact] void should_return_success_for_add() => Context.AddResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_added_message() => Context.AddResult.StandardOutput.ShouldContain("added");

    [Fact] void should_show_application_in_list() => Context.ListedApplication.ValueKind.ShouldEqual(JsonValueKind.Object);

    [Fact] void should_return_success_for_remove() => Context.RemoveResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_removed_message() => Context.RemoveResult.StandardOutput.ShouldContain("removed");
}
