// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions;

/// <summary>
/// Represents the credential of a private registry. It is read from <c>cratis config</c>, never from the model or the lock, and never printed.
/// </summary>
/// <param name="Username">The user name, when the registry uses basic authentication.</param>
/// <param name="Password">The password.</param>
/// <param name="Token">An access token, used as the password when no password is set and as the bearer token for npm.</param>
public record RegistryCredential(string? Username, string? Password, string? Token)
{
    /// <summary>
    /// Gets the secret values that must never appear in output.
    /// </summary>
    public IEnumerable<string> Secrets => new[] { Password, Token }.OfType<string>().Where(value => value.Length > 0);

    /// <summary>
    /// Removes every secret value from a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The message without secrets.</returns>
    public string Redact(string message) => Secrets.Aggregate(message, (current, secret) => current.Replace(secret, "***", StringComparison.Ordinal));
}

/// <summary>
/// Defines where registry credentials come from.
/// </summary>
public interface IRegistryCredentials
{
    /// <summary>
    /// Gets the credential for a registry host.
    /// </summary>
    /// <param name="host">The host, with its port when it is not the default.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The credential, or <see langword="null"/> to connect anonymously.</returns>
    Task<RegistryCredential?> For(string host, CancellationToken cancellationToken);
}

/// <summary>
/// Defines no credentials: every registry is used anonymously.
/// </summary>
public class AnonymousRegistryCredentials : IRegistryCredentials
{
    /// <inheritdoc/>
    public Task<RegistryCredential?> For(string host, CancellationToken cancellationToken) => Task.FromResult<RegistryCredential?>(null);
}

/// <summary>
/// Reads registry credentials from the selected <c>cratis config</c> environment, under
/// <c>extensions.registry.&lt;host&gt;.username</c>, <c>.password</c> and <c>.token</c>.
/// </summary>
/// <param name="lookup">Reads one configuration value, or <see langword="null"/> when it is not set.</param>
public class ConfiguredRegistryCredentials(Func<string, CancellationToken, Task<string?>> lookup) : IRegistryCredentials
{
    /// <summary>
    /// Gets the configuration key of one credential field.
    /// </summary>
    /// <param name="host">The registry host.</param>
    /// <param name="field">One of <c>username</c>, <c>password</c> or <c>token</c>.</param>
    /// <returns>The key.</returns>
    public static string KeyOf(string host, string field) => $"extensions.registry.{host}.{field}";

    /// <inheritdoc/>
    public async Task<RegistryCredential?> For(string host, CancellationToken cancellationToken)
    {
        var username = await lookup(KeyOf(host, "username"), cancellationToken);
        var password = await lookup(KeyOf(host, "password"), cancellationToken);
        var token = await lookup(KeyOf(host, "token"), cancellationToken);
        return username is null && password is null && token is null ? null : new(username, password, token);
    }
}
