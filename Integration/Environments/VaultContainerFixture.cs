// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Environments.Integration;

/// <summary>
/// Starts a <c language="csharp">hashicorp/vault</c> dev server, which has a KV v2 mount at <c language="csharp">secret</c> and a fixed root token.
/// </summary>
public class VaultContainerFixture : IAsyncLifetime
{
    /// <summary>
    /// The root token of the dev server.
    /// </summary>
    public const string RootToken = "cratis-integration-token";

    IContainer? _container;

    /// <summary>
    /// Gets the <c language="csharp">host:port</c> the dev server listens on.
    /// </summary>
    public string Address => $"{_container!.Hostname}:{_container.GetMappedPublicPort(8200)}";

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        _container = new ContainerBuilder("hashicorp/vault:1.18")
            .WithEnvironment("VAULT_DEV_ROOT_TOKEN_ID", RootToken)
            .WithEnvironment("VAULT_DEV_LISTEN_ADDRESS", "0.0.0.0:8200")
            .WithPortBinding(8200, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPath("/v1/sys/health").ForPort(8200)))
            .Build();
        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
