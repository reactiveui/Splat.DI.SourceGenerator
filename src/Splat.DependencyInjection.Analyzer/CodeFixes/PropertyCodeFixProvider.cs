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

/// <summary>Code fix provider that fixes property setter accessibility for dependency injection.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyCodeFixProvider))]
[Shared]
public class PropertyCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [SourceGenerator.DiagnosticWarnings.PropertyMustPublicBeSettable.Id];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // This provider is exported for C# only, and C# documents always support syntax trees, so the root is never null.
        var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;

        var diagnostic = context.Diagnostics[0];

        // Manual ancestor walk to find PropertyDeclarationSyntax
        var node = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
        PropertyDeclarationSyntax? propertyDeclaration = null;
        while (node is not null)
        {
            if (node is PropertyDeclarationSyntax pds)
            {
                propertyDeclaration = pds;
                break;
            }

            node = node.Parent;
        }

        if (propertyDeclaration is null)
        {
            return;
        }

        // Offer to add public setter
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Constants.CodeFixAddPublicSetter,
                createChangedDocument: c => AddSetterAsync(context.Document, propertyDeclaration, SyntaxKind.PublicKeyword, c),
                equivalenceKey: Constants.CodeFixAddPublicSetter),
            diagnostic);

        // Offer to add internal setter
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Constants.CodeFixAddInternalSetter,
                createChangedDocument: c => AddSetterAsync(context.Document, propertyDeclaration, SyntaxKind.InternalKeyword, c),
                equivalenceKey: Constants.CodeFixAddInternalSetter),
            diagnostic);
    }

    /// <summary>
    /// Orchestrates adding or updating a property setter with the specified accessibility modifier.
    /// Determines the property shape and delegates to the appropriate transformation method.
    /// </summary>
    /// <param name="document">The document containing the property.</param>
    /// <param name="property">The property syntax to modify.</param>
    /// <param name="accessorModifier">The <see cref="SyntaxKind"/> accessibility modifier for the setter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The modified document with the updated setter.</returns>
    internal static async Task<Document> AddSetterAsync(
        Document document,
        PropertyDeclarationSyntax property,
        SyntaxKind accessorModifier,
        CancellationToken cancellationToken)
    {
        // Only C# documents reach this method, and C# documents always support syntax trees, so the root is never null.
        var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

        var setterModifiers = BuildSetterModifiers(property, accessorModifier);

        PropertyDeclarationSyntax newProperty;

        if (property.AccessorList is null)
        {
            newProperty = ConvertExpressionBodiedProperty(property, setterModifiers);
        }
        else
        {
            // Check if there's a setter (manual loop to avoid LINQ allocation)
            var accessors = property.AccessorList.Accessors;
            AccessorDeclarationSyntax? existingSetter = null;
            for (var i = 0; i < accessors.Count; i++)
            {
                if (accessors[i].Kind() != SyntaxKind.SetAccessorDeclaration)
                {
                    continue;
                }

                existingSetter = accessors[i];
                break;
            }

            newProperty = existingSetter is null
                ? AddSetterToGetterOnlyProperty(property, setterModifiers)
                : UpdateExistingSetterModifiers(property, existingSetter, setterModifiers);
        }

        var newRoot = root.ReplaceNode(property, newProperty);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Builds the setter modifier token list, omitting the modifier if the property already
    /// has the same accessibility (to avoid redundant modifiers like <c>public int Foo { get; public set; }</c>).
    /// </summary>
    /// <param name="property">The property to check modifiers on.</param>
    /// <param name="accessorModifier">The desired accessibility modifier.</param>
    /// <returns>A token list containing the modifier, or an empty list if redundant.</returns>
    internal static SyntaxTokenList BuildSetterModifiers(PropertyDeclarationSyntax property, SyntaxKind accessorModifier)
    {
        var modifiers = property.Modifiers;
        for (var i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].IsKind(accessorModifier))
            {
                return SyntaxFactory.TokenList();
            }
        }

        return SyntaxFactory.TokenList(SyntaxFactory.Token(accessorModifier));
    }

    /// <summary>
    /// Converts an expression-bodied property to a property with an accessor list containing
    /// both a getter (preserving the expression body) and a setter.
    /// </summary>
    /// <param name="property">The expression-bodied property to convert.</param>
    /// <param name="setterModifiers">The modifier tokens for the setter.</param>
    /// <returns>The transformed property with getter and setter accessors.</returns>
    internal static PropertyDeclarationSyntax ConvertExpressionBodiedProperty(
        PropertyDeclarationSyntax property,
        in SyntaxTokenList setterModifiers)
    {
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

        return property
            .WithAccessorList(accessorList)
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    /// <summary>Adds a setter accessor to a property that only has a getter.</summary>
    /// <param name="property">The getter-only property.</param>
    /// <param name="setterModifiers">The modifier tokens for the setter.</param>
    /// <returns>The property with the setter added.</returns>
    internal static PropertyDeclarationSyntax AddSetterToGetterOnlyProperty(
        PropertyDeclarationSyntax property,
        in SyntaxTokenList setterModifiers)
    {
        var setter = SyntaxFactory.AccessorDeclaration(
                SyntaxKind.SetAccessorDeclaration)
            .WithModifiers(setterModifiers)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var newAccessorList = property.AccessorList!.AddAccessors(setter);
        return property.WithAccessorList(newAccessorList);
    }

    /// <summary>Updates the accessibility modifiers on an existing setter accessor.</summary>
    /// <param name="property">The property containing the setter.</param>
    /// <param name="existingSetter">The existing setter accessor to update.</param>
    /// <param name="setterModifiers">The new modifier tokens for the setter.</param>
    /// <returns>The property with the updated setter modifiers.</returns>
    internal static PropertyDeclarationSyntax UpdateExistingSetterModifiers(
        PropertyDeclarationSyntax property,
        AccessorDeclarationSyntax existingSetter,
        in SyntaxTokenList setterModifiers)
    {
        var newSetter = existingSetter.WithModifiers(setterModifiers);
        var newAccessorList = property.AccessorList!.ReplaceNode(existingSetter, newSetter);
        return property.WithAccessorList(newAccessorList);
    }
}
