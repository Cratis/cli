// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_canonicalizing_the_destination : given.a_token_cache
{
    bool _sameHost;
    bool _sameEndpoint;
    bool _differentPort;
    bool _differentClient;
    bool _differentContext;
    bool _secretExcluded;
    bool _validationExcluded;
    bool _certificateExcluded;
    bool _contextCasePreserved;

    void Because()
    {
        var original = CachePath(Connection());
        _sameHost = original == CachePath(Connection("LOCALHOST:35000"));
        _sameEndpoint = original == CachePath(Connection("localhost"));
        _differentPort = original != CachePath(Connection("localhost:35001"));
        _differentClient = original != CachePath(Connection(client: "Client"));
        _differentContext = original != CachePath(Connection(), "other");
        _contextCasePreserved = original != CachePath(Connection(), "Production");
        _secretExcluded = original == CachePath(new ChronicleConnectionString("chronicle://client:different-fixture@localhost:35000"));
        _validationExcluded = original == CachePath(new ChronicleConnectionString("chronicle://client:fixture-secret@localhost:35000/?skipTlsValidation=false"));
        _certificateExcluded = original == CachePath(new ChronicleConnectionString("chronicle://client:fixture-secret@localhost:35000/?certificatePath=other.crt&certificatePassword=fixture"));
    }

    [Fact] void should_fold_host_case() => _sameHost.ShouldBeTrue();
    [Fact] void should_resolve_the_default_port() => _sameEndpoint.ShouldBeTrue();
    [Fact] void should_preserve_explicit_ports() => _differentPort.ShouldBeTrue();
    [Fact] void should_preserve_client_case() => _differentClient.ShouldBeTrue();
    [Fact] void should_partition_contexts() => _differentContext.ShouldBeTrue();
    [Fact] void should_preserve_context_case() => _contextCasePreserved.ShouldBeTrue();
    [Fact] void should_exclude_client_secrets() => _secretExcluded.ShouldBeTrue();
    [Fact] void should_exclude_validation_settings() => _validationExcluded.ShouldBeTrue();
    [Fact] void should_exclude_certificate_secrets() => _certificateExcluded.ShouldBeTrue();
}
