// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using TUnit.Assertions;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for the DiagnosticWarnings class.
/// </summary>
public class DiagnosticWarningsTests
{
    /// <summary>
    /// Verifies that all diagnostic IDs are correctly defined.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task VerifyDiagnosticIds()
    {
        // Access all fields to ensure static initializer is run and fields are covered
        await Assert.That(DiagnosticWarnings.MultipleConstructorNeedAttribute.Id).IsEqualTo("SPLATDI001");
        await Assert.That(DiagnosticWarnings.PropertyMustPublicBeSettable.Id).IsEqualTo("SPLATDI002");
        await Assert.That(DiagnosticWarnings.MultipleConstructorsMarked.Id).IsEqualTo("SPLATDI003");
        await Assert.That(DiagnosticWarnings.ConstructorsMustBePublic.Id).IsEqualTo("SPLATDI004");
        await Assert.That(DiagnosticWarnings.ConstructorsMustNotHaveCircularDependency.Id).IsEqualTo("SPLATDI005");
        await Assert.That(DiagnosticWarnings.InterfaceRegisteredMultipleTimes.Id).IsEqualTo("SPLATDI006");
        await Assert.That(DiagnosticWarnings.LazyParameterNotRegisteredLazy.Id).IsEqualTo("SPLATDI007");

        // Verify categories and severity
        await Assert.That(DiagnosticWarnings.MultipleConstructorNeedAttribute.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(DiagnosticWarnings.InterfaceRegisteredMultipleTimes.DefaultSeverity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(DiagnosticWarnings.MultipleConstructorNeedAttribute.Category).IsEqualTo("Compiler");
    }
}
