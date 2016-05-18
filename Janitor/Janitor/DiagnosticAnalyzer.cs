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

namespace Janitor
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class JanitorAnalyzer : DiagnosticAnalyzer
  {
    public const string DiagnosticId = "Janitor";

    // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
    // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
    private const string Category = "Performance";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
      // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
      // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
      context.RegisterSemanticModelAction(AnalyzeDispose);
    }

    private static void AnalyzeDispose(SemanticModelAnalysisContext context)
    {
      SyntaxTree tree = context.SemanticModel.SyntaxTree;
      SemanticModel model = context.SemanticModel;

      DisposableSymbolsCollector walker = new DisposableSymbolsCollector(model);
      walker.Visit(tree.GetRoot());

      UsingStatementsCollector upc = new UsingStatementsCollector(model);
      upc.Visit(tree.GetRoot());

      foreach (var item in walker.SymbolsRequiringDispose)
      {
        MethodInvocationsCollector mic = new MethodInvocationsCollector(model, item.DisposeMethodSymbol, item.DisposableSymbol);
        mic.Visit(tree.GetRoot());

        if (mic.Invocations.Count == 0 && !upc.SymbolsWithUsingPattern.Contains(item.DisposableSymbol))
        {
          var diagnostic = Diagnostic.Create(Rule, item.DisposableSymbol.Locations[0], item.DisposableSymbol.Name);
          context.ReportDiagnostic(diagnostic);
        }
      }
    }
  }
}
