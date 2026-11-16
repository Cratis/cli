// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Cratis.Environments;

/// <summary>
/// Reads and writes the environment file format.
/// </summary>
public static class EnvironmentYaml
{
    static readonly HashSet<string> _reserved = [with(StringComparer.OrdinalIgnoreCase), "true", "false", "null", "yes", "no", "on", "off", "~"];

    /// <summary>
    /// Parses an environment file.
    /// </summary>
    /// <param name="yaml">The file contents.</param>
    /// <returns>The document.</returns>
    /// <exception cref="EnvironmentException">Thrown when the file is not valid.</exception>
    public static EnvironmentDocument Parse(string yaml)
    {
        var document = new EnvironmentDocument();
        if (ReadRoot(yaml) is { } root)
        {
            foreach (var (keyNode, valueNode) in root.Children)
            {
                ReadProperty(document, Scalar(keyNode), valueNode);
            }
        }

        return document;
    }

    /// <summary>
    /// Serializes an environment document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>The file contents.</returns>
    public static string Write(EnvironmentDocument document)
    {
        var root = new YamlMappingNode
        {
            { "secretsProvider", Plain(document.SecretsProvider) }
        };

        if (document.EncryptionSalt is not null)
        {
            root.Add("encryptionSalt", Plain(document.EncryptionSalt));
        }

        if (document.Kdf is not null || document.EncryptionCheck is not null)
        {
            var encryption = new YamlMappingNode();
            if (document.Kdf is not null)
            {
                encryption.Add("kdf", Plain(document.Kdf.Algorithm));
                encryption.Add("iterations", Plain(document.Kdf.Iterations.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            if (document.EncryptionCheck is not null)
            {
                encryption.Add("check", Plain(document.EncryptionCheck));
            }

            root.Add("encryption", encryption);
        }

        var config = new YamlMappingNode();
        foreach (var (key, entry) in document.Config)
        {
            config.Add(Plain(key), entry.Kind switch
            {
                ConfigEntryKind.Secure => new YamlMappingNode { { "secure", Plain(entry.Value) } },
                ConfigEntryKind.Reference => new YamlMappingNode { { "ref", Plain(entry.Value) } },
                _ => Plain(entry.Value)
            });
        }

        if (config.Children.Count > 0)
        {
            root.Add("config", config);
        }

        var writer = new StringWriter();
        new YamlStream(new YamlDocument(root)).Save(writer, assignAnchors: false);
        var text = writer.ToString().Replace("...\n", string.Empty, StringComparison.Ordinal).Replace("...\r\n", string.Empty, StringComparison.Ordinal);
        return text.TrimEnd() + Environment.NewLine;
    }

    static YamlMappingNode? ReadRoot(string yaml)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (YamlException ex)
        {
            throw new EnvironmentException($"The environment file is not valid YAML: {ex.Message}", ex);
        }

        return stream.Documents.Count == 0 ? null : stream.Documents[0].RootNode as YamlMappingNode;
    }

    static void ReadProperty(EnvironmentDocument document, string? key, YamlNode value)
    {
        switch (key)
        {
            case "secretsProvider":
                document.SecretsProvider = Scalar(value) ?? EnvironmentDocument.PassphraseProvider;
                break;
            case "encryptionSalt":
                document.EncryptionSalt = Scalar(value);
                break;
            case "encryption" when value is YamlMappingNode encryption:
                ReadEncryption(document, encryption);
                break;
            case "config" when value is YamlMappingNode config:
                ReadConfig(document, config);
                break;
        }
    }

    static void ReadEncryption(EnvironmentDocument document, YamlMappingNode encryption)
    {
        string? algorithm = null;
        var iterations = 0;
        foreach (var (keyNode, valueNode) in encryption.Children)
        {
            switch (Scalar(keyNode))
            {
                case "kdf":
                    algorithm = Scalar(valueNode);
                    break;
                case "iterations":
                    _ = int.TryParse(Scalar(valueNode), System.Globalization.CultureInfo.InvariantCulture, out iterations);
                    break;
                case "check":
                    document.EncryptionCheck = Scalar(valueNode);
                    break;
            }
        }

        if (algorithm is not null)
        {
            document.Kdf = new KdfParameters(algorithm, iterations);
        }
    }

    static void ReadConfig(EnvironmentDocument document, YamlMappingNode config)
    {
        foreach (var (keyNode, valueNode) in config.Children)
        {
            var key = Scalar(keyNode);
            if (key is null)
            {
                continue;
            }

            document.Config[key] = ReadEntry(key, valueNode);
        }
    }

    static ConfigEntry ReadEntry(string key, YamlNode value) => value switch
    {
        YamlMappingNode map when Scalar(map, "secure") is { } secure => new ConfigEntry(ConfigEntryKind.Secure, secure),
        YamlMappingNode map when Scalar(map, "ref") is { } reference => new ConfigEntry(ConfigEntryKind.Reference, reference),
        YamlMappingNode => throw new EnvironmentException($"Config key '{key}' must be a value, 'secure: <base64>' or 'ref: <name>'."),
        _ => ConfigEntry.Plain(Scalar(value) ?? string.Empty)
    };

    static string? Scalar(YamlNode node) => (node as YamlScalarNode)?.Value;

    static string? Scalar(YamlMappingNode map, string key) =>
        map.Children.TryGetValue(new YamlScalarNode(key), out var node) ? Scalar(node) : null;

    static YamlScalarNode Plain(string value)
    {
        var node = new YamlScalarNode(value);
        if (!IsSafePlainValue(value) || _reserved.Contains(value))
        {
            node.Style = ScalarStyle.DoubleQuoted;
        }

        return node;
    }

    static bool IsSafePlainValue(string value) => value.Length > 0 && value.All(IsSafePlain);

    static bool IsSafePlain(char c) => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '/' or ':' or '@' or '+' or '=' or '-';
}
