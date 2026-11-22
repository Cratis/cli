// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_disposing_the_http_client;

[Collection(CliSpecsCollection.Name)]
public class with_a_certificate_without_a_password : given.a_client_certificate
{
    IntPtr _certificateHandle;

    void Because()
    {
        CreateClient(null);
        var certificate = _handler.SslOptions.ClientCertificates[0];
        _client.Dispose();
        _certificateHandle = certificate.Handle;
    }

    [Fact]
    void should_dispose_the_certificate_with_the_client() => _certificateHandle.ShouldEqual(IntPtr.Zero);
}
