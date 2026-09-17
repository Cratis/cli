// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Specs.for_TemplateConfigParser.given;

public abstract class a_parser : Specification
{
    protected static TemplateConfig Parse(string json) => TemplateConfigParser.ParseDocument(json);
}
