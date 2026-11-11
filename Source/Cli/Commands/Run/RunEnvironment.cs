// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;
using Cratis.Environments.Secrets;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// The values of the environment a run uses, or the keys it lacks.
/// </summary>
/// <param name="Name">The environment name.</param>
/// <param name="Exists">Whether the environment has a file.</param>
/// <param name="Values">The resolved values by key, secrets included. Never printed.</param>
/// <param name="Missing">The keys the model needs that the environment lacks.</param>
/// <param name="ProjectRoot">The folder that holds <c language="shell">.cratis</c>.</param>
public sealed record RunEnvironment(string Name, bool Exists, IReadOnlyDictionary<string, string> Values, IReadOnlyList<string> Missing, string ProjectRoot)
{
    /// <summary>
    /// The environment keys that configure the mail server onboarding sends through.
    /// </summary>
    public const string MailPrefix = EnvironmentRequirements.OnboardingPrefix + "email.";

    /// <summary>
    /// The key whose presence says the environment names a real mail server.
    /// </summary>
    public const string MailHost = MailPrefix + "server.host";

    /// <summary>
    /// Gets a value indicating whether the environment names a real mail server. Otherwise onboarding emails are written to a folder and never sent.
    /// </summary>
    public bool HasMailServer => Values.ContainsKey(MailHost);

    /// <summary>
    /// Resolves the environment a run uses for a model.
    /// </summary>
    /// <param name="application">The compiled model.</param>
    /// <param name="modelFolder">The folder the model is read from, where the project is found from.</param>
    /// <param name="name">The environment named with <c language="shell">--environment</c>, or null.</param>
    /// <param name="passphrase">The passphrase source, asked only when a secret needs it.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The environment, with the keys it lacks.</returns>
    public static async Task<RunEnvironment> Resolve(ApplicationSyntax application, string modelFolder, string? name, IPassphraseSource passphrase, CancellationToken cancellationToken)
    {
        var project = ProjectEnvironments.Discover(modelFolder);
        var selected = project.Resolve(name).Name;
        var required = EnvironmentRequirements.For(application).Keys.Select(key => key.Key).ToArray();
        if (!project.Exists(selected))
        {
            return new(selected, false, new Dictionary<string, string>(), [.. WithoutMail(required, mailServer: false)], project.ProjectRoot);
        }

        var environment = ProjectEnvironment.Open(project, selected, passphrase);
        var mailServer = environment.GetEntry(MailHost) is not null;
        var missing = WithoutMail(required, mailServer).Where(key => environment.GetEntry(key) is null).ToArray();
        if (missing.Length > 0)
        {
            return new(selected, true, new Dictionary<string, string>(), missing, project.ProjectRoot);
        }

        Dictionary<string, string> values = [with(StringComparer.Ordinal)];
        var identities = application.Authentication?.Providers.Select(provider => provider.Identity).ToHashSet(StringComparer.Ordinal) ?? [];
        foreach (var key in environment.Keys.Where(key => IsNeeded(key, required, mailServer, identities)))
        {
            values[key] = (await environment.GetValueAsync(key, cancellationToken))!;
        }

        return new(selected, true, values, [], project.ProjectRoot);
    }

    /// <summary>
    /// Keeps the keys a run asks the environment for: without a mail server, the mail keys are not needed because the emails are written to a folder.
    /// </summary>
    /// <param name="keys">The keys the model needs.</param>
    /// <param name="mailServer">Whether the environment names a mail server.</param>
    /// <returns>The keys the environment has to supply.</returns>
    public static IEnumerable<string> WithoutMail(IEnumerable<string> keys, bool mailServer) =>
        mailServer ? keys : keys.Where(key => !key.StartsWith(MailPrefix, StringComparison.Ordinal));

    static bool IsNeeded(string key, string[] required, bool mailServer, HashSet<string> identities) =>
        key.StartsWith(MailPrefix, StringComparison.Ordinal)
            ? mailServer || key == MailPrefix + "sender"
            : required.Contains(key, StringComparer.Ordinal) || IsOfProvider(key, identities);

    /// <summary>
    /// Any other setting the environment holds for a provider the model declares, such as its scopes, is handed on too; a provider the model does not declare is not.
    /// </summary>
    /// <param name="key">The environment key.</param>
    /// <param name="identities">The identities of the providers the model declares.</param>
    /// <returns>True when the key is a setting of a provider the model declares.</returns>
    static bool IsOfProvider(string key, HashSet<string> identities)
    {
        if (!key.StartsWith(EnvironmentRequirements.ProviderPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = key[EnvironmentRequirements.ProviderPrefix.Length..];
        var dot = rest.IndexOf('.', StringComparison.Ordinal);
        return dot > 0 && identities.Contains(rest[..dot]);
    }
}
