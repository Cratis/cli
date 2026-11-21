// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.Commands.Chronicle.Observers;

namespace Cratis.Cli.for_ReplayObserverCommand.given;

public class ReplayObserverCommandForSpecs : ReplayObserverCommand
{
    public Task<int> ExecuteConnected(IServices services, ObserverCommandSettings settings) => ExecuteCommandAsync(services, settings, OutputFormats.JsonCompact);

    public Task<int> ExecuteFromCommandLine(ObserverCommandSettings settings) => ExecuteAsync(null!, settings, CancellationToken.None);

    public string? Prompt(ObserverCommandSettings settings) => GetConfirmationPrompt(settings);
}
