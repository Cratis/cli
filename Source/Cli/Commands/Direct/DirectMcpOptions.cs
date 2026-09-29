// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>
/// The origin and tenant a Direct MCP bridge is asked to pin. With neither an origin nor a tenant pin the active Direct
/// login is used, tenant included; an origin or a tenant pin given explicitly is never completed from the active login.
/// </summary>
/// <param name="Url">An explicit Direct origin.</param>
/// <param name="Tenant">An explicit tenant.</param>
/// <param name="NoTenant">Whether the bridge is pinned to no tenant, so the active login's tenant is never inherited.</param>
internal sealed record DirectMcpOptions(string? Url, string? Tenant, bool NoTenant = false)
{
    /// <summary>Gets whether the options pin the tenant, to a named tenant or to none.</summary>
    internal bool PinsTenant => Tenant is not null || NoTenant;
}
