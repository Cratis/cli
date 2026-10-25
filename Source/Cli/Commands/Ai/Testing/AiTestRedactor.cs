// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Ai.Testing;

internal sealed partial class AiTestRedactor(IEnumerable<string> secrets)
{
    readonly string[] _secrets = [.. secrets.Where(value => !string.IsNullOrEmpty(value)).Distinct().OrderByDescending(value => value.Length)];

    [GeneratedRegex("KEY|TOKEN|SECRET", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex SecretName { get; }

    [GeneratedRegex(@"(?i)(?:\bBearer\s+[^\s""',}]+|\b(?:sk-|ghp_)[a-z0-9_-]+|\b(?:[a-z0-9_]*(?:token|secret|key)[a-z0-9_]*)\s*[:=]\s*[""']?[^\s""',}]+)", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Tokens { get; }

    internal static AiTestRedactor FromEnvironment() => new(Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
        .Where(entry => SecretName.IsMatch((string)entry.Key))
        .Select(entry => entry.Value?.ToString() ?? string.Empty));

    internal string Redact(string text)
    {
        foreach (var secret in _secrets) text = text.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
        return Tokens.Replace(text, "[REDACTED]");
    }
}
