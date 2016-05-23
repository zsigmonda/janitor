using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Janitor.BusinessLogic
{
  /// <summary>
  /// Ez az osztály tárolja egy szimbólumról, hogy egy metódusát melyik syntaxnode-ban hívják meg.
  /// </summary>
  public class MethodInvocationData
  {
    /// <summary>
    /// A keresett metódushoz tartozó szimbólum.
    /// </summary>
    public IMethodSymbol InvokedMethodSymbol { get; set; }
    
    /// <summary>
    /// Annak az objektumnak vagy osztálynak (named type) a szimbóluma, amelyhez a keresett metódus tartozik.
    /// </summary>
    public ISymbol InvokerSymbol { get; set; }

    /// <summary>
    /// A syntaxnode, amelyben a keresett metódust hívják.
    /// </summary>
    public InvocationExpressionSyntax InvocationSyntax { get; set; }
  }

  /// <summary>
  /// Ez az osztály megmondja egy neki átadott metódus-szimbólumról, hogy hol hivatkoznak rá (hol hívják meg).
  /// </summary>
  public class MethodInvocationsCollector : CSharpSyntaxWalker
  {
    /// <summary>
    /// A konstruktorban átadott metódusra mutató hivatkozások.
    /// </summary>
    public readonly List<MethodInvocationData> Invocations = new List<MethodInvocationData>();

    /// <summary>
    /// A szimbólumokat tartalmazó modell, amelyen az elemzést végezzük. Csak konstruktorban adható meg.
    /// </summary>
    public SemanticModel Model { get; private set; }
    
    /// <summary>
    /// A metódus szimbóluma, amelynek a hivatkozásaira kíváncsiak vagyunk. Csak konstruktorban adható meg.
    /// </summary>
    public IMethodSymbol InvokedMethod { get; private set; }
    
    /// <summary>
    /// Az objektum szimbóluma, melyre szűkíteni szeretnénk a keresést. Csak konstruktorban adható meg.
    /// </summary>
    public ISymbol ContainingObject { get; private set; }

    /// <summary>
    /// A nem elérhető metódushívások figyelmen kívül hagyása. Csak konstruktorban adható meg.
    /// </summary>
    public bool IgnoreUnreachable { get; private set; }
    
    /// <summary>
    /// A cancellationToken objektum, amellyel az elemzés futása megszakítható. Csak konstruktorban adható meg.
    /// </summary>
    public System.Threading.CancellationToken CancellationToken { get; private set; }

    /// <summary>
    /// Létrehozza az osztály egy példányát, és elvégzi annak inicializálását.
    /// </summary>
    /// <param name="model">A szimbólumokat tartalmazó modell, amelyen az elemzést végezzük.</param>
    /// <param name="method">A metódus szimbóluma, amelynek a hivatkozásaira kíváncsiak vagyunk.</param>
    /// <param name="containingObject">Az objektum szimbóluma, melyre szűkíteni szeretnénk a keresést. Ennek hiányában az osztályra végezzük el a keresést.</param>
    /// <param name="ignoreUnreachable">Ez a paraméter megmnodja, hogy figyelmen kívül hagyjuk-e az elérhetetlen hívásokat: true esetén nem adjuk vissza őket, false esetén pedig igen.</param>
    /// <param name="cancellationToken">A cancellationToken objektum, amellyel az elemzés futása megszakítható.</param>
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
