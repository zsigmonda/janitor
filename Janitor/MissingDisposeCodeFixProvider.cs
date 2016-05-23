using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Janitor
{
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingDisposeCodeFixProvider)), Shared]
  public class MissingDisposeCodeFixProvider : CodeFixProvider
  {
    private const string title = "Add surrounding Using statement";

    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
      get
      {
        return ImmutableArray.Create(MissingTryCatchAnalyzer.DiagnosticId);
      }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
      return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

      foreach (Diagnostic diagnostic in context.Diagnostics)
      {
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
        BlockSyntax codeBlock = root.FindNode(diagnosticSpan) as BlockSyntax;

        //Regisztráljuk a codefix-et mindegyik diagnostic objecthez
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedSolution: c => FixMissingTryCatchAsync(context.Document, codeBlock, c),
                equivalenceKey: title),
            diagnostic);
      }
    }

    private async Task<Solution> FixMissingTryCatchAsync(Document document, BlockSyntax codeBlock, CancellationToken cancellationToken)
    {
      Solution originalSolution = document.Project.Solution;
      Solution newSolution = originalSolution;

      try
      {
        SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
        //A blokkot körül kell venni egy try-catch-el: a bemenetként átadott blokk köré kerül egy tc, köré mégegy blokk, és ezt cseréljük le az eredetivel.

        //A solution immutable - emiatt létrehozzuk az újat, amiben megtörtént a blokk felvétele, és visszaadjuk azt.

        Microsoft.CodeAnalysis.Editing.SyntaxEditor syntaxEditor = new Microsoft.CodeAnalysis.Editing.SyntaxEditor(root, document.Project.Solution.Workspace);

        SyntaxList<CatchClauseSyntax> catchClauses = new SyntaxList<CatchClauseSyntax>().Add
          (SyntaxFactory.CatchClause(
            SyntaxFactory.Token(SyntaxKind.CatchKeyword),
            SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("Exception")),
            null,
            SyntaxFactory.Block(SyntaxFactory.ThrowStatement())));

        SyntaxNode newNode = SyntaxFactory.Block(
          SyntaxFactory.TryStatement(
            SyntaxFactory.Token(SyntaxKind.TryKeyword),
            codeBlock,
            catchClauses,
            null));

        //Meg is kell formázni a kódot, hogy szépen nézzen ki
        syntaxEditor.ReplaceNode(codeBlock, newNode.WithAdditionalAnnotations(Formatter.Annotation));

        newSolution = originalSolution.WithDocumentSyntaxRoot(document.Id, syntaxEditor.GetChangedRoot());
      }
      catch (Exception ex)
      {
        //TODO Output windowba írás
      }
      return newSolution;
    }
  }
}
