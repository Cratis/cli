// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
namespace Cratis.Cli.Commands.Chronicle.ReadModels.for_ReadModelKeySettings;

public class when_describing_the_read_model_argument : Specification
{
    string _description;

    /// <summary>
    /// The command sends this value as ReadModelIdentifier, so help that calls it a container name
    /// points the user at the wrong column of 'read-models list'.
    /// </summary>
    void Because() => _description = typeof(ReadModelKeySettings)
        .GetProperty(nameof(ReadModelKeySettings.ReadModel))!
        .GetCustomAttribute<DescriptionAttribute>()!
        .Description;

    [Fact] void should_describe_it_as_an_identifier() => _description.ShouldContain("identifier");
    [Fact] void should_not_describe_it_as_a_container_name() => _description.ShouldNotContain("container name");
    [Fact] void should_point_at_the_identifier_column() => _description.ShouldContain("Identifier");
}
