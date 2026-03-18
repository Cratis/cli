// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Known AI provider identifiers for <see cref="ChatClientFactory"/>.
/// </summary>
public static class ChatClientProviders
{
    /// <summary>OpenAI provider identifier.</summary>
    public const string OpenAI = "openai";

    /// <summary>Anthropic provider identifier.</summary>
    public const string Anthropic = "anthropic";

    /// <summary>Ollama provider identifier.</summary>
    public const string Ollama = "ollama";

    /// <summary>Azure OpenAI provider identifier.</summary>
    public const string AzureOpenAI = "azure-openai";
}
