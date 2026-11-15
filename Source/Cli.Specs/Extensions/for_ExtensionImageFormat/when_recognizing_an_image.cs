// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionImageFormat;

public class when_recognizing_an_image : Specification
{
    [Fact] void should_recognize_a_png() => ExtensionImageFormat.Of([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1]).ShouldEqual("png");
    [Fact] void should_recognize_a_jpeg() => ExtensionImageFormat.Of([0xFF, 0xD8, 0xFF, 0xE0]).ShouldEqual("jpg");
    [Fact] void should_recognize_a_webp() => ExtensionImageFormat.Of(Encoding.ASCII.GetBytes("RIFF0000WEBPVP8 ")).ShouldEqual("webp");
    [Fact] void should_recognize_an_svg() => ExtensionImageFormat.Of(Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><svg/>")).ShouldEqual("svg");
    [Fact] void should_not_recognize_a_gif() => ExtensionImageFormat.Of(Encoding.ASCII.GetBytes("GIF89a....")).ShouldBeNull();
    [Fact] void should_not_recognize_empty_content() => ExtensionImageFormat.Of([]).ShouldBeNull();
    [Fact] void should_read_the_format_a_name_claims() => ExtensionImageFormat.Claimed("assets/Shot.JPEG").ShouldEqual("jpg");
}
