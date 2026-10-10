// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Connections;
using Cratis.Cli.Commands.Chronicle.Auth;
using Cratis.Cli.given;

namespace Cratis.Cli.for_LoginCommand.given;

public class a_client_certificate : a_temp_config_directory
{
    protected X509Certificate2 _certificate = null!;
    protected X509Certificate2 _otherCertificate = null!;
    protected HttpClient _client = null!;
    protected SocketsHttpHandler _handler = null!;

    void Establish()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        _otherCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
    }

    protected void CreateClient(string? password)
    {
        var path = Path.Combine(_tempConfigHome, "client.pfx");
        File.WriteAllBytes(path, _certificate.Export(X509ContentType.Pkcs12, password));
        var connectionString = new ChronicleConnectionString($"chronicle://localhost:35000?skipTlsValidation=false&certificatePath={Uri.EscapeDataString(path)}&certificatePassword={Uri.EscapeDataString(password ?? string.Empty)}");
        _client = new ProductionLogin().CreateClient(connectionString);
        var handler = (HttpMessageHandler)typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_client)!;
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        _handler = (SocketsHttpHandler)handler;
    }

    void Destroy()
    {
        _client?.Dispose();
        _certificate.Dispose();
        _otherCertificate.Dispose();
    }

    sealed class ProductionLogin : LoginCommand
    {
        public HttpClient CreateClient(ChronicleConnectionString connectionString) => CreateHttpClient(connectionString);
    }
}
