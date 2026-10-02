// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_EmbeddedEventModels;

/// <summary>
/// An assembly built without the embedded Screenplay package - the CLI itself - embeds nothing, which is what
/// sends 'cratis view' to generating the documents from source instead.
/// </summary>
public class when_reading_an_assembly_without_documents : Specification
{
    EmbeddedEventModels _models;

    void Because() => _models = EmbeddedEventModels.From(typeof(ViewCommand).Assembly.Location);

    void Destroy() => _models.Dispose();

    [Fact] void should_find_no_documents() => _models.Any.ShouldBeFalse();
}
