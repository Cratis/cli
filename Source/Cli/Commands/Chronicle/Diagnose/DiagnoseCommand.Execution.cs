// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

public partial class DiagnoseCommand
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, DiagnoseSettings settings, string format)
    {
        if (settings.Watch)
        {
            if (format is not OutputFormats.Table)
            {
                OutputFormatter.WriteError(format, "--watch requires text output format", "Remove -o/--output or use --output table", ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            if (settings.Interval < 1)
            {
                OutputFormatter.WriteError(format, "--interval must be at least 1 second", errorCode: ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
            }

            return await RunWatch(services, settings);
        }

        var data = await Gather(services, settings);
        Render(format, data);
        return data.ExitCode;
    }
}
