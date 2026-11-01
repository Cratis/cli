// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

public class a_scoped_cli_process : a_completeness_cli_process
{
    protected JsonElement Summary => JsonSerializer.Deserialize<JsonElement>(_output);
    protected JsonElement ErrorSummary => JsonSerializer.Deserialize<JsonElement>(_error);

    void Establish() => File.WriteAllText(_document, "module M\n  feature F\n    slice StateView Clean\n      event Changed\n        id Uuid\n    slice StateView Dirty\n      event Broken\n        broken\nmodule Other\n  feature F\n    slice StateView Outside\n      event OtherBroken\n        outsideBroken\n");
}
