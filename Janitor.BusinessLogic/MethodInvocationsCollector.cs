using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Janitor.BusinessLogic
{
  public class MethodInvocationData
  {
    public IMethodSymbol InvokedMethodSymbol { get; set; }
    public ISymbol InvokerSymbol { get; set; }
    public InvocationExpressionSyntax InvocationSyntax { get; set; }
  }

  public class MethodInvocationsCollector : CSharpSyntaxWalker
  {
    public readonly List<MethodInvocationData> Invocations = new List<MethodInvocationData>();

    public SemanticModel Model { get; private set; }
    public IMethodSymbol InvokedMethod { get; private set; }
    public ISymbol FieldOrLocalVariable { get; private set; }
    public bool IgnoreUnreachable { get; private set; }

    public MethodInvocationsCollector(SemanticModel model, IMethodSymbol method, ISymbol containingObject, bool ignoreUnreachable = true)
    {
      this.Model = model;
      this.InvokedMethod = method;
      this.FieldOrLocalVariable = containingObject;
      this.IgnoreUnreachable = ignoreUnreachable;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
      IMethodSymbol methodSymbol = Model.GetSymbolInfo(node).Symbol as IMethodSymbol;
      if (methodSymbol != null && methodSymbol.Equals(InvokedMethod))
      {
        IdentifierNameSyntax objectName = node.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();

        ISymbol objectSymbol = Model.GetSymbolInfo(objectName).Symbol;
        if (objectSymbol != null && objectSymbol.Equals(FieldOrLocalVariable))
        {
          if (IgnoreUnreachable)
          {
            ControlFlowAnalysis cfa = Model.AnalyzeControlFlow(node.Ancestors().OfType<StatementSyntax>().FirstOrDefault());
            if (cfa.Succeeded && cfa.StartPointIsReachable && cfa.EndPointIsReachable)
              Invocations.Add(new MethodInvocationData() { InvocationSyntax = node, InvokedMethodSymbol = methodSymbol, InvokerSymbol = objectSymbol });
          }
          else
          {
            Invocations.Add(new MethodInvocationData() { InvocationSyntax = node, InvokedMethodSymbol = methodSymbol, InvokerSymbol = objectSymbol });
          }
        }
      }
      base.VisitInvocationExpression(node);
    }
  }
}
