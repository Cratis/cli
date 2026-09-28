// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Cli.Commands.Chronicle.Auth;
using Cratis.Cli.given;

namespace Cratis.Cli.for_LoginCommand;

[Collection(CliSpecsCollection.Name)]
public class when_validating_a_custom_certificate : a_temp_config_directory
{
    bool _clientAuthValid;
    bool _serverAuthValid;

    void Because()
    {
        _clientAuthValid = ValidateForPurpose("1.3.6.1.5.5.7.3.2");
        _serverAuthValid = ValidateForPurpose("1.3.6.1.5.5.7.3.1");
    }

    [Fact] void should_reject_client_auth_only_certificates() => _clientAuthValid.ShouldBeFalse();
    [Fact] void should_accept_server_auth_certificates() => _serverAuthValid.ShouldBeTrue();

    bool ValidateForPurpose(string purpose)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(purpose)], false));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var path = Path.Combine(_tempConfigHome, $"{purpose}.cer");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));

        return CertificateValidator.IsValid(certificate, path);
    }

    sealed class CertificateValidator : LoginCommand
    {
        internal static bool IsValid(X509Certificate2 certificate, string path) =>
            ValidateCertificate(certificate, SslPolicyErrors.RemoteCertificateChainErrors, path, null);
    }
}
