using System.Text;

namespace LibraryManagement.Api.Common;

public static class BookCoverGenerator
{
    private static readonly Dictionary<string, (string From, string To)> CategoryColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Classic"] = ("#8B4513", "#654321"),
        ["Fiction"] = ("#2C3E50", "#3498DB"),
        ["Fantasy"] = ("#6C3483", "#7D3C98"),
        ["Science Fiction"] = ("#1A5276", "#2E86C1"),
        ["Sci-Fi"] = ("#1A5276", "#2E86C1"),
        ["Thriller"] = ("#78281F", "#E74C3C"),
        ["Mystery"] = ("#4A235A", "#8E44AD"),
        ["Historical Fiction"] = ("#7D6608", "#D4AC0D"),
        ["Philosophy"] = ("#1A4331", "#229954"),
        ["Biography"] = ("#4A235A", "#6C3483"),
        ["Science"] = ("#0E6251", "#1ABC9C"),
        ["Psychology"] = ("#4E342E", "#8D6E63"),
        ["Dystopian"] = ("#1B1B2F", "#2C3E50"),
        ["Business"] = ("#1B4F72", "#2E86C1"),
        ["History"] = ("#3E2723", "#795548"),
        ["Self-Help"] = ("#1B5E20", "#4CAF50"),
        ["Memoir"] = ("#4A235A", "#8E44AD"),
        ["Technology"] = ("#0D47A1", "#1976D2"),
    };

    private static readonly string DefaultFrom = "#2C3E50";
    private static readonly string DefaultTo = "#34495E";

    public static string GenerateFallbackSvg(string title, string author, string category)
    {
        var (from, to) = GetCategoryColors(category);
        var initials = GetInitials(title);
        var titleLines = WrapText(title, 20);
        var escapedAuthor = EscapeXml(author);
        var escapedCategory = EscapeXml(category);

        var sb = new StringBuilder();
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"600\" viewBox=\"0 0 400 600\">");
        sb.AppendLine("<defs>");

        // Main gradient
        sb.AppendLine($"<linearGradient id=\"bg\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"100%\">");
        sb.AppendLine($"  <stop offset=\"0%\" stop-color=\"{from}\"/>");
        sb.AppendLine($"  <stop offset=\"100%\" stop-color=\"{to}\"/>");
        sb.AppendLine("</linearGradient>");

        // Radial highlight
        sb.AppendLine("<radialGradient id=\"highlight\" cx=\"50%\" cy=\"30%\" r=\"60%\">");
        sb.AppendLine("  <stop offset=\"0%\" stop-color=\"white\" stop-opacity=\"0.08\"/>");
        sb.AppendLine("  <stop offset=\"100%\" stop-color=\"white\" stop-opacity=\"0\"/>");
        sb.AppendLine("</radialGradient>");

        sb.AppendLine("</defs>");

        // Background with rounded corners
        sb.AppendLine("<rect width=\"400\" height=\"600\" rx=\"12\" ry=\"12\" fill=\"url(#bg)\"/>");

        // Highlight overlay
        sb.AppendLine("<rect width=\"400\" height=\"600\" rx=\"12\" ry=\"12\" fill=\"url(#highlight)\"/>");

        // Subtle dot pattern
        sb.AppendLine("<g opacity=\"0.06\">");
        for (int y = 20; y < 600; y += 30)
        {
            for (int x = 20; x < 400; x += 30)
            {
                sb.AppendLine($"<circle cx=\"{x}\" cy=\"{y}\" r=\"1.5\" fill=\"white\"/>");
            }
        }
        sb.AppendLine("</g>");

        // Border
        sb.AppendLine("<rect x=\"8\" y=\"8\" width=\"384\" height=\"584\" rx=\"8\" ry=\"8\" fill=\"none\" stroke=\"white\" stroke-opacity=\"0.15\" stroke-width=\"1\"/>");

        // Inner decorative border
        sb.AppendLine("<rect x=\"16\" y=\"16\" width=\"368\" height=\"568\" rx=\"6\" ry=\"6\" fill=\"none\" stroke=\"white\" stroke-opacity=\"0.06\" stroke-width=\"0.5\" stroke-dasharray=\"4,4\"/>");

        // Top decorative line
        sb.AppendLine("<line x1=\"60\" y1=\"40\" x2=\"340\" y2=\"40\" stroke=\"white\" stroke-opacity=\"0.12\" stroke-width=\"0.5\"/>");

        // Initials - large centered text
        sb.AppendLine($"<text x=\"200\" y=\"250\" text-anchor=\"middle\" font-family=\"Georgia, serif\" font-size=\"120\" font-weight=\"bold\" fill=\"white\" fill-opacity=\"0.2\">{EscapeXml(initials)}</text>");

        // Divider line
        sb.AppendLine("<line x1=\"140\" y1=\"310\" x2=\"260\" y2=\"310\" stroke=\"white\" stroke-opacity=\"0.3\" stroke-width=\"1\"/>");
        // Small diamond on divider
        sb.AppendLine("<polygon points=\"200,305 205,310 200,315 195,310\" fill=\"white\" fill-opacity=\"0.3\"/>");

        // Title - multi-line
        double titleY = 340;
        double lineHeight = 26;
        foreach (var line in titleLines)
        {
            var escapedLine = EscapeXml(line);
            sb.AppendLine($"<text x=\"200\" y=\"{titleY}\" text-anchor=\"middle\" font-family=\"Georgia, serif\" font-size=\"18\" font-weight=\"bold\" fill=\"white\">{escapedLine}</text>");
            titleY += lineHeight;
        }

        // Author
        sb.AppendLine($"<text x=\"200\" y=\"{titleY + 10}\" text-anchor=\"middle\" font-family=\"Helvetica, Arial, sans-serif\" font-size=\"14\" fill=\"white\" fill-opacity=\"0.7\">by {escapedAuthor}</text>");

        // Category pill at bottom
        var categoryWidth = escapedCategory.Length * 9 + 30;
        var pillX = 200 - categoryWidth / 2;
        sb.AppendLine($"<rect x=\"{pillX}\" y=\"510\" width=\"{categoryWidth}\" height=\"28\" rx=\"14\" ry=\"14\" fill=\"white\" fill-opacity=\"0.15\"/>");
        sb.AppendLine($"<text x=\"200\" y=\"529\" text-anchor=\"middle\" font-family=\"Helvetica, Arial, sans-serif\" font-size=\"12\" font-weight=\"500\" letter-spacing=\"1\" fill=\"white\" fill-opacity=\"0.85\">{escapedCategory.ToUpper()}</text>");

        // Footer text
        sb.AppendLine("<text x=\"200\" y=\"570\" text-anchor=\"middle\" font-family=\"Helvetica, Arial, sans-serif\" font-size=\"9\" fill=\"white\" fill-opacity=\"0.2\" letter-spacing=\"2\">✦ FALLBACK COVER ✦</text>");

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    public static string GenerateDataUri(string title, string author, string category)
    {
        var svg = GenerateFallbackSvg(title, author, category);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        return $"data:image/svg+xml;base64,{encoded}";
    }

    private static (string From, string To) GetCategoryColors(string? category)
    {
        if (string.IsNullOrEmpty(category))
            return (DefaultFrom, DefaultTo);

        if (CategoryColors.TryGetValue(category, out var colors))
            return colors;

        return (DefaultFrom, DefaultTo);
    }

    private static string GetInitials(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "?";

        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var word in words.Take(3))
        {
            sb.Append(char.ToUpper(word[0]));
        }
        return sb.ToString();
    }

    private static List<string> WrapText(string text, int maxCharsPerLine)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            lines.Add("Untitled");
            return lines;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxCharsPerLine && currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString().Trim());
                currentLine.Clear();
            }
            if (currentLine.Length > 0)
                currentLine.Append(' ');
            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString().Trim());

        if (lines.Count == 0)
            lines.Add("Untitled");

        return lines.Take(3).ToList();
    }

    public static string EscapeXml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
