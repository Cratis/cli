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
    bool _noEnhancedKeyUsageValid;

    void Because()
    {
        _clientAuthValid = ValidateForPurpose("1.3.6.1.5.5.7.3.2");
        _serverAuthValid = ValidateForPurpose("1.3.6.1.5.5.7.3.1");
        _noEnhancedKeyUsageValid = ValidateWithoutEnhancedKeyUsage();
    }

    [Fact] void should_reject_client_auth_only_certificates() => _clientAuthValid.ShouldBeFalse();
    [Fact] void should_accept_server_auth_certificates() => _serverAuthValid.ShouldBeTrue();
    [Fact] void should_accept_certificates_without_enhanced_key_usage() => _noEnhancedKeyUsageValid.ShouldBeTrue();

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

    bool ValidateWithoutEnhancedKeyUsage()
    {
        using var rootKey = RSA.Create(2048);
        var rootRequest = new CertificateRequest("CN=Custom root", rootKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));
        using var root = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));

        using var leafKey = RSA.Create(2048);
        var leafRequest = new CertificateRequest("CN=localhost", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        using var leaf = leafRequest.Create(root, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(30), RandomNumberGenerator.GetBytes(16));
        var rootPath = Path.Combine(_tempConfigHome, "custom-root.cer");
        File.WriteAllBytes(rootPath, root.Export(X509ContentType.Cert));

        return CertificateValidator.IsValid(leaf, rootPath);
    }

    sealed class CertificateValidator : LoginCommand
    {
        internal static bool IsValid(X509Certificate2 certificate, string path) =>
            ValidateCertificate(certificate, SslPolicyErrors.RemoteCertificateChainErrors, path, null);
    }
}
