using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Janitor.BusinessLogic;

namespace Janitor
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class RegularExpressionAnalyzer : DiagnosticAnalyzer
  {
    public const string DiagnosticId = "Janitor";

    //Lokalizált string-ek
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.RegularExpressionAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.RegularExpressionAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.RegularExpressionAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = "Syntax";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(AnalyzeRegularExpression, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeRegularExpression(SyntaxNodeAnalysisContext context)
    {
      InvocationExpressionSyntax invocationExpression = (InvocationExpressionSyntax)context.Node;

      MemberAccessExpressionSyntax memberAccessExpr = invocationExpression.Expression as MemberAccessExpressionSyntax;
      if (memberAccessExpr == null) return;
      string memberAccessExprName = memberAccessExpr.Name.ToString();
      if (memberAccessExprName != "Match" && memberAccessExprName != "Matches" && memberAccessExprName != "IsMatch")
        return;

      IMethodSymbol memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr, context.CancellationToken).Symbol as IMethodSymbol;

      if (memberSymbol != null && (
        memberSymbol.ToString().StartsWith("System.Text.RegularExpressions.Regex.Match(") ||
        memberSymbol.ToString().StartsWith("System.Text.RegularExpressions.Regex.Matches(") ||
        memberSymbol.ToString().StartsWith("System.Text.RegularExpressions.Regex.IsMatch(")
        ))
      {
        ArgumentListSyntax argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
        if ((argumentList?.Arguments.Count ?? 0) < 2) return;
        LiteralExpressionSyntax regexLiteral = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;

        if (regexLiteral == null) return;
        var regexOpt = context.SemanticModel.GetConstantValue(regexLiteral, context.CancellationToken);

        if (!regexOpt.HasValue) return;
        var regex = regexOpt.Value as string;
        if (regex == null) return;

        try
        {
          System.Text.RegularExpressions.Regex.Match("", regex);
        }
        catch (ArgumentException e)
        {
          Diagnostic diagnostic = Diagnostic.Create(Rule, regexLiteral.GetLocation(), e.Message);
          context.ReportDiagnostic(diagnostic);
        }
      }
    }
  }
}
