// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

public class when_changing_the_passphrase : given.a_temp_project
{
    string _saltBefore;
    string _saltAfter;
    string _resolved;
    Exception _oldPassphrase;
    int _moved;

    async Task Because()
    {
        var environment = ProjectEnvironment.Create(_environments, "production", null, PassphraseSources.Fixed("old"), FastKdf);
        await environment.SetAsync("smtp:password", "the value", secret: true);
        await environment.SetAsync("plain", "visible", secret: false);
        _saltBefore = _environments.Load("production").EncryptionSalt;

        var reopened = ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("old", "new"));
        _moved = await reopened.ChangePassphraseAsync();
        _saltAfter = _environments.Load("production").EncryptionSalt;

        var withNew = ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("new"));
        _resolved = await withNew.GetValueAsync("smtp:password");
        var withOld = ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("old"));
        _oldPassphrase = await Catch.Exception(() => withOld.GetValueAsync("smtp:password"));
    }

    [Fact] void should_move_the_secret() => _moved.ShouldEqual(1);
    [Fact] void should_resolve_with_the_new_passphrase() => _resolved.ShouldEqual("the value");
    [Fact] void should_reject_the_old_passphrase() => _oldPassphrase.ShouldBeOfExactType<InvalidPassphraseException>();
    [Fact] void should_use_a_new_salt() => _saltAfter.ShouldNotEqual(_saltBefore);
    [Fact] void should_keep_plain_values() => _environments.Load("production").Config["plain"].Value.ShouldEqual("visible");
}
