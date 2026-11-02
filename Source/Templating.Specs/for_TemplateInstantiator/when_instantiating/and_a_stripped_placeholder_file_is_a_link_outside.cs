// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public class and_a_stripped_placeholder_file_is_a_link_outside : given_a_template_directory
{
    string _output = null!;
    string _outsideFile = null!;
    bool _linked;
    Exception? _error;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "_.gitignore"), "ignored\n");
        var parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-instantiate", Guid.NewGuid().ToString("N"))).FullName;
        _output = Directory.CreateDirectory(Path.Combine(parent, "app")).FullName;
        _outsideFile = Path.Combine(parent, "victim.txt");
        File.WriteAllText(_outsideFile, "original");
        try
        {
            File.CreateSymbolicLink(Path.Combine(_output, ".gitignore"), _outsideFile);
            _linked = true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _linked = false;
        }
    }

    void Because()
    {
        if (!_linked) return;
        _error = Catch.Exception(() => Instantiate(new InstantiationInputs("App", _output, new Dictionary<string, string>(), Force: true)));
    }

    [Fact]
    void should_refuse_the_manifest()
    {
        if (!_linked) return;
        _error.ShouldBeOfExactType<InvalidTemplateManifest>();
    }

    [Fact]
    void should_not_write_through_the_link()
    {
        if (!_linked) return;
        File.ReadAllText(_outsideFile).ShouldEqual("original");
    }
}
