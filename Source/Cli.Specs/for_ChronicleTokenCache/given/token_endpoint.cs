// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Cli.for_ChronicleTokenCache.given;

public sealed class token_endpoint : IAsyncDisposable
{
    readonly string _marker = Guid.NewGuid().ToString("N");
    readonly RSA _key = RSA.Create(2048);
    readonly X509Certificate2 _certificate;
    readonly WebApplication _app;

    public token_endpoint()
    {
        var request = new CertificateRequest("CN=localhost", _key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(_certificate)));
        _app = builder.Build();
        _app.MapPost("/connect/token", async context =>
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            Requests++;
            OnlyClientCredentials &= form["grant_type"] == "client_credentials" && !form.ContainsKey("refresh_token");
            await context.Response.WriteAsJsonAsync(new { access_token = $"{_marker}-{Requests}", expires_in = 3600 }, context.RequestAborted);
        });
    }

    public int Requests { get; private set; }
    public bool OnlyClientCredentials { get; private set; } = true;
    public string Server => new Uri(_app.Urls.Single()).Authority;
    public string Endpoint => $"https://{Server}/connect/token";

    public bool IsCurrentToken(string token) => token == $"{_marker}-{Requests}";

    public async Task Start()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await _app.StartAsync(deadline.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        _certificate.Dispose();
        _key.Dispose();
    }
}
