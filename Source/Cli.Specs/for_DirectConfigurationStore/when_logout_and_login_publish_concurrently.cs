// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectConfigurationStore;

public class when_logout_and_login_publish_concurrently : given.a_configuration_store
{
    async Task Because()
    {
        await Login(First, "a", pause: false);
        var logout = First.Update(
        async (config, save) =>
        {
            FirstInside.SetResult();
            await ReleaseFirst.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var outcomes = await DirectCredentials.Logout(config.Direct!, [Entry("a")], _ => Server.Provider(), CancellationToken.None);
            save();
            return outcomes;
        },
        CancellationToken.None);
        await FirstInside.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var login = Login(Second, "b", pause: false);
        ReleaseFirst.SetResult();
        await Task.WhenAll(logout, login).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_keep_the_new_login_indexed() => Persisted.Direct!.Credentials.Select(entry => entry.Tenant!).ShouldContainOnly(["b"]);
    [Fact] void should_keep_the_new_login_secret() => Server.Stored.Keys.ShouldContainOnly([DirectTarget.Create("https://direct.example", "b").Key]);
    [Fact] void should_revoke_only_the_logged_out_token() => Server.Revoked.ShouldContainOnly(["refresh-a"]);
}
