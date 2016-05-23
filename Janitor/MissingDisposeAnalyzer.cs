using Janitor.BusinessLogic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Janitor
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class MissingDisposeAnalyzer : DiagnosticAnalyzer
  {
    public const string DiagnosticId = "JN0001";
    
    //Lokalizált string-ek
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.MissingDisposeAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.MissingDisposeAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.MissingDisposeAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = "Performance";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(AnalyzeMissingDispose, SyntaxKind.VariableDeclarator, SyntaxKind.PropertyDeclaration);
      Logger.Instance.LogInformation("MissingDisposeAnalyzer registered.");
    }

    private void AnalyzeMissingDispose(SyntaxNodeAnalysisContext context)
    {
      try
      {
        SyntaxTree tree = context.SemanticModel.SyntaxTree;
        SemanticModel model = context.SemanticModel;

        DisposableSymbolsCollector collector = new DisposableSymbolsCollector(model, context.CancellationToken);

        DisposableSymbolData data = null;

        if (context.Node.Kind() == SyntaxKind.VariableDeclarator)
        {
          data = collector.ProcessVariableDeclarator(context.Node as VariableDeclaratorSyntax);
        }
        else
        {
          data = collector.ProcessPropertyDeclaration(context.Node as PropertyDeclarationSyntax);
        }

        if (data != null)
        {
          UsingStatementsCollector upc = new UsingStatementsCollector(model, true, context.CancellationToken);
          upc.Visit(tree.GetRoot());

          context.CancellationToken.ThrowIfCancellationRequested();

          MethodInvocationsCollector mic = new MethodInvocationsCollector(model, data.DisposeMethodSymbol, data.DisposableSymbol, true, context.CancellationToken);
          mic.Visit(tree.GetRoot());

          if (mic.Invocations.Count == 0 && !upc.SymbolsWithUsingPattern.Contains(data.DisposableSymbol))
          {
            Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), data.DisposableSymbol.Name);
            context.ReportDiagnostic(diagnostic);
          }
        }
      }
      catch (OperationCanceledException)
      {
        Logger.Instance.LogInformation("Operation cancelled through CancellationToken.");
      }
      catch (Exception ex)
      {
        Logger.Instance.LogError("Error occured.", ex);
      }
    }
  }
}
