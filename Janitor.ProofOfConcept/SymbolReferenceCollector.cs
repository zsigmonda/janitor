using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using Microsoft.CodeAnalysis.FindSymbols;
using Janitor.BusinessLogic;

namespace Janitor.ProofOfConcept
{
  public class SymbolReferenceCollector : SymbolVisitor
  {
    public readonly List<ISymbol> References = new List<ISymbol>();
    public IMethodSymbol Symbol { get; private set; }
    public SymbolReferenceCollector(IMethodSymbol symbol)
    {
      this.Symbol = symbol;
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
      foreach (var childSymbol in symbol.GetMembers())
      {
        childSymbol.Accept(this);
      }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
      foreach (var childSymbol in symbol.GetMembers())
      {
        childSymbol.Accept(this);
      }
    }

    public override void VisitMethod(IMethodSymbol symbol)
    {
      if (Symbol.Equals(symbol) || symbol.Name == "C")
      {
        References.Add(symbol);
      }
    }
  }
}
