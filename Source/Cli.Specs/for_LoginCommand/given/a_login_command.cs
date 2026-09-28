// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Connections;
using Cratis.Cli.Commands.Chronicle.Auth;
using Cratis.Cli.given;

namespace Cratis.Cli.for_LoginCommand.given;

/// <summary>
/// A login command with a local configuration and an in-memory OAuth endpoint.
/// </summary>
public class a_login_command : a_temp_config_directory
{
    protected LoginSettings _settings = null!;
    protected LoginCommandForSpecs _command = null!;
    protected TokenEndpoint _endpoint = null!;

    void Establish()
    {
        new CliConfiguration
        {
            ActiveContext = "production",
            Contexts = new Dictionary<string, CliContext>
            {
                ["production"] = new() { Server = "chronicle://production:35000", ClientId = "old-client", ClientSecret = "old-secret" },
                ["development"] = new() { Server = "chronicle://development:35000" }
            }
        }.Save();
        _settings = new LoginSettings { Username = "admin", Secret = "password", Output = OutputFormats.JsonCompact };
        _endpoint = new TokenEndpoint();
        _command = new LoginCommandForSpecs(_endpoint);
    }

    protected Task<int> Execute() => ((ICommand<LoginSettings>)_command).ExecuteAsync(
        new CommandContext([], Substitute.For<IRemainingArguments>(), "login", null), _settings, CancellationToken.None);

    /// <summary>
    /// Exposes an in-memory HTTP transport for OAuth without changing the production command registration.
    /// </summary>
    /// <param name="endpoint">The in-memory OAuth endpoint.</param>
    public sealed class LoginCommandForSpecs(TokenEndpoint endpoint) : LoginCommand
    {
        /// <inheritdoc/>
        protected override HttpClient CreateHttpClient(ChronicleConnectionString connectionString) => new(endpoint, disposeHandler: false) { Timeout = endpoint.Timeout };
    }

    /// <summary>
    /// In-memory OAuth token endpoint.
    /// </summary>
    public sealed class TokenEndpoint : HttpMessageHandler
    {
        /// <summary>
        /// Gets or sets the response body.
        /// </summary>
        public string Body { get; set; } = "{\"access_token\":\"user-token\",\"expires_in\":3600}";

        /// <summary>
        /// Gets or sets the response content when testing streaming responses.
        /// </summary>
        public HttpContent? ResponseContent { get; set; }

        /// <summary>
        /// Gets or sets the HTTP request timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

        /// <summary>
        /// Gets or sets the response status.
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        /// <summary>
        /// Gets the requested URI.
        /// </summary>
        public Uri? RequestUri { get; private set; }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = ResponseContent ?? new StringContent(Body) });
        }
    }
}
