// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Cli.Commands.Chronicle.Auth;

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_client_certificate_password_is_wrong : given.a_login_command
{
    int _result;
    StringWriter _output = null!;
    TextWriter _previousOutput = null!;

    void Establish()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var path = Path.Combine(_tempConfigHome, "protected.pfx");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, "correct-password"));
        _settings.Server = $"chronicle://localhost:35000?certificatePath={Uri.EscapeDataString(path)}&certificatePassword=incorrect-private-password";
        _previousOutput = Console.Error;
        _output = new StringWriter();
        Console.SetError(_output);
    }

    async Task Because() => _result = await ((ICommand<LoginSettings>)new LoginCommand()).ExecuteAsync(
        new CommandContext([], Substitute.For<IRemainingArguments>(), "login", null), _settings, CancellationToken.None);

    protected override void CleanUp()
    {
        Console.SetError(_previousOutput);
        _output.Dispose();
        base.CleanUp();
    }

    [Fact] void should_fail_with_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_explain_the_certificate_failure() => _output.ToString().ShouldContain("client certificate is invalid or its password is incorrect");
    [Fact] void should_not_disclose_the_password() => _output.ToString().ShouldNotContain("incorrect-private-password");
    [Fact] void should_not_include_a_stack_trace() => _output.ToString().ShouldNotContain("CryptographicException");
    [Fact] void should_not_store_a_login() => CliConfiguration.Load().GetCurrentContext().LoggedInUser.ShouldBeNull();
}
