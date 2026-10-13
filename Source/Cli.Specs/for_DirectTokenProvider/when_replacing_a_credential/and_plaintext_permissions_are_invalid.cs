// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_plaintext_permissions_are_invalid : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    readonly string _home = Path.Combine(Path.GetTempPath(), "direct-permissions-spec-" + Guid.NewGuid().ToString("N"));
    string? _warning;
    string? _refresh;
    UnixFileMode _permissions;

    async Task Because()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var store = new DirectFileSecrets(_home);
        var provider = _server.Provider(store: store);
        await provider.Save(_target, new DirectTokens("previous-access", "previous-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), CancellationToken.None);
        var path = Path.Combine(_home, ".cratis", "direct-secrets", _target.Key + ".json");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        _warning = await provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None);
        _refresh = JsonSerializer.Deserialize<DirectTokens>((await store.Read(_target.Key, CancellationToken.None))!)!.RefreshToken;
        _permissions = File.GetUnixFileMode(path);
    }

    [for_DirectFileSecrets.given.unix_only.Fact] void should_allow_recovery_by_replacing_the_secret() => _refresh.ShouldEqual("new-refresh");
    [for_DirectFileSecrets.given.unix_only.Fact] void should_restore_owner_only_permissions() => _permissions.ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    [for_DirectFileSecrets.given.unix_only.Fact] void should_warn_that_the_old_credential_was_not_revoked() => _warning!.ShouldContain("unreadable and could not be revoked");
    [for_DirectFileSecrets.given.unix_only.Fact] void should_not_send_the_untrusted_secret() => _server.Revoked.ShouldBeEmpty();

    void Destroy()
    {
        _server.Dispose();
        if (Directory.Exists(_home))
        {
            Directory.Delete(_home, recursive: true);
        }
    }
}
