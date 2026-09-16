// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_ValueFormRegistry.given;

public abstract class a_registry : Specification
{
    protected static ValueFormRegistry Registry(params (string Name, string Identifier, string? Pattern, string? Replacement, string[]? Steps)[] userForms)
    {
        var forms = userForms.ToDictionary(
            form => form.Name,
            form => new Configuration.ValueFormConfig
            {
                Identifier = form.Identifier,
                Pattern = form.Pattern,
                Replacement = form.Replacement,
                Steps = form.Steps ?? []
            });
        return new ValueFormRegistry(forms);
    }

    protected static ValueFormRegistry BuiltIns { get; } = ValueFormRegistry.Empty;
}
