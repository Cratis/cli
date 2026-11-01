// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prologue.Contracts;

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_a_captured_service : given.an_interpret_command
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
        _settings.Service = "billing";
    }

    Task Because() => Interpret();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_interpret_only_that_service() => Services().ShouldContainOnly(["billing"]);
    [Fact] void should_model_the_service_as_a_module() => JsonDocument.Parse(_output).RootElement.GetProperty("modules").GetInt32().ShouldEqual(1);

    IEnumerable<string> Services() => JsonDocument.Parse(_output).RootElement.GetProperty("services").EnumerateArray().Select(service => service.GetString()!);
}
