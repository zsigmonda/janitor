using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.Text;

namespace Janitor.ProofOfConcept
{
  public class TryBlockCollector : CSharpSyntaxWalker
  {
    public readonly List<TextSpan> TrySpans = new List<TextSpan>();

    public SemanticModel Model { get; private set; }
    public System.Threading.CancellationToken CancellationToken { get; private set; }

    public TryBlockCollector(SemanticModel model, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
      this.Model = model;
      this.CancellationToken = cancellationToken;
    }

    public override void VisitTryStatement(TryStatementSyntax node)
    {
      TrySpans.Add(node.Block.FullSpan);
          


      base.VisitTryStatement(node);
    }
  }
}
