// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleSettings;

public class when_normalizing_token_servers : Specification
{
    string _ipv6 = null!;
    string _expanded = null!;
    string _differentHost = null!;
    string _differentPort = null!;
    string _dns = null!;
    string _ipv4 = null!;
    string _parsedHost = null!;

    void Because()
    {
        var connection = new ChronicleConnectionString("chronicle://[2001:DB8::1]:35000");
        _parsedHost = connection.ServerAddress.Host;
        _ipv6 = ChronicleSettings.GetTokenServer(connection);
        _expanded = ChronicleSettings.GetTokenServer(new ChronicleConnectionString("chronicle://[2001:db8:0:0:0:0:0:1]:35000"));
        _differentHost = ChronicleSettings.GetTokenServer(new ChronicleConnectionString("chronicle://[2001:db8::2]:35000"));
        _differentPort = ChronicleSettings.GetTokenServer(new ChronicleConnectionString("chronicle://[2001:db8::1]:35001"));
        _dns = ChronicleSettings.GetTokenServer(new ChronicleConnectionString("chronicle://EXAMPLE.com:35000"));
        _ipv4 = ChronicleSettings.GetTokenServer(new ChronicleConnectionString("chronicle://127.0.0.1:35000"));
    }

    [Fact] void should_parse_ipv6_without_brackets_and_preserve_case() => _parsedHost.ShouldEqual("2001:DB8::1");
    [Fact] void should_bracket_and_canonicalize_ipv6() => _ipv6.ShouldEqual("[2001:db8::1]:35000");
    [Fact] void should_match_equivalent_ipv6_addresses() => _expanded.ShouldEqual(_ipv6);
    [Fact] void should_distinguish_ipv6_hosts() => _differentHost.ShouldNotEqual(_ipv6);
    [Fact] void should_distinguish_ports() => _differentPort.ShouldNotEqual(_ipv6);
    [Fact] void should_preserve_older_dns_keys() => _dns.ShouldEqual("example.com:35000");
    [Fact] void should_preserve_older_ipv4_keys() => _ipv4.ShouldEqual("127.0.0.1:35000");
}
