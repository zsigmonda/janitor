using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Janitor.BusinessLogic
{
  public class UsingStatementsCollector : CSharpSyntaxWalker
  {
    public readonly List<ISymbol> SymbolsWithUsingPattern = new List<ISymbol>();

    public SemanticModel Model { get; private set; }
    public bool IgnoreUnreachable { get; private set; }
    public System.Threading.CancellationToken CancellationToken { get; private set; }

    public UsingStatementsCollector(SemanticModel model, bool ignoreUnreachable = true, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
      this.Model = model;
      this.IgnoreUnreachable = ignoreUnreachable;
      this.CancellationToken = cancellationToken;
    }

    public override void VisitUsingStatement(UsingStatementSyntax node)
    {
      /*
      Milyen első child node-jai lehetnek egy using statement-nek, amivel meg tudjuk mondani, milyen LocalSymbol-ra vagy FieldSymbol-ra hivatkozunk?
      - VariableDeclaratorSyntax, ebből semanticmodel.getdeclaredsymbol-lal megvan a symbol
      - AssignmentExpressionSyntax, ennek a Left identifier-éből kell elindulni a smybol felé
      - IdentifierNameSyntax, ebből semanticmodel.getsymbolinfo-val megmondjuk a symbolt
      */

      bool process = true;
      if (IgnoreUnreachable)
      {
        ControlFlowAnalysis cfa = Model.AnalyzeControlFlow(node);
        process = (cfa.Succeeded && cfa.StartPointIsReachable);
      }

      if (process)
      {
        SyntaxNode bracketed = node.ChildNodes().FirstOrDefault();
        ISymbol symbol = null;

        if (bracketed != null)
        {
          if (bracketed is VariableDeclarationSyntax)
          {
            VariableDeclaratorSyntax declarator = (bracketed as VariableDeclarationSyntax).DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            symbol = Model.GetDeclaredSymbol(declarator, CancellationToken);
          }
          else
          {
            if (bracketed is AssignmentExpressionSyntax)
            {
              if ((bracketed as AssignmentExpressionSyntax).Left != null)
                symbol = Model.GetSymbolInfo((bracketed as AssignmentExpressionSyntax).Left, CancellationToken).Symbol;
            }
            else
            {
              if (bracketed is IdentifierNameSyntax)
              {
                symbol = Model.GetSymbolInfo(bracketed as IdentifierNameSyntax, CancellationToken).Symbol;
              }
            }
          }
        }

        if (symbol != null)
          SymbolsWithUsingPattern.Add(symbol);
      }

      base.VisitUsingStatement(node);
    }
  }
}
