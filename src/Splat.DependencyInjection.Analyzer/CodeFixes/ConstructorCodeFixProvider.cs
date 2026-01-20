// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.Analyzer.CodeFixes;

/// <summary>
/// Code fix provider that adds [DependencyInjectionConstructor] attribute to a constructor.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorCodeFixProvider))]
[Shared]
public class ConstructorCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var typeDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDeclaration == null)
        {
            return;
        }

        // Find all constructors
        var constructors = typeDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .Where(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword))
            .ToList();

        foreach (var constructor in constructors)
        {
            var parameterCount = constructor.ParameterList.Parameters.Count;
            var title = parameterCount == 0
                ? "Add [DependencyInjectionConstructor] to parameterless constructor"
                : $"Add [DependencyInjectionConstructor] to constructor with {parameterCount} parameter(s)";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddAttributeAsync(context.Document, constructor, c),
                    equivalenceKey: constructor.GetLocation().ToString()),
                diagnostic);
        }
    }

    private static async Task<Document> AddAttributeAsync(
        Document document,
        ConstructorDeclarationSyntax constructor,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Create attribute syntax
        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName("DependencyInjectionConstructor"));

        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(attribute))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // Add attribute to constructor
        var newConstructor = constructor.AddAttributeLists(attributeList);

        // Replace old constructor with new one
        var newRoot = root.ReplaceNode(constructor, newConstructor);

        return document.WithSyntaxRoot(newRoot);
    }
}
