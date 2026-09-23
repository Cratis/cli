// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_ConditionalProcessor.given;

public abstract class a_processor : Specification
{
    protected static readonly FileFamilyConfig LanguageConfig = FileFamilies.ConfigFor(FileFamily.Language);
    protected static readonly FileFamilyConfig JsonConfig = FileFamilies.ConfigFor(FileFamily.Json);
    protected static readonly FileFamilyConfig XmlConfig = FileFamilies.ConfigFor(FileFamily.Xml);

    protected static string Process(
        string content,
        FileFamilyConfig? configuration = null,
        IReadOnlyDictionary<string, string>? scope = null,
        ExpressionDialect dialect = ExpressionDialect.Cpp2) =>
        ConditionalProcessor.Process(
            content,
            configuration ?? LanguageConfig,
            dialect,
            scope ?? new Dictionary<string, string>(),
            "test.file");
}
