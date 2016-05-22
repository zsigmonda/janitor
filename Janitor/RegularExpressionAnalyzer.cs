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
    public const string DiagnosticId = "JN0003";

    //Lokalizált string-ek
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.RegularExpressionAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.RegularExpressionAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.RegularExpressionAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = "Syntax";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
    private static SymbolDisplayFormat sdf = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSyntaxNodeAction(AnalyzeRegularExpression, SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);
    }

    private void AnalyzeRegularExpression(SyntaxNodeAnalysisContext context)
    {
      IMethodSymbol methodSymbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol as IMethodSymbol;

      if (methodSymbol != null && (methodSymbol.Name == "Match" || methodSymbol.Name == "IsMatch" || methodSymbol.Name == "Matches" || methodSymbol.Name == ".ctor"))
      {
        if (methodSymbol.ContainingType != null && methodSymbol.ContainingType.ToDisplayString(sdf) == "System.Text.RegularExpressions.Regex")
        {
          IParameterSymbol patternParameter = methodSymbol.Parameters.SingleOrDefault(p => p.Name == "pattern");

          if (patternParameter != null)
          {
            ArgumentListSyntax argumentList = null;
            if (context.Node.Kind() == SyntaxKind.ObjectCreationExpression)
            {
              argumentList = (context.Node as ObjectCreationExpressionSyntax).ArgumentList as ArgumentListSyntax;
            }
            else
            {
              if (context.Node.Kind() == SyntaxKind.InvocationExpression)
              {
                argumentList = (context.Node as InvocationExpressionSyntax).ArgumentList as ArgumentListSyntax;
              }
            }

            if (argumentList != null)
            {
              LiteralExpressionSyntax regexLiteral = argumentList.Arguments[patternParameter.Ordinal].Expression as LiteralExpressionSyntax;

              if (regexLiteral != null)
              {
                var regexOpt = context.SemanticModel.GetConstantValue(regexLiteral, context.CancellationToken);

                if (regexOpt.HasValue)
                {
                  string regex = regexOpt.Value as string;
                  if (regex != null)
                  {

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
          }
        }
      }
    }
  }
}
