// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Cli.Commands.Chronicle.Auth;

namespace Cratis.Cli.for_LoginCommand.when_creating_the_http_client;

public class without_a_certificate : Specification
{
    HttpClient _client = null!;
    SocketsHttpHandler _handler = null!;

    void Because()
    {
        _client = new LoginWithProductionTransport().CreateClient();
        _handler = (SocketsHttpHandler)typeof(HttpMessageInvoker)
            .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_client)!;
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_disable_automatic_redirects() => _handler.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_accept_valid_server_certificates() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, null, null, System.Net.Security.SslPolicyErrors.None).ShouldBeTrue();
    [Fact] void should_reject_chain_errors_without_a_pin() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, null, null, System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeFalse();
    [Fact] void should_leave_revocation_checking_disabled() => _handler.SslOptions.CertificateRevocationCheckMode.ShouldEqual(System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck);

    sealed class LoginWithProductionTransport : LoginCommand
    {
        public HttpClient CreateClient() => CreateHttpClient(new ChronicleConnectionString("chronicle://localhost:35000?skipTlsValidation=false"));
    }
}
