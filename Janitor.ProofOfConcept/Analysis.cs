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
  class Analysis
  {
    private string m_sourceCode;

    public Analysis(string inputFileName)
    {
      using (StreamReader sr = new StreamReader(inputFileName))
      {
        m_sourceCode = sr.ReadToEnd();
      }
    }

    public void DoAnalysis()
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


      BlockCollector collector = new BlockCollector();
      collector.Visit(root);

      List<TryStatementSyntax> tryStatements = root.DescendantNodesAndSelf().OfType<TryStatementSyntax>().ToList();

      foreach (BlockSyntax block in collector.Blocks)
      {
        if (tryStatements.Count(span => span.Contains(block)) == 0)
        {
          //nincs trycatch körülötte - meg kell nézni a szülő szimbólum összes referenciáját

          ISymbol _sym = model.GetEnclosingSymbol(block.SpanStart);

          MethodInvocationsCollector mic = new MethodInvocationsCollector(model, _sym as IMethodSymbol, null, false);
          mic.Visit(root);

          bool allReferencesCovered = mic.Invocations.Count > 0;
          foreach (var micres in mic.Invocations)
          {
            if (tryStatements.Count(span => span.Contains(micres.InvocationSyntax)) == 0)
            {
              allReferencesCovered = false;
              break;
            }
          }

          if (!allReferencesCovered)
          {
            Console.WriteLine("Block not covered: {0}", block.GetLocation());
          }
        }
      }


      Console.ReadKey();
    }
  }

  class BlockCollector : CSharpSyntaxWalker
  {
    public readonly List<BlockSyntax> Blocks = new List<BlockSyntax>();
    public override void VisitBlock(BlockSyntax node)
    {
      Blocks.Add(node);
      base.VisitBlock(node);
    }
  }
}
