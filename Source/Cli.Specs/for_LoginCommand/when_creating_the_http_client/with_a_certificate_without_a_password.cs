// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Cli.for_LoginCommand.when_creating_the_http_client;

[Collection(CliSpecsCollection.Name)]
public class with_a_certificate_without_a_password : given.a_client_certificate
{
    void Because() => CreateClient(null);

    [Fact] void should_attach_the_client_certificate() => _handler.SslOptions.ClientCertificates![0].GetCertHashString().ShouldEqual(_certificate.GetCertHashString());
    [Fact] void should_keep_the_private_key() => ((X509Certificate2)_handler.SslOptions.ClientCertificates![0]).HasPrivateKey.ShouldBeTrue();
    [Fact] void should_dispose_the_certificate_with_the_client()
    {
        var certificate = _handler.SslOptions.ClientCertificates![0];
        _client.Dispose();
        certificate.Handle.ShouldEqual(IntPtr.Zero);
    }
}
