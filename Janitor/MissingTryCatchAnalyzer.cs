using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Janitor.BusinessLogic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Janitor
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MissingTryCatchAnalyzer : DiagnosticAnalyzer
  {
    public const string DiagnosticId = "Janitor";

    //Lokalizált string-ek
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.MissingTryCatchAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.MissingTryCatchAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.MissingTryCatchAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = "Performance";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
    private List<TextSpan> tryBlockSpans = null;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(CollectTryBlocks, SyntaxKind.CompilationUnit);
      context.RegisterSyntaxNodeAction(AnalyzeMissingTryCatch, SyntaxKind.Block);
    }

    private void AnalyzeMissingTryCatch(SyntaxNodeAnalysisContext context)
    {
      if (tryBlockSpans == null)
      {
        tryBlockSpans = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<TryStatementSyntax>().Where(tc => tc.Block != null).Select(tc => tc.Block.FullSpan).ToList();
      }

      if (tryBlockSpans.Count(span => span.Contains(context.Node.Span)) == 0)
      {
        //nincs trycatch körülötte - meg kell nézni a szimbólum összes referenciáját

        ISymbol symbol = context.ContainingSymbol;
        if (symbol != null)
        {
          //a symbol, ami tartalmazza a block-ot - ennek meg tudjuk nézni a referenciáit
          IEnumerable<ReferencedSymbol> refs = SymbolFinder.FindReferencesAsync(symbol, null, context.CancellationToken).Result;

          bool covered = true;
          if (refs != null)
          {
            foreach (ReferencedSymbol rs in refs)
            {
              if (rs.Locations != null)
              {
                foreach (ReferenceLocation loc in rs.Locations)
                {
                  if (tryBlockSpans.Count(span => span.Contains(loc.Location.SourceSpan)) == 0)
                  {
                    covered = false;
                    break;
                  }
                }
              }
            }
          }

          //minden referenciánk le van fedve trycatch-el?
          if (!covered)
          {
            Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
            context.ReportDiagnostic(diagnostic);
          }
        }
      }
    }

    private void CollectTryBlocks(SyntaxNodeAnalysisContext context)
    {
      //Összeszedem az összes try-ban lévő code block helyét.
      tryBlockSpans = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<TryStatementSyntax>().Where(tc => tc.Block != null).Select(tc => tc.Block.FullSpan).ToList();
    }
  }
}
