using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Janitor.ProofOfConcept
{
  public class DisposableSymbolsCollector : CSharpSyntaxWalker
  {
    public readonly Dictionary<ISymbol, IMethodSymbol> SymbolsRequiringDispose = new Dictionary<ISymbol, IMethodSymbol>();

    private readonly SymbolDisplayFormat sdf = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public SemanticModel Model { get; private set; }

    public DisposableSymbolsCollector(SemanticModel model)
    {
      this.Model = model;
    }

    public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
      ISymbol info = Model.GetDeclaredSymbol(node);     

      if (info is IFieldSymbol)
      {
        IFieldSymbol fieldSymbol = info as IFieldSymbol;
        INamedTypeSymbol intface = fieldSymbol.Type.AllInterfaces.SingleOrDefault(iface => iface.Name == "IDisposable" && iface.ToDisplayString(sdf) == "System.IDisposable");
        if (intface != null)
        {
          ISymbol disposeMethod = intface.GetMembers("Dispose").FirstOrDefault();
          ISymbol implDisposeMethod = fieldSymbol.Type.FindImplementationForInterfaceMember(disposeMethod);

          SymbolsRequiringDispose.Add(fieldSymbol, implDisposeMethod as IMethodSymbol);
        }
      }

      if (info is ILocalSymbol)
      {
        ILocalSymbol localSymbol = info as ILocalSymbol;
        INamedTypeSymbol intface = localSymbol.Type.AllInterfaces.SingleOrDefault(iface => iface.Name == "IDisposable" && iface.ToDisplayString(sdf) == "System.IDisposable");
        if (intface != null)
        {
          ISymbol disposeMethod = intface.GetMembers("Dispose").FirstOrDefault();
          ISymbol implDisposeMethod = localSymbol.Type.FindImplementationForInterfaceMember(disposeMethod);

          SymbolsRequiringDispose.Add(localSymbol, implDisposeMethod as IMethodSymbol);
        }
      }
      
      base.VisitVariableDeclarator(node);
    }
  }
}
