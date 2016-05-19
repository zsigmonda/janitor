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
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingDisposeCodeFixProvider)), Shared]
  public class MissingDisposeCodeFixProvider : CodeFixProvider
  {
    private const string title = "Add surrounding using statement";

    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
      get { return ImmutableArray.Create(MissingDisposeAnalyzer.DiagnosticId); }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
      return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

      // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
      var diagnostic = context.Diagnostics.First();
      var diagnosticSpan = diagnostic.Location.SourceSpan;

      // Find the type declaration identified by the diagnostic.
      var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().First();

      // Register a code action that will invoke the fix.
      context.RegisterCodeFix(
          CodeAction.Create(
              title: title,
              createChangedSolution: c => FixMissingDisposeAsync(context.Document, declaration, c),
              equivalenceKey: title),
          diagnostic);
    }

    private async Task<Solution> FixMissingDisposeAsync(Document document, VariableDeclaratorSyntax disposableVariable, CancellationToken cancellationToken)
    {
      return document.Project.Solution;
    }
  }
}