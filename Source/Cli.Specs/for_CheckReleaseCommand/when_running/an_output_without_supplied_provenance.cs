// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_running;

public class an_output_without_supplied_provenance : given.a_release_target
{
    static readonly JsonSerializerOptions _options = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
    JsonElement _subject;
    JsonElement _production;

    async Task Because()
    {
        _release = _release with { Repository = null, Component = null, Environment = null };
        await Check();
        var document = JsonSerializer.SerializeToElement(ReleaseOutput.ToDocument(_result), _options);
        _subject = document.GetProperty("subject");
        _production = document.GetProperty("production");
    }

    [Fact] void should_not_invent_the_repository() => _subject.TryGetProperty("repo", out _).ShouldBeFalse();
    [Fact] void should_not_invent_the_component() => _subject.TryGetProperty("component", out _).ShouldBeFalse();
    [Fact] void should_not_invent_the_environment() => _production.TryGetProperty("environment", out _).ShouldBeFalse();
}
