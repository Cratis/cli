// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments.given;

#pragma warning disable MA0136 // Screenplay fixtures intentionally end with a newline.

public class a_model_root : Specification
{
    protected const string FileSource = """
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

    protected const string InlineSource = """
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

    protected const string ReducerSource = """
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

    protected const string PolicySource = """
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

    protected string _root = null!;
    protected string _path = null!;
    private protected ScreenplayPlanning _planning = null!;
    protected SemanticDocumentSet? _supplied;
    protected CompilationResult<SemanticCompilation>? _compiled;
    protected ArtifactRenderRequest? _renderRequest;

    void Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-attachments-{Guid.NewGuid():N}")).FullName;
        _path = Path.Combine(_root, "Orders.play");
        var compiler = Substitute.For<ISemanticModelCompiler>();
        compiler.Compile(Arg.Any<string>(), Arg.Any<SemanticDocumentSet>()).Returns(call =>
        {
            _supplied = call.ArgAt<SemanticDocumentSet>(1);
            _compiled = new SemanticModelCompiler().Compile(call.ArgAt<string>(0), _supplied);
            return _compiled;
        });
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            _renderRequest = call.Arg<ArtifactRenderRequest>();
            return new CratisArtifactRenderPlanner().Plan(_renderRequest);
        });
        _planning = new ScreenplayPlanning(compiler, new RenderTargetRoster([new CratisRenderTarget(planner)]));
    }

    protected void WriteSource(string source = FileSource) => File.WriteAllText(_path, source);

    protected void WriteAttachment(string body)
    {
        Directory.CreateDirectory(Path.Combine(_root, "Rules"));
        File.WriteAllText(Path.Combine(_root, "Rules", "Positive.cs"), body);
    }

    void Destroy() => Directory.Delete(_root, true);
}
