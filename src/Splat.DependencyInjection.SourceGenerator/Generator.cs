using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using ReactiveMarbles.RoslynHelpers;

namespace Splat.DependencyInjection.SourceGenerator
{
    /// <summary>
    /// The main generator instance.
    /// </summary>
    [Generator]
    public class Generator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text.
            context.AddSource("Splat.DI.Extensions.SourceGenerated.cs", SourceText.From(Constants.ExtensionMethodText, Encoding.UTF8));

            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
            {
                return;
            }

            var compilation = context.Compilation;

            var options = (compilation as CSharpCompilation)?.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
            compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(Constants.ExtensionMethodText, Encoding.UTF8), options ?? new CSharpParseOptions()));

            var outputText = SourceGeneratorHelpers.Generate(context, compilation, syntaxReceiver);

            context.AddSource("Splat.DI.Extensions.Registrations.SourceGenerated.cs", SourceText.From(outputText, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }
}
