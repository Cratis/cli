// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Represents a named connection context that stores server, event store, namespace, credentials, and login session.
/// </summary>
public class CliContext
{
    /// <summary>
    /// Gets or sets the server connection string.
    /// </summary>
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the default event store name.
    /// </summary>
    public string? EventStore { get; set; }

    /// <summary>
    /// Gets or sets the default namespace name.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the client ID for client_credentials authentication.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for client_credentials authentication.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the management port for the HTTP API and token endpoint.
    /// </summary>
    public int? ManagementPort { get; set; }

    /// <summary>
    /// Gets or sets the cached access token from a previous login.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiry time in UTC ISO-8601 format.
    /// </summary>
    public string? TokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets the username of the currently logged-in user.
    /// </summary>
    public string? LoggedInUser { get; set; }

    /// <summary>
    /// Gets or sets the AI provider name (e.g. "openai", "anthropic", "ollama", "azure-openai").
    /// </summary>
    public string? AiProvider { get; set; }

    /// <summary>
    /// Gets or sets the AI model name (e.g. "gpt-4o", "claude-sonnet-4-20250514").
    /// </summary>
    public string? AiModel { get; set; }

    /// <summary>
    /// Gets or sets the AI API key. Prefix with <c>$</c> to reference an environment variable.
    /// </summary>
    public string? AiApiKey { get; set; }

    /// <summary>
    /// Gets or sets the base URL for the AI provider (for Ollama, Azure OpenAI, or custom endpoints).
    /// </summary>
    public string? AiBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the user has acknowledged that data will be sent to an external AI provider.
    /// </summary>
    public bool? AiDataConsent { get; set; }
}
