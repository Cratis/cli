// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Cli.for_LoginCommand.when_creating_the_http_client;

[Collection(CliSpecsCollection.Name)]
public class with_a_password_protected_certificate : given.a_client_certificate
{
    void Because() => CreateClient("client-password");

    [Fact] void should_attach_the_client_certificate() => _handler.SslOptions.ClientCertificates![0].GetCertHashString().ShouldEqual(_certificate.GetCertHashString());
    [Fact] void should_keep_the_private_key() => ((X509Certificate2)_handler.SslOptions.ClientCertificates![0]).HasPrivateKey.ShouldBeTrue();
    [Fact] void should_disable_redirects() => _handler.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_accept_a_valid_server_certificate() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, null, null, SslPolicyErrors.None).ShouldBeTrue();
    [Fact] void should_accept_chain_errors_for_the_pinned_certificate() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, _certificate, null, SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeTrue();
    [Fact] void should_reject_chain_errors_for_another_certificate() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, _otherCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeFalse();
    [Fact] void should_reject_a_name_mismatch_even_for_the_pin() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, _certificate, null, SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeFalse();
}
