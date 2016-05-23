using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Janitor.BusinessLogic
{
  /// <summary>
  /// Ez az osztály visszaadja az összes Using statement-et használó szimbólumot, ami a forráskódban előfordul.
  /// </summary>
  public class UsingStatementsCollector : CSharpSyntaxWalker
  {
    /// <summary>
    /// Az összes, a forráskódban megtalálható Using statement-ek használó szimbólum
    /// </summary>
    public readonly List<ISymbol> SymbolsWithUsingPattern = new List<ISymbol>();

    /// <summary>
    /// A szimbólumokat tartalmazó modell, amelyen az elemzést végezzük. Csak konstruktorban adható meg.
    /// </summary>
    public SemanticModel Model { get; private set; }

    /// <summary>
    /// A nem elérhető blokkok figyelmen kívül hagyása. Csak konstruktorban adható meg.
    /// </summary>
    public bool IgnoreUnreachable { get; private set; }

    /// <summary>
    /// A cancellationToken objektum, amellyel az elemzés futása megszakítható. Csak konstruktorban adható meg.
    /// </summary>
    public System.Threading.CancellationToken CancellationToken { get; private set; }

    /// <summary>
    /// Létrehozza az osztály egy példányát, és elvégzi annak inicializálását.
    /// </summary>
    /// <param name="model">A szimbólumokat tartalmazó modell, amelyen az elemzést végezzük.</param>
    /// <param name="ignoreUnreachable">Ez a paraméter megmnodja, hogy figyelmen kívül hagyjuk-e az elérhetetlen blokkokat: true esetén nem adjuk vissza őket, false esetén pedig igen.</param>
    /// <param name="cancellationToken">A cancellationToken objektum, amellyel az elemzés futása megszakítható.</param>
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
