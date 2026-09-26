// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Splat.DependencyInjection.Analyzer.CodeFixes;

/// <summary>Code fix provider that adds [DependencyInjectionConstructor] attribute to a constructor.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorCodeFixProvider))]
[Shared]
public class ConstructorCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute.Id];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // This provider is exported for C# only, and C# documents always support syntax trees, so the root is never null.
        var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;

        var diagnostic = context.Diagnostics[0];

        // Every token found in a syntax tree has a parent node (at least the compilation unit).
        var typeDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent!.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDeclaration is null)
        {
            return;
        }

        foreach (var member in typeDeclaration.Members)
        {
            if (member is not ConstructorDeclarationSyntax constructor || constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                continue;
            }

            var parameterCount = constructor.ParameterList.Parameters.Count;
            var title = parameterCount == 0
                ? $"Add [{SourceGenerator.Constants.ConstructorAttributeShortName}] to parameterless constructor"
                : $"Add [{SourceGenerator.Constants.ConstructorAttributeShortName}] to constructor with {parameterCount} parameter(s)";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddAttributeAsync(context.Document, constructor, c),
                    equivalenceKey: constructor.GetLocation().ToString()),
                diagnostic);
        }
    }

    /// <summary>Adds the [DependencyInjectionConstructor] attribute to the specified constructor.</summary>
    /// <param name="document">The document containing the constructor.</param>
    /// <param name="constructor">The constructor syntax to add the attribute to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The modified document with the attribute added.</returns>
    internal static async Task<Document> AddAttributeAsync(
        Document document,
        ConstructorDeclarationSyntax constructor,
        CancellationToken cancellationToken)
    {
        // Only C# documents reach this method, and C# documents always support syntax trees, so the root is never null.
        var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

        // Create attribute syntax
        var attribute = SyntaxFactory.Attribute(
            SyntaxFactory.ParseName(SourceGenerator.Constants.ConstructorAttributeShortName));

        // If the constructor has no existing attributes and has leading trivia (like XML documentation),
        // we need to move that trivia to the new attribute list
        AttributeListSyntax attributeList;
        ConstructorDeclarationSyntax newConstructor;

        if (constructor.AttributeLists.Count == 0 && constructor.HasLeadingTrivia)
        {
            // Move leading trivia to the attribute list
            var leadingTrivia = constructor.GetLeadingTrivia();
            attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attribute))
                .WithLeadingTrivia(leadingTrivia);

            // Remove trivia from constructor and add attribute
            newConstructor = constructor
                .WithoutLeadingTrivia()
                .WithAttributeLists(SyntaxFactory.SingletonList(attributeList));
        }
        else
        {
            // No leading trivia, just insert at the beginning
            attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attribute));

            var newAttributeLists = constructor.AttributeLists.Insert(0, attributeList);
            newConstructor = constructor.WithAttributeLists(newAttributeLists);
        }

        // Replace old constructor with new one
        var newRoot = root.ReplaceNode(constructor, newConstructor);

        return document.WithSyntaxRoot(newRoot);
    }
}
