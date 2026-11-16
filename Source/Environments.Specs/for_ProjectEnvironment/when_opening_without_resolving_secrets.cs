// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

[Collection(EnvironmentSpecsCollection.Name)]
public class when_opening_without_resolving_secrets : a_temp_project
{
    IPassphraseSource _passphrase;
    string? _value;
    string? _missing;

    void Establish()
    {
        _passphrase = Substitute.For<IPassphraseSource>();
        var document = new EnvironmentDocument();
        document.Config["plain"] = ConfigEntry.Plain("visible");
        _environments.Save("production", document);
    }

    async Task Because()
    {
        var environment = ProjectEnvironment.Open(_environments, "production", _passphrase);
        _value = await environment.GetValueAsync("plain");
        _missing = await environment.GetValueAsync("missing");
    }

    [Fact] void should_read_plain_configuration() => _value.ShouldEqual("visible");
    [Fact] void should_return_null_for_a_missing_key() => _missing.ShouldBeNull();
    [Fact] void should_not_request_the_passphrase() => _passphrase.DidNotReceive().GetPassphrase();
    [Fact] void should_not_request_a_new_passphrase() => _passphrase.DidNotReceive().GetNewPassphrase();
}
