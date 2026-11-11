// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prologue.Contracts;

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_several_captured_services : given.an_interpret_command
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(_folder, "prologue-billing-http-00001.jsonl"), CaptureFiles.Serialize(new CapturedEntry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            SourceKind.Http,
            new HttpCommandObserved("POST", "/api/invoices", 201, string.Empty))
        { Service = "billing" }));
        _settings.NoLlm = true;
    }

    Task Because() => Interpret();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_interpret_every_service() => JsonElement.Parse(_output).GetProperty("services").EnumerateArray().Select(service => service.GetString()).ShouldContainOnly([string.Empty, "billing"]);
    [Fact] void should_keep_the_services_in_separate_modules() => JsonElement.Parse(_output).GetProperty("modules").GetInt32().ShouldEqual(2);
}
