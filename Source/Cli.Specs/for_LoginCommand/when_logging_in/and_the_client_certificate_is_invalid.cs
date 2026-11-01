// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Auth;

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_client_certificate_is_invalid : given.a_login_command
{
    int _result;

    void Establish()
    {
        var path = Path.Combine(_tempConfigHome, "invalid.pfx");
        File.WriteAllText(path, "not a PKCS#12 certificate");
        _settings.Server = $"chronicle://localhost:35000?certificatePath={Uri.EscapeDataString(path)}&certificatePassword=private-certificate-password";
    }

    async Task Because() => _result = await ((ICommand<LoginSettings>)new LoginCommand()).ExecuteAsync(
        new CommandContext([], Substitute.For<IRemainingArguments>(), "login", null), _settings, CancellationToken.None);

    [Fact] void should_fail_with_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_store_a_login() => CliConfiguration.Load().GetCurrentContext().LoggedInUser.ShouldBeNull();
}
