using System.Text;

namespace Api.Features.Comparisons.Queries.GetAllComparisons;

public static class StringBuilderExtensions
{
    public static StringBuilder CreateLatexDocument() => new StringBuilder().WithTag("documentclass");

    public static StringBuilder WithDocumentAnnotations(this StringBuilder builder, IEnumerable<string> annotations)
    {
        var annotationsString = string.Join(", ", annotations);
        return builder.WithSquareBrace(annotationsString);
    }

    public static StringBuilder WithLineBreak(this StringBuilder builder) => builder.Append(Environment.NewLine);

    public static StringBuilder WithPackage(this StringBuilder builder, string package, IEnumerable<string>? annotations = null)
    {
        var tag = builder.WithTag("usepackage");
        if (annotations?.Any() ?? false)
        {
            tag = tag.WithSquareBrace(string.Join(", ", annotations!));
        }
        return tag.WithCurlyBrace(package)
            .WithLineBreak();
    }

    public static StringBuilder WithTitle(this StringBuilder builder, IEnumerable<string> titles)
        => builder
            .WithTag("title")
            .WithCurlyBrace(string.Join(@"\\", titles))
            .WithLineBreak();

    public static StringBuilder WithAuthor(this StringBuilder builder, IEnumerable<string> authors)
        => builder
            .WithTag("author")
            .WithCurlyBrace(string.Join(@"\\", authors))
            .WithLineBreak();

    public static StringBuilder WithDate(this StringBuilder builder)
        => builder
            .WithTag("date")
            .WithCurlyBrace(DateTimeOffset.Now.ToString())
            .WithLineBreak();

    public static StringBuilder WithGroup(this StringBuilder builder, string groupType, StringBuilder innerContent)
        => builder
            .WithTag("begin")
            .WithCurlyBrace(groupType)
            .WithLineBreak()
            .Append(innerContent)
            .WithLineBreak()
            .WithTag("end")
            .WithCurlyBrace(groupType)
            .WithLineBreak();

    public static StringBuilder WithRenderedTitle(this StringBuilder builder)
        => builder
            .WithTag("maketitle")
            .WithLineBreak();

    public static StringBuilder WithSection(this StringBuilder builder, string section, bool hideNumber = false)
        => builder.Append($@"\section{(hideNumber ? "*" : "")}{{\centering {section}}}").WithLineBreak();

    public static StringBuilder WithSubsection(this StringBuilder builder, string section, bool hideNumber = false)
        => builder.Append($@"\subsection{(hideNumber ? "*" : "")}{{\centering {section}}}").WithLineBreak();

    public static StringBuilder WithSubsubsection(this StringBuilder builder, string section, bool hideNumber = false)
        => builder.Append($@"\subsubsection{(hideNumber ? "*" : "")}{{\centering {section}}}").WithLineBreak();

    public static StringBuilder WithTag(this StringBuilder builder, string tag)
        => builder.Append($@"\{tag}");

    public static StringBuilder ToTag(this string tag)
        => new StringBuilder().WithTag(tag);

    public static StringBuilder ToSpacedTag(this string tag)
        => new StringBuilder().WithSpacedTag(tag);

    public static StringBuilder WithSpacedTag(this StringBuilder builder, string tag)
        => builder.Append($@"\{tag} ");

    public static StringBuilder WithSquareBrace(this StringBuilder builder, string content)
        => builder.Append($"[{content}]");

    public static StringBuilder WithCurlyBrace(this StringBuilder builder, string content)
        => builder.Append($"{{{content}}}");

    public static StringBuilder WithTitleFormat(this StringBuilder builder,
        string type,
        string displayType,
        string? firstDescriptor,
        string? secondDescriptor,
        string? thirdDescriptor,
        string? fourthDescriptor,
        string? fifthDescriptor)
        => builder
            .WithTag("titleformat")
            .WithCurlyBrace(type)
            .WithSquareBrace(displayType)
            .WithCurlyBrace(firstDescriptor ?? "")
            .WithCurlyBrace(secondDescriptor ?? "")
            .WithCurlyBrace(thirdDescriptor ?? "")
            .WithCurlyBrace(fourthDescriptor ?? "")
            .WithSquareBrace(fifthDescriptor ?? "")
            .WithLineBreak();

    public static StringBuilder WithTitleSpacing(this StringBuilder builder,
        string partDescription,
        string firstSpacing,
        string secondSpacing,
        string thirdSpacing)
        => builder
            .WithTag("titlespacing")
            .WithCurlyBrace(partDescription)
            .WithCurlyBrace(firstSpacing)
            .WithCurlyBrace(secondSpacing)
            .WithCurlyBrace(thirdSpacing)
            .WithLineBreak();
}
