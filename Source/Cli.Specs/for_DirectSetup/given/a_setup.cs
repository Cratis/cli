// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectSetup.given;

public class a_setup : for_DirectMcpRegistration.given.a_home_and_a_project
{
    private protected DirectSetupSettings _settings;
    private protected DirectSetupResult _result;
    private protected DirectLoginSettings _loginSettings;
    protected bool _valid;
    protected int _checks;
    protected int _logins;
    protected int _loginExit = ExitCodes.Success;

    void Establish() => _settings = new() { Url = Origin, Clients = ["codex"] };

    private protected Task<DirectSetupResult> Run() => DirectSetup.Run(
        _settings,
        Locations,
        null,
        (_, _) =>
        {
            _checks++;
            return Task.FromResult(_valid);
        },
        (settings, _) =>
        {
            _logins++;
            _loginSettings = settings;
            return Task.FromResult(_loginExit);
        },
        CancellationToken.None);
}
