// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

#pragma warning disable MA0136 // Screenplay fixtures intentionally end with a newline.

public class when_rendering_implementation_attachments
{
    const string FileSource = """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Decimal
                validate
                  amount rule Positive message "Positive amount required"
                    file Rules/Positive.cs
                produces OrderPlaced
                  orderId = orderId
                  amount = amount
              event OrderPlaced
                orderId Uuid
                amount Decimal
        """;

    const string InlineSource = """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Decimal
                validate csharp
                  ```csharp
                  if (context.Artifact.amount <= 0) yield return "Nothing to order";
                  ```
                produces OrderPlaced
                  orderId = orderId
                  amount = amount
              event OrderPlaced
                orderId Uuid
                amount Decimal
        """;

    const string ReducerSource = """
        module Billing
          feature Accounts
            slice StateView Balance
              event AmountDeposited
                amount Decimal
              readmodel AccountBalance
                balance Decimal
                id Uuid
              query BalanceById => AccountBalance?
                by id Uuid
              reducer Balance => AccountBalance
                on AmountDeposited
                  csharp
                    ```
                    return new(context.Event.amount);
                    ```
        """;

    const string PolicySource = """
        policy CustomAccess
          ```csharp
          return context.Identity.IsAuthenticated;
          ```
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                authorize CustomAccess
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
        """;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task should_load_file_contents_from_the_folder_or_single_file_parent(int inputForm)
    {
        var root = NewRoot();
        try
        {
            var path = Path.Combine(root, "Orders.play");
            await File.WriteAllTextAsync(path, FileSource);
            Directory.CreateDirectory(Path.Combine(root, "Rules"));
            const string body = "return context.Value > 0;\r\n";
            await File.WriteAllTextAsync(Path.Combine(root, "Rules", "Positive.cs"), body);
            SemanticDocumentSet? supplied = null;
            CompilationResult<SemanticCompilation>? compiled = null;
            var compiler = Substitute.For<ISemanticModelCompiler>();
            compiler.Compile(Arg.Any<string>(), Arg.Any<SemanticDocumentSet>()).Returns(call =>
            {
                supplied = call.ArgAt<SemanticDocumentSet>(1);
                compiled = new SemanticModelCompiler().Compile(call.ArgAt<string>(0), supplied);
                return compiled;
            });
            var input = inputForm switch
            {
                0 => root,
                1 => path,
                _ => Path.GetRelativePath(Directory.GetCurrentDirectory(), path)
            };
            var result = await new ScreenplayPlanning(compiler, new RenderTargetRoster())
                .Plan(new(input, "Orders", "cratis"), CancellationToken.None);
            var requirement = Assert.Single(compiled!.ImplementationRequirements);
            Assert.Equal(body, supplied!.AttachmentContents["Rules/Positive.cs"]);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body))), requirement.ContentHash);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-005");
            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020");
            Assert.False(result.Success);
            Assert.Empty(result.Artifacts!.Artifacts);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData("../outside.cs", "PLAY0430")]
    [InlineData("Rules/Positive.cs", "PLAY0432")]
    public async Task should_report_refused_and_missing_files_with_stage_requirement_failure(string reference, string code)
    {
        var root = NewRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "Orders.play"), FileSource.Replace("Rules/Positive.cs", reference, StringComparison.Ordinal));
            var result = await new ScreenplayPlanning().Plan(new(root, "Orders", "cratis"), CancellationToken.None);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains(code, StringComparison.Ordinal));
            Assert.False(result.Success);
            Assert.Empty(result.Artifacts!.Artifacts);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task should_refuse_a_linked_attachment()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var root = NewRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "Orders.play"), FileSource);
            Directory.CreateDirectory(Path.Combine(root, "Rules"));
            await File.WriteAllTextAsync(Path.Combine(root, "real.cs"), "return true;");
            File.CreateSymbolicLink(Path.Combine(root, "Rules", "Positive.cs"), Path.Combine(root, "real.cs"));
            var result = await new ScreenplayPlanning().Plan(new(root, "Orders", "cratis"), CancellationToken.None);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "PLAY0431");
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains("PLAY0431", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(false, "PLAY0434")]
    [InlineData(true, "PLAY0433")]
    public async Task should_refuse_non_text_and_oversized_attachments(bool oversized, string code)
    {
        var root = NewRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "Orders.play"), FileSource);
            Directory.CreateDirectory(Path.Combine(root, "Rules"));
            await File.WriteAllBytesAsync(Path.Combine(root, "Rules", "Positive.cs"), oversized ? new byte[(2 * 1024 * 1024) + 1] : [0, 255, 0]);
            var result = await new ScreenplayPlanning().Plan(new(root, "Orders", "cratis"), CancellationToken.None);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains(code, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData(InlineSource, "STAGE-ESM-005")]
    [InlineData(ReducerSource, "STAGE-ESM-019")]
    [InlineData(PolicySource, "STAGE-ESM-015")]
    public async Task should_report_precise_v3_target_rejections_for_verified_inline_bodies(string source, string code)
    {
        var root = NewRoot();
        try
        {
            var path = Path.Combine(root, "Orders.play");
            await File.WriteAllTextAsync(path, source);
            var result = await new ScreenplayPlanning().Plan(new(path, "Orders", "cratis"), CancellationToken.None);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020");
            Assert.False(result.Success);
            Assert.Empty(result.Artifacts!.Artifacts);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void should_not_read_files_from_a_workspace_document_set()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("orders"), "orders", "Orders.play", FileSource);
        var result = new ScreenplayPlanning().PlanDocuments(
            new(SemanticDocumentSet.Create([document], catalog), "Orders", "cratis"), CancellationToken.None);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020");
        Assert.False(result.Success);
    }

    static string NewRoot() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-attachments-{Guid.NewGuid():N}")).FullName;
}
