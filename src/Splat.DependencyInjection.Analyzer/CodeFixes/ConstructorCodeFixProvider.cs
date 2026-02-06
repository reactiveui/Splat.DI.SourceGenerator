// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
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

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Manual ancestor walk to find TypeDeclarationSyntax
        var node = root.FindToken(diagnosticSpan.Start).Parent;
        TypeDeclarationSyntax? typeDeclaration = null;
        while (node != null)
        {
            if (node is TypeDeclarationSyntax tds)
            {
                typeDeclaration = tds;
                break;
            }

            node = node.Parent;
        }

        if (typeDeclaration == null)
        {
            return;
        }

        // Find all non-static constructors (manual loop to avoid LINQ allocations)
        var constructors = new System.Collections.Generic.List<ConstructorDeclarationSyntax>(capacity: 4);
        var members = typeDeclaration.Members;
        for (var i = 0; i < members.Count; i++)
        {
            if (members[i] is ConstructorDeclarationSyntax ctor)
            {
                // Check if not static
                var isStatic = false;
                var modifiers = ctor.Modifiers;
                for (var j = 0; j < modifiers.Count; j++)
                {
                    if (modifiers[j].IsKind(SyntaxKind.StaticKeyword))
                    {
                        isStatic = true;
                        break;
                    }
                }

                if (!isStatic)
                {
                    constructors.Add(ctor);
                }
            }
        }

        foreach (var constructor in constructors)
        {
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
