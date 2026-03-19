// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli;

if (args.Length == 0 && !Console.IsOutputRedirected && !GlobalSettings.IsAiAgentEnvironment())
{
    Banner.Render();
    FirstRunDetector.ShowIfNeeded();
}

return await CliApp.Create().RunAsync(args);
