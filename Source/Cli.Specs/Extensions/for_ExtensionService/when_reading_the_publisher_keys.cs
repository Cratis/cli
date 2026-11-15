// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_reading_the_publisher_keys : Specification
{
    const string Publisher = "https://github.com/cratis";

    ExtensionProject _project;
    RegistryPackage _pinned;
    IReadOnlyDictionary<string, IReadOnlyList<string>> _keys;
    string _keyFile;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _pinned = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _project.Registry.With(_pinned);
        _project.Lock("2", _pinned);
        _keyFile = Path.Combine(Path.GetDirectoryName(_project.Cache.PathOf(_pinned.Digest))!, ExtensionService.KeyFileName);
    }

    async Task Because() => _keys = await _project.Service().PublisherKeys(_project.Workspace(), CancellationToken.None);

    void Destroy() => _project.Dispose();

    [Fact] void should_hand_over_the_key_the_package_is_signed_with() => _keys[Publisher].Single().ShouldEqual(Convert.ToBase64String(ExtensionPackageFactory.PublicKeyOf(Publisher)));
    [Fact] void should_keep_the_key_beside_the_cached_contract_so_the_package_is_fetched_once() => File.ReadAllText(_keyFile).ShouldEqual(Convert.ToBase64String(ExtensionPackageFactory.PublicKeyOf(Publisher)));
}
