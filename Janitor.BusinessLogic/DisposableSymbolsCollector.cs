using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Janitor.BusinessLogic
{
  /// <summary>
  /// Ez az osztály tárolja egy szimbólumról, hogy melyik metódusa származik az IDisposable interfészből.
  /// </summary>
  public class DisposableSymbolData
  {
    /// <summary>
    /// A szimbólum, amely implementálja az IDisposable interfészt.
    /// </summary>
    public ISymbol DisposableSymbol { get; set; }
    
    /// <summary>
    /// A szimbólumhoz tartozó public void Dispose() metódus szimbóluma.
    /// </summary>
    public IMethodSymbol DisposeMethodSymbol { get; set; }
  }

  /// <summary>
  /// Ez az osztály képes kigyűjteni az összes olyan szimbólumot egy modellből, amely implementálja az IDisposable interfészt.
  /// </summary>
  public class DisposableSymbolsCollector : CSharpSyntaxWalker
  {
    /// <summary>
    /// Azon szimbólumok listája, amelyek implementálják az IDisposable interfészt.
    /// </summary>
    public readonly List<DisposableSymbolData> SymbolsRequiringDispose = new List<DisposableSymbolData>();

    private readonly SymbolDisplayFormat sdf = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    /// <summary>
    /// A szimbólumokat tartalmazó modell, amelyen az elemzést végezzük. Csak konstruktorban adható meg.
    /// </summary>
    public SemanticModel Model { get; private set; }

    /// <summary>
    /// A cancellationToken objektum, amellyel az elemzés futása megszakítható. Csak konstruktorban adható meg.
    /// </summary>
    public System.Threading.CancellationToken CancellationToken { get; private set; }

    /// <summary>
    /// Létrehozza az osztály egy példányát, és elvégzi annak inicializálását.
    /// </summary>
    /// <param name="model">A szimbólumokat tartalmazó modell, amelyen az elemzést végezzük.</param>
    /// <param name="cancellationToken">A cancellationToken objektum, amellyel az elemzés futása megszakítható.</param>
    public DisposableSymbolsCollector(SemanticModel model, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
      this.Model = model;
      this.CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Feldolgoz egy helyi változót vagy tagváltozót deklaráló syntaxnode-ot, és megmondja, hogy az ott deklarált szimbólum implementálja-e az IDisposable interfészt.
    /// </summary>
    /// <param name="node">A syntaxnode, amelyet elemeztetni szeretnénk.</param>
    /// <returns>A visszatérési érték a szimbólum, hogyha implementálja az IDisposable-t. Ha nem, akkor null.</returns>
    public DisposableSymbolData ProcessVariableDeclarator(VariableDeclaratorSyntax node)
    {
      ISymbol info = Model.GetDeclaredSymbol(node, CancellationToken);

      if (info is IFieldSymbol)
      {
        IFieldSymbol fieldSymbol = info as IFieldSymbol;
        INamedTypeSymbol intface = fieldSymbol.Type.AllInterfaces.SingleOrDefault(iface => iface.Name == "IDisposable" && iface.ToDisplayString(sdf) == "System.IDisposable");
        if (intface != null)
        {
          ISymbol disposeMethod = intface.GetMembers("Dispose").FirstOrDefault();
          ISymbol implDisposeMethod = fieldSymbol.Type.FindImplementationForInterfaceMember(disposeMethod);

          return new DisposableSymbolData() { DisposableSymbol = fieldSymbol, DisposeMethodSymbol = implDisposeMethod as IMethodSymbol };
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

          return new DisposableSymbolData() { DisposableSymbol = localSymbol, DisposeMethodSymbol = implDisposeMethod as IMethodSymbol };
        }
      }

      return null;
    }

    /// <summary>
    /// Feldolgoz egy property-t deklaráló syntaxnode-ot, és megmondja, hogy az ott deklarált szimbólum implementálja-e az IDisposable interfészt.
    /// </summary>
    /// <param name="node">A syntaxnode, amelyet elemeztetni szeretnénk.</param>
    /// <returns>A visszatérési érték a szimbólum, hogyha implementálja az IDisposable-t. Ha nem, akkor null.</returns>
    public DisposableSymbolData ProcessPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      ISymbol info = Model.GetDeclaredSymbol(node, CancellationToken);

      if (info is IPropertySymbol)
      {
        IPropertySymbol propSymbol = info as IPropertySymbol;
        INamedTypeSymbol intface = propSymbol.Type.AllInterfaces.SingleOrDefault(iface => iface.Name == "IDisposable" && iface.ToDisplayString(sdf) == "System.IDisposable");
        if (intface != null)
        {
          ISymbol disposeMethod = intface.GetMembers("Dispose").FirstOrDefault();
          ISymbol implDisposeMethod = propSymbol.Type.FindImplementationForInterfaceMember(disposeMethod);

          return new DisposableSymbolData() { DisposableSymbol = propSymbol, DisposeMethodSymbol = implDisposeMethod as IMethodSymbol };
        }
      }

      return null;
    }

    public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
      DisposableSymbolData data = ProcessVariableDeclarator(node);

      if (data != null)
      {
        SymbolsRequiringDispose.Add(data);
      }
      
      base.VisitVariableDeclarator(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      DisposableSymbolData data = ProcessPropertyDeclaration(node);

      if (data != null)
      {
        SymbolsRequiringDispose.Add(data);
      }

      base.VisitPropertyDeclaration(node);
    }
  }
}
