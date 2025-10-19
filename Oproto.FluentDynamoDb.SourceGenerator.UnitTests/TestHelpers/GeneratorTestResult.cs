using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Oproto.FluentDynamoDb.SourceGenerator.UnitTests.Integration;

public class GeneratorTestResult
{
    public ImmutableArray<Diagnostic> Diagnostics { get; set; }
    public GeneratedSource[] GeneratedSources { get; set; } = Array.Empty<GeneratedSource>();
}

public class GeneratedSource
{
    public string FileName { get; }
    public SourceText SourceText { get; }

    public GeneratedSource(string fileName, SourceText sourceText)
    {
        FileName = fileName;
        SourceText = sourceText;
    }
}