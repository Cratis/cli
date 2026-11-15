// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The values a model's extension systems take from an environment, split by whether they may be shown.
/// </summary>
/// <param name="Settings">Public settings by the Stage configuration name; these may appear on a command line.</param>
/// <param name="Secrets">Secrets by the Stage configuration name; these never leave the process except through a file only the user can read.</param>
/// <param name="Missing">The environment keys that have no value. Names only, never values.</param>
public record RunExtensionValues(IReadOnlyList<KeyValuePair<string, string>> Settings, IReadOnlyList<KeyValuePair<string, string>> Secrets, IReadOnlyList<string> Missing)
{
    /// <summary>
    /// Gets the Stage configuration name of a system's setting, which is the environment variable Stage reads it from.
    /// </summary>
    /// <param name="system">The system.</param>
    /// <param name="setting">The setting.</param>
    /// <returns>The name, <c language="text">systems__&lt;System&gt;__&lt;setting&gt;</c>.</returns>
    public static string StageName(string system, string setting) => $"systems__{system}__{setting}";

    /// <summary>
    /// Resolves what each extension system needs: a mapped setting from its named secret or its literal (which the environment may override), and any other
    /// setting of the extension from <c language="text">systems.&lt;System&gt;.&lt;setting&gt;</c>.
    /// </summary>
    /// <param name="application">The compiled model.</param>
    /// <param name="requirements">What the model needs, which names the implicit settings and whether each is a secret.</param>
    /// <param name="values">The environment's values by key.</param>
    /// <returns>The resolved values and the keys that are missing.</returns>
    public static RunExtensionValues Resolve(ApplicationSyntax application, EnvironmentRequirements requirements, IReadOnlyDictionary<string, string> values)
    {
        List<KeyValuePair<string, string>> settings = [];
        List<KeyValuePair<string, string>> secrets = [];
        HashSet<string> missing = [];

        foreach (var system in application.Systems.Where(system => system.Extension is not null))
        {
            foreach (var setting in system.Settings)
            {
                var name = StageName(system.Name, setting.Name);
                if (setting.Secret is not null)
                {
                    var key = EnvironmentRequirements.SecretKey(setting.Secret);
                    if (values.TryGetValue(key, out var secret)) secrets.Add(new(name, secret));
                    else missing.Add(key);
                }
                else if (values.TryGetValue(EnvironmentRequirements.SystemKey(system.Name, setting.Name), out var overridden))
                {
                    settings.Add(new(name, overridden));
                }
                else if (setting.Value is not null)
                {
                    settings.Add(new(name, setting.Value));
                }
            }
        }

        foreach (var key in requirements.Keys.Where(key => key.Source == EnvironmentKeySource.SystemSetting))
        {
            var name = StageName(key.Owner, key.Key[(EnvironmentRequirements.SystemPrefix.Length + key.Owner.Length + 1)..]);
            if (!values.TryGetValue(key.Key, out var value)) missing.Add(key.Key);
            else (key.IsSecret ? secrets : settings).Add(new(name, value));
        }

        return new(settings, secrets, [.. missing.Order(StringComparer.Ordinal)]);
    }
}
