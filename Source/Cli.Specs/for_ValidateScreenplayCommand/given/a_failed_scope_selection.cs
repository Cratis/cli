// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_failed_scope_selection : a_validate_screenplay_command
{
    protected ScopeSelectionErrorKind _kind;
    protected int _exitCode;
    protected JsonElement _error;

    protected async Task RunFailedSelection()
    {
        _settings.Path = _document;
        _settings.Scope = "M";
        _validation.TryValidateScoped(_document, "M", Arg.Any<CompletenessChecks>(), out Arg.Any<ValidatedScreenplay?>(), out Arg.Any<ScopeSelectionError?>())
            .Returns(call =>
            {
                call[3] = null;
                call[4] = new ScopeSelectionError(_kind, "Cannot read the application");
                return false;
            });
        var previous = Console.Error;
        await using var writer = new StringWriter();
        try
        {
            Console.SetError(writer);
            _exitCode = await Execute();
            _error = JsonSerializer.Deserialize<JsonElement>(writer.ToString());
        }
        finally
        {
            Console.SetError(previous);
        }
    }
}
