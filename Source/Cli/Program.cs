// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli;
using Spectre.Console;

if (args.Length == 0 && !Console.IsOutputRedirected)
{
    Banner.Render();
}

return await CliApp.Create().RunAsync(args);
