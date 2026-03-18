// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ClientModel;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Creates <see cref="IChatClient"/> instances for different AI providers.
/// </summary>
public static class ChatClientFactory
{
    /// <summary>
    /// Creates an <see cref="IChatClient"/> for the specified provider and model.
    /// </summary>
    /// <param name="provider">The provider identifier (openai, anthropic, ollama, azure-openai).</param>
    /// <param name="model">The model name.</param>
    /// <param name="apiKey">The API key (may be a <c>$ENV_VAR</c> reference).</param>
    /// <param name="baseUrl">Optional base URL override.</param>
    /// <param name="tools">Optional tools to enable for function calling.</param>
    /// <returns>A configured <see cref="IChatClient"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="provider"/> is not a recognized provider identifier.</exception>
    public static IChatClient Create(string provider, string model, string apiKey, string? baseUrl, IReadOnlyList<AITool>? tools = null)
    {
        var resolvedKey = ResolveApiKey(apiKey);

#pragma warning disable CA2000 // innerClient ownership is transferred to ChatClientBuilder
        var innerClient = provider.ToLowerInvariant() switch
        {
            Providers.OpenAI => CreateOpenAI(resolvedKey, model, baseUrl),
            Providers.Anthropic => CreateAnthropic(resolvedKey, model, baseUrl),
            Providers.AzureOpenAI => CreateAzureOpenAI(resolvedKey, model, baseUrl),
            Providers.Ollama => CreateOllama(model, baseUrl),
            _ => throw new ArgumentException($"Unknown AI provider: {provider}. Supported: openai, anthropic, ollama, azure-openai")
        };
#pragma warning restore CA2000

        var builder = new ChatClientBuilder(innerClient);

        if (tools is { Count: > 0 })
        {
            builder.UseFunctionInvocation();
        }

        return builder.Build();
    }

    /// <summary>
    /// Resolves an API key, dereferencing environment variable references (prefixed with <c>$</c>).
    /// </summary>
    /// <param name="apiKey">The raw API key or <c>$ENV_VAR</c> reference.</param>
    /// <returns>The resolved API key string, or empty if null/empty.</returns>
    public static string ResolveApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return string.Empty;
        }

        if (apiKey.StartsWith('$'))
        {
            var envVarName = apiKey[1..];
            return Environment.GetEnvironmentVariable(envVarName) ?? string.Empty;
        }

        return apiKey;
    }

    /// <summary>
    /// Returns the default environment variable name for API keys per provider.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <returns>The default environment variable name, or null for providers that don't use API keys.</returns>
    public static string? DefaultEnvVar(string provider) => provider.ToLowerInvariant() switch
    {
        Providers.OpenAI or Providers.AzureOpenAI => "OPENAI_API_KEY",
        Providers.Anthropic => "ANTHROPIC_API_KEY",
        _ => null
    };

    /// <summary>
    /// Returns the default model for a given provider.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <returns>The default model name for the provider.</returns>
    public static string DefaultModel(string provider) => provider.ToLowerInvariant() switch
    {
        Providers.OpenAI or Providers.AzureOpenAI => "gpt-4o",
        Providers.Anthropic => "claude-sonnet-4-20250514",
        Providers.Ollama => "llama3.1",
        _ => "gpt-4o"
    };

    static IChatClient CreateOpenAI(string apiKey, string model, string? baseUrl)
    {
        var options = baseUrl is not null
            ? new OpenAIClientOptions { Endpoint = new Uri(baseUrl) }
            : null;

        var client = options is not null
            ? new OpenAIClient(new ApiKeyCredential(apiKey), options)
            : new OpenAIClient(apiKey);

        return client.GetChatClient(model).AsIChatClient();
    }

#pragma warning disable IDE0060 // baseUrl reserved for future Anthropic endpoint override
    static IChatClient CreateAnthropic(string apiKey, string model, string? baseUrl)
#pragma warning restore IDE0060
    {
#pragma warning disable CA2000 // AnthropicClient lifetime is managed by the IChatClient wrapper
        var client = new Anthropic.AnthropicClient(new Anthropic.Core.ClientOptions { ApiKey = apiKey });
#pragma warning restore CA2000
        return client.AsIChatClient(model);
    }

    static IChatClient CreateAzureOpenAI(string apiKey, string model, string? baseUrl)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentException("Azure OpenAI requires a base URL (your Azure endpoint).");
        }

        var client = new Azure.AI.OpenAI.AzureOpenAIClient(
            new Uri(baseUrl),
            new ApiKeyCredential(apiKey));

        return client.GetChatClient(model).AsIChatClient();
    }

    static OllamaApiClient CreateOllama(string model, string? baseUrl)
    {
        var uri = new Uri(baseUrl ?? "http://localhost:11434");
        return new OllamaApiClient(uri, model);
    }

    /// <summary>
    /// Known provider identifiers.
    /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible — kept for API compatibility
    public static class Providers
#pragma warning restore CA1034
    {
        /// <inheritdoc cref="ChatClientProviders.OpenAI"/>
        public const string OpenAI = ChatClientProviders.OpenAI;

        /// <inheritdoc cref="ChatClientProviders.Anthropic"/>
        public const string Anthropic = ChatClientProviders.Anthropic;

        /// <inheritdoc cref="ChatClientProviders.Ollama"/>
        public const string Ollama = ChatClientProviders.Ollama;

        /// <inheritdoc cref="ChatClientProviders.AzureOpenAI"/>
        public const string AzureOpenAI = ChatClientProviders.AzureOpenAI;
    }
}
