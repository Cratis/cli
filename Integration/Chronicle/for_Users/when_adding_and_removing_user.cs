// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Users.when_adding_and_removing_user.context;

namespace Cratis.Cli.Integration.Chronicle.for_Users;

[Collection(ChronicleCollection.Name)]
public class when_adding_and_removing_user(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        /// <summary>
        /// The username for this run. Making it unique keeps a user left behind by an earlier failed run
        /// from being taken for the one this run adds, which would both hide the read-after-write race and
        /// remove the wrong user.
        /// </summary>
        public readonly string Username = $"integration-test-user-{Guid.NewGuid():N}";

        public CliCommandResult AddResult = null!;
        public CliCommandResult RemoveResult = null!;
        public JsonElement ListedUser;

        async Task Because()
        {
            AddResult = await RunCliAsync("chronicle", "users", "add", Username, "integration-test@test.com", "TestP@ss123!");

            // 'users add' returns once the UserAdded event is appended; the list is read from a store a
            // kernel reactor projects that event into, so it catches up a moment later. Poll for it.
            ListedUser = await WaitForElementInList(
                $"User '{Username}'",
                user => user.TryGetProperty("username", out var username) && username.GetString() == Username,
                "chronicle",
                "users",
                "list");

            var userId = ListedUser.GetProperty("id").GetString()!;
            RemoveResult = await RunCliAsync("chronicle", "users", "remove", userId, "--yes");
        }
    }

    [Fact] void should_return_success_for_add() => Context.AddResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_added_message() => Context.AddResult.StandardOutput.ShouldContain("added");

    [Fact] void should_show_user_in_list() => Context.ListedUser.ValueKind.ShouldEqual(JsonValueKind.Object);

    [Fact] void should_return_success_for_remove() => Context.RemoveResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_removed_message() => Context.RemoveResult.StandardOutput.ShouldContain("removed");
}
