using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Janitor
{
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingTryCatchCodeFixProvider)), Shared]
  public class MissingTryCatchCodeFixProvider : CodeFixProvider
  {
    private const string title = "Add Try statement with a Catch clause";

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
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

      foreach (Diagnostic diagnostic in context.Diagnostics)
      {
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        BlockSyntax codeBlock = root.FindNode(diagnosticSpan) as BlockSyntax;

        // Register a code action that will invoke the fix.
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
      SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
      //a blokkot körül kell venni egy try-catch-el: a bemenetként átadott blokk köré kerül egy tc, köré mégegy blokk, és ezt cseréljük le az eredetivel.

      //a solution immutable - emiatt létrehozzuk az újat, amiben megtörtént a blokk felvétele, és visszaadjuk azt.
      Solution originalSolution = document.Project.Solution;
      Microsoft.CodeAnalysis.Editing.SyntaxEditor syntaxEditor = new Microsoft.CodeAnalysis.Editing.SyntaxEditor(root, document.Project.Solution.Workspace);

      SyntaxList<CatchClauseSyntax> catchClauses = new SyntaxList<CatchClauseSyntax>().Add
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

      Solution newSolution = originalSolution.WithDocumentSyntaxRoot(document.Id, syntaxEditor.GetChangedRoot());
      return newSolution;
    }
  }
}
