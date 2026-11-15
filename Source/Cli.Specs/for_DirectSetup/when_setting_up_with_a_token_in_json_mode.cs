// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectSetup;

public class when_setting_up_with_a_token_in_json_mode : for_DirectMcpRegistration.given.a_home_and_a_project
{
    const string Token = "dpat_example-for-process-specifications";
    string _setup;
    string _status;
    string _logout;
    string _index;
    int _environmentLogin;
    int _invalid;
    int _tenant;
    bool _removed;

    async Task Because()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The explicit plaintext store is unavailable on Windows.");
        var setup = await Run(["setup", "--token=-", "--client", "claude", "--url", Origin, "--insecure-file-store", "-o", "json"], Token);
        Assert.True(setup.Code == ExitCodes.Success, setup.Output);
        _setup = setup.Output;
        using var document = JsonDocument.Parse(_setup);
        document.RootElement.ValueKind.ShouldEqual(JsonValueKind.Object);
        _index = await File.ReadAllTextAsync(HomeFile(".cratis/config.json"));
        _status = (await Run(["status", "-o", "json"])).Output;
        _logout = (await Run(["logout", "-o", "json"])).Output;
        _removed = Directory.GetFiles(HomeFile(".cratis/direct-secrets")).Length == 0;
        _environmentLogin = (await Run(["login", "--token", "--url", Origin, "--insecure-file-store", "-o", "json"], environment: Token)).Code;
        _invalid = (await Run(["login", "--token=-", "-o", "json"], "not-a-personal-access-token")).Code;
        _tenant = (await Run(["login", "--token=-", "--tenant", "team", "-o", "json"], Token)).Code;
        await Run(["logout", "-o", "json"]);
    }

    [Fact] void should_emit_one_setup_document() => JsonDocument.Parse(_setup).Dispose();
    [Fact] void should_register_the_client() => IsRegistered(HomeFile(".claude.json"), "mcpServers").ShouldBeTrue();
    [Fact] void should_hide_the_token_from_the_index() => _index.Contains(Token).ShouldBeFalse();
    [Fact] void should_hide_the_token_from_stdout() => (_setup + _status + _logout).Contains(Token).ShouldBeFalse();
    [Fact] void should_show_the_credential_kind() => _status.ShouldContain("personal-access-token");
    [Fact] void should_remove_the_secret_on_logout() => _removed.ShouldBeTrue();
    [Fact] void should_not_claim_server_side_revocation() => _logout.ShouldContain("not revoked server-side");
    [Fact] void should_log_in_using_the_environment() => _environmentLogin.ShouldEqual(ExitCodes.Success);
    [Fact] void should_reject_bad_format() => _invalid.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_reject_a_tenant_option() => _tenant.ShouldEqual(ExitCodes.AuthenticationError);

    async Task<(int Code, string Output)> Run(string[] arguments, string? input = null, string? environment = null)
    {
        var specs = GetType().Assembly.Location;
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            WorkingDirectory = _project,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        string[] launch = ["exec", "--runtimeconfig", Path.ChangeExtension(specs, ".runtimeconfig.json"), "--depsfile", Path.ChangeExtension(specs, ".deps.json"), typeof(DirectSetupCommand).Assembly.Location, "direct"];
        foreach (var argument in launch.Concat(arguments)) start.ArgumentList.Add(argument);
        start.Environment["HOME"] = _home;
        start.Environment["USERPROFILE"] = _home;
        start.Environment.Remove("DIRECT_TOKEN");
        if (environment is not null) start.Environment["DIRECT_TOKEN"] = environment;
        using var child = new Process { StartInfo = start };
        child.Start().ShouldBeTrue();
        var output = child.StandardOutput.ReadToEndAsync();
        var errors = child.StandardError.ReadToEndAsync();
        try
        {
            if (input is not null) await child.StandardInput.WriteLineAsync(input);
            child.StandardInput.Close();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await child.WaitForExitAsync(deadline.Token);
            return (child.ExitCode, await output + (child.ExitCode == 0 ? string.Empty : await errors));
        }
        finally
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync();
            await Task.WhenAll(output, errors);
        }
    }
}
