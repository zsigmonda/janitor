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
  public class Transformation
  {
    private string m_sourceCode;

    public Transformation(string inputFileName)
    {
      using (StreamReader sr = new StreamReader(inputFileName))
      {
        m_sourceCode = sr.ReadToEnd();
      }
    }
    public void DoTransformation()
    {
      var refMscorlib = PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location);

      //Build solution
      AdhocWorkspace ws = new AdhocWorkspace();

      //Create new solution
      var solId = SolutionId.CreateNewId();
      var solutionInfo = SolutionInfo.Create(solId, VersionStamp.Create());

      //Create new project
      var project = ws.AddProject("Sample", "C#");
      project = project.AddMetadataReference(refMscorlib);
      //Add project to workspace
      bool ok = ws.TryApplyChanges(project.Solution);

      var sourceText = SourceText.From(m_sourceCode);
      //Create new document
      var doc = ws.AddDocument(project.Id, "NewDoc", sourceText);

      SyntaxTree tree = CSharpSyntaxTree.ParseText(m_sourceCode);
      var root = (CompilationUnitSyntax)tree.GetRoot();

      var compilation = CSharpCompilation.Create("HelloWorld", new[] { tree }, new[] { refMscorlib });
      SemanticModel model = compilation.GetSemanticModel(tree);

      //a blokkot körül kell venni egy try-catch-el: a bemenetként átadott blokk köré kerül egy tc, köré mégegy blokk, és ezt cseréljük le az eredetivel.

      //a solution immutable - emiatt létrehozzuk az újat, amiben megtörtént a blokk felvétele, és visszaadjuk azt.
      Solution originalSolution = doc.Project.Solution;
      Microsoft.CodeAnalysis.Editing.SyntaxEditor syntaxEditor = new Microsoft.CodeAnalysis.Editing.SyntaxEditor(root, doc.Project.Solution.Workspace);

      BlockSyntax codeBlock = root.DescendantNodesAndSelf().OfType<BlockSyntax>().First();

      SyntaxNode syn = SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("System.Exception"));

      SyntaxList <CatchClauseSyntax> catchClauses = new SyntaxList<CatchClauseSyntax>().Add
        (SyntaxFactory.CatchClause(      
          SyntaxFactory.Token(SyntaxKind.CatchKeyword),
          SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("System.Exception")),
          null,
          SyntaxFactory.Block()));

      SyntaxNode newNode = SyntaxFactory.Block(
        SyntaxFactory.TryStatement(
          SyntaxFactory.Token(SyntaxKind.TryKeyword),
          codeBlock,
          catchClauses,
          null));

      syntaxEditor.ReplaceNode(codeBlock, newNode);

      // Return the new solution with the now-uppercase type name.
      Solution newSolution = originalSolution.WithDocumentSyntaxRoot(doc.Id, syntaxEditor.GetChangedRoot());

      Console.ReadKey();
    }
  }
}
