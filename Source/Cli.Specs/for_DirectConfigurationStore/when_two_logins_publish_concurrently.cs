// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectConfigurationStore;

public class when_two_logins_publish_concurrently : given.a_configuration_store
{
    async Task Because()
    {
        var first = Login(First, "a", pause: true);
        await FirstInside.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = Login(Second, "b", pause: false);
        ReleaseFirst.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_keep_both_index_entries() => Persisted.Direct!.Credentials.Select(entry => entry.Tenant!).ShouldContainOnly(["a", "b"]);
    [Fact] void should_keep_the_other_configuration_sections() => Persisted.ActiveContext.ShouldEqual("concurrent-context");
    [Fact] void should_keep_both_secrets() => Server.Stored.Keys.ShouldContainOnly([DirectTarget.Create("https://direct.example", "a").Key, DirectTarget.Create("https://direct.example", "b").Key]);
}
