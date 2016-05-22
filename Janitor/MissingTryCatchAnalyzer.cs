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
    public const string DiagnosticId = "JN0002";

    //Lokalizált string-ek
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.MissingTryCatchAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.MissingTryCatchAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.MissingTryCatchAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = "Performance";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
    private List<TryStatementSyntax> tryStatements = null;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(CollectTryBlocks, SyntaxKind.CompilationUnit);
      context.RegisterSyntaxNodeAction(AnalyzeMissingTryCatch, SyntaxKind.Block);
    }

    private void AnalyzeMissingTryCatch(SyntaxNodeAnalysisContext context)
    {
      if (tryStatements == null)
      {
        tryStatements = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<TryStatementSyntax>().ToList();
      }

      if (tryStatements.Count(span => span.Contains(context.Node)) == 0)
      {
        //nincs try-catch a blokk körül: lehet, hogy nem is kell köré
        //ez akkor fordulhat elő, hogyha a blokkban csak és kizárólag trystatement van.
        if (context.Node.ChildNodes().Count(cn => cn.Kind() != SyntaxKind.TryStatement) > 0)
        {
          //meg kell nézni a szülő szimbólum összes referenciáját
          IMethodSymbol _sym = context.ContainingSymbol as IMethodSymbol;

          if (_sym != null)
          {
            MethodInvocationsCollector mic = new MethodInvocationsCollector(context.SemanticModel, _sym, null, false, context.CancellationToken);
            mic.Visit(context.SemanticModel.SyntaxTree.GetRoot());

            bool allReferencesCovered = mic.Invocations.Count > 0;
            foreach (var micres in mic.Invocations)
            {
              if (tryStatements.Count(span => span.Contains(micres.InvocationSyntax)) == 0)
              {
                allReferencesCovered = false;
                break;
              }
            }

            if (!allReferencesCovered)
            {
              Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
              context.ReportDiagnostic(diagnostic);
            }
          }
        }
      }
    }

    private void CollectTryBlocks(SyntaxNodeAnalysisContext context)
    {
      //Összeszedem az összes try-ban lévő code block helyét.
      tryStatements = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodesAndSelf().OfType<TryStatementSyntax>().ToList();
    }
  }
}
