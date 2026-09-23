// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Specs.for_TemplateConfigParser.given;

namespace Cratis.Templating.Specs.for_TemplateConfigParser;

public class when_parsing_a_full_manifest : a_parser
{
    const string Manifest = "        {\n        \"author\": \"Cratis\",\n        \"name\": \"Full Template\",\n        \"shortName\": \"full\",\n        \"identity\": \"Cratis.Templates.Full\",\n        \"description\": \"A full manifest\",\n        \"classifications\": [\"Web\"],\n        \"sourceName\": \"TemplateApp\",\n        \"defaultName\": \"MyApp\",\n        \"preferNameDirectory\": true,\n        \"preferDefaultName\": false,\n        \"placeholderFilename\": \"_\",\n        \"precedence\": 3,\n        \"groupIdentity\": \"Cratis.Group\",\n        \"guids\": [\"8e23b1e0-d6b3-4f1b-9db2-c55d49a2b311\"],\n        \"tags\": { \"language\": \"C#\", \"type\": \"project\" },\n        \"symbols\": {\n            \"Framework\": {\n                \"type\": \"parameter\",\n                \"datatype\": \"choice\",\n                \"choices\": [ { \"choice\": \"net10.0\", \"description\": \".NET 10\" } ],\n                \"defaultValue\": \"net10.0\",\n                \"replaces\": \"TARGET_FRAMEWORK\",\n                \"prompt\": \"Select framework\"\n            },\n            \"Guid\": { \"type\": \"generated\", \"generator\": \"guid\", \"replaces\": \"PROJECT_GUID\" },\n            \"Computed\": { \"type\": \"computed\", \"value\": \"Framework == \\\"net10.0\\\"\", \"evaluator\": \"C++2\" },\n            \"Derived\": { \"type\": \"derived\", \"valueSource\": \"Framework\", \"valueTransform\": \"upperCaseInvariant\" },\n            \"Bound\": { \"type\": \"bind\", \"binding\": \"name\" }\n        },\n        \"sources\": [\n            {\n                \"source\": \"./\",\n                \"exclude\": [\"**/bin/**\"],\n                \"copyOnly\": [\"Assets/logo.png\"],\n                \"rename\": [ { \"pattern\": \"Source\", \"replacement\": \"Target\" } ],\n                \"modifiers\": [ { \"condition\": \"Framework == \\\"net10.0\\\"\", \"exclude\": [\"legacy/**\"] } ]\n            }\n        ],\n        \"primaryOutputs\": [ { \"path\": \"TemplateApp.csproj\", \"condition\": \"Framework == \\\"net10.0\\\"\" } ],\n        \"postActions\": [ { \"actionId\": \"B17581D1-C5C9-4489-8F0A-004BE667B814\", \"continueOnError\": true } ],\n        \"baselines\": { \"legacy\": { \"symbols\": { \"Framework\": \"net8.0\" } } },\n        \"constraints\": { \"linux-only\": { \"type\": \"os\", \"args\": \"Linux\" } },\n        \"forms\": { \"upper\": { \"identifier\": \"upperCaseInvariant\" } },\n        \"globalCustomOperations\": [ { \"type\": \"replacement\", \"configuration\": { \"token\": \"COPY_YEAR\", \"variable\": \"year\" } } ],\n        \"specialCustomOperations\": { \"**.custom\": { \"operations\": [ { \"type\": \"conditional\", \"configuration\": { \"if\": [\"--#if\"], \"endif\": [\"--#endif\"], \"trim\": true } } ] } }\n    }";

    TemplateConfig? _config;

    void Because() => _config = Parse(Manifest);

    [Fact] void should_read_the_identity_properties() => _config!.Identity.ShouldEqual("Cratis.Templates.Full");
    [Fact] void should_read_all_five_symbol_types() => _config!.Symbols.Count.ShouldEqual(5);
    [Fact] void should_read_the_framework_choices() => _config!.Symbols["Framework"].Choices[0].Choice.ShouldEqual("net10.0");
    [Fact] void should_read_the_guid_entries() => _config!.Guids.Count.ShouldEqual(1);
    [Fact] void should_read_the_source_rules() => _config!.Sources[0].Exclude.Count.ShouldEqual(1);
    [Fact] void should_read_the_source_modifiers() => _config!.Sources[0].Modifiers[0].Exclude[0].ShouldEqual("legacy/**");
    [Fact] void should_read_the_baselines() => _config!.Baselines["legacy"].Symbols["Framework"].ShouldEqual("net8.0");
    [Fact] void should_read_the_constraints() => _config!.Constraints["linux-only"].Type.ShouldEqual("os");
    [Fact] void should_read_the_custom_operations() => _config!.GlobalCustomOperations[0].Token.ShouldEqual("COPY_YEAR");
    [Fact] void should_read_the_special_custom_operations() => (_config!.SpecialCustomOperations["**.custom"][0].Trim ?? false).ShouldBeTrue();
}
