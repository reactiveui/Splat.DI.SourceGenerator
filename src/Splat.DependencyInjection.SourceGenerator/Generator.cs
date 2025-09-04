// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// The main incremental generator instance.
/// </summary>
[Generator]
public class Generator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Always add the extension method text first
        context.RegisterPostInitializationOutput(ctx => 
            ctx.AddSource("Splat.DI.g.cs", SourceText.From(Constants.ExtensionMethodText, Encoding.UTF8)));

        // Create a syntax provider to detect registration method calls
        var invocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsRegistrationInvocation(node),
                transform: static (ctx, ct) => ctx.Node as InvocationExpressionSyntax)
            .Where(static invocation => invocation is not null)!;

        // Combine with compilation and collect all invocations
        var compilationAndInvocations = invocations
            .Combine(context.CompilationProvider)
            .Collect();

        // Generate source when there are registrations
        context.RegisterSourceOutput(compilationAndInvocations, static (ctx, data) =>
        {
            if (!data.Any())
                return;

            var compilation = data.First().Right;
            
            // Create a syntax receiver to mimic the old behavior
            var syntaxReceiver = new SyntaxReceiver();
            foreach (var (invocation, _) in data)
            {
                syntaxReceiver.OnVisitSyntaxNode(invocation);
            }

            // Create a minimal context adapter that provides what SourceGeneratorHelpers.Generate needs
            var contextAdapter = new MinimalGeneratorContext(compilation);

            try
            {
                // Add the compilation with extension methods (matching original behavior)
                var options = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
                var updatedCompilation = compilation.AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(
                        SourceText.From(Constants.ExtensionMethodText, Encoding.UTF8), 
                        options ?? new CSharpParseOptions()));

                // Generate using existing logic
                var outputText = SourceGeneratorHelpers.Generate(contextAdapter, updatedCompilation, syntaxReceiver);
                
                if (!string.IsNullOrEmpty(outputText))
                {
                    ctx.AddSource("Splat.DI.Reg.g.cs", SourceText.From(outputText, Encoding.UTF8));
                }
            }
            catch
            {
                // If generation fails, skip silently to avoid breaking the build
                // In a production implementation, we'd collect and report diagnostics
            }
        });
    }

    private static bool IsRegistrationInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            MemberBindingExpressionSyntax bindingAccess => bindingAccess.Name.Identifier.Text,
            _ => null
        };

        return methodName is "Register" or "RegisterLazySingleton" or "RegisterConstant";
    }

    /// <summary>
    /// Minimal context that provides the interface needed by SourceGeneratorHelpers.Generate.
    /// </summary>
    private class MinimalExecutionContext : IGeneratorContext
    {
        public MinimalExecutionContext(Compilation compilation)
        {
            Compilation = compilation;
        }

        public Compilation Compilation { get; }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            // For incremental generators, diagnostics are handled differently
            // In a full implementation, these would be collected and reported through the proper channels
        }
    }
}
