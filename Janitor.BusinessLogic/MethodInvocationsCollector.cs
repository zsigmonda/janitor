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
    public ISymbol ContainingObject { get; private set; }
    public bool IgnoreUnreachable { get; private set; }
    public System.Threading.CancellationToken CancellationToken { get; private set; }

    public MethodInvocationsCollector(SemanticModel model, IMethodSymbol method, ISymbol containingObject = null, bool ignoreUnreachable = true, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
      this.Model = model;
      this.InvokedMethod = method;
      this.ContainingObject = containingObject;
      this.IgnoreUnreachable = ignoreUnreachable;
      this.CancellationToken = cancellationToken;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
      IMethodSymbol methodSymbol = Model.GetSymbolInfo(node, CancellationToken).Symbol as IMethodSymbol;
      if (methodSymbol != null && methodSymbol.Equals(InvokedMethod))
      {
        //a szimbólum, aminek a metódusára hivatkozunk (ez egy tagváltozó vagy valamilyen objektum-referencia lesz)
        ISymbol objectSymbol = null;

        if (ContainingObject != null)
        {
          //szűrünk egy adott objektumon történő metódushívásra
          IdentifierNameSyntax objectName = node.Expression.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
          objectSymbol = Model.GetSymbolInfo(objectName).Symbol;
        }
        else
        {
          //nem szűrünk típusra: visszaadom az osztály szimbólumát
          objectSymbol = methodSymbol.ContainingSymbol;
        }
        
        if (objectSymbol != null && (objectSymbol.Equals(ContainingObject) || ContainingObject == null))
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
