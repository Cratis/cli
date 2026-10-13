// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Cli.Commands.Chronicle;

internal static class TokenServer
{
    internal static string Normalize(ChronicleServerAddress address)
    {
        var host = address.Host.Trim('[', ']').ToLowerInvariant();
        if (IPAddress.TryParse(host, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            host = $"[{ip}]";
        }

        return $"{host}:{address.Port}";
    }
}
