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
/// Code fix provider that fixes property setter accessibility for dependency injection.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyCodeFixProvider))]
[Shared]
public class PropertyCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(SourceGenerator.DiagnosticWarnings.PropertyMustPublicBeSettable.Id);

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

        var propertyDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();

        if (propertyDeclaration == null)
        {
            return;
        }

        // Offer to add public setter
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add public setter",
                createChangedDocument: c => AddPublicSetterAsync(context.Document, propertyDeclaration, c),
                equivalenceKey: nameof(AddPublicSetterAsync)),
            diagnostic);

        // Offer to add internal setter
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add internal setter",
                createChangedDocument: c => AddInternalSetterAsync(context.Document, propertyDeclaration, c),
                equivalenceKey: nameof(AddInternalSetterAsync)),
            diagnostic);
    }

    private static async Task<Document> AddPublicSetterAsync(
        Document document,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken)
    {
        return await AddSetterAsync(document, property, SyntaxKind.PublicKeyword, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> AddInternalSetterAsync(
        Document document,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken)
    {
        return await AddSetterAsync(document, property, SyntaxKind.InternalKeyword, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> AddSetterAsync(
        Document document,
        PropertyDeclarationSyntax property,
        SyntaxKind accessorModifier,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Determine if the setter needs an explicit modifier
        // If the property already has the same accessibility, the setter doesn't need a modifier
        bool needsModifier = true;
        if (property.Modifiers.Any(m => m.IsKind(accessorModifier)))
        {
            needsModifier = false;
        }

        var setterModifiers = needsModifier
            ? SyntaxFactory.TokenList(SyntaxFactory.Token(accessorModifier))
            : SyntaxFactory.TokenList();

        PropertyDeclarationSyntax newProperty;

        if (property.AccessorList == null)
        {
            // Expression-bodied property - convert to property with getter and setter
            var getter = SyntaxFactory.AccessorDeclaration(
                SyntaxKind.GetAccessorDeclaration,
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                SyntaxFactory.Token(SyntaxKind.GetKeyword),
                null,
                SyntaxFactory.ArrowExpressionClause(property.ExpressionBody!.Expression),
                SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var setter = SyntaxFactory.AccessorDeclaration(
                SyntaxKind.SetAccessorDeclaration,
                SyntaxFactory.List<AttributeListSyntax>(),
                setterModifiers,
                SyntaxFactory.Token(SyntaxKind.SetKeyword),
                null,
                null,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var accessorList = SyntaxFactory.AccessorList(
                SyntaxFactory.List(new[] { getter, setter }));

            newProperty = property
                .WithAccessorList(accessorList)
                .WithExpressionBody(null)
                .WithSemicolonToken(default);
        }
        else if (property.AccessorList.Accessors.All(a => a.Kind() != SyntaxKind.SetAccessorDeclaration))
        {
            // Has getter but no setter - add setter
            var setter = SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.SetAccessorDeclaration)
                .WithModifiers(setterModifiers)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var newAccessorList = property.AccessorList.AddAccessors(setter);
            newProperty = property.WithAccessorList(newAccessorList);
        }
        else
        {
            // Has setter with wrong accessibility - update it
            var oldSetter = property.AccessorList.Accessors.First(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
            var newSetter = oldSetter.WithModifiers(setterModifiers);

            var newAccessorList = property.AccessorList.ReplaceNode(oldSetter, newSetter);
            newProperty = property.WithAccessorList(newAccessorList);
        }

        var newRoot = root.ReplaceNode(property, newProperty);
        return document.WithSyntaxRoot(newRoot);
    }
}
