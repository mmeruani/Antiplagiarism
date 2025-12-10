using System.Text;
using System.Text.RegularExpressions;

namespace FileAnalysis.Services;

public class QuickChartWordCloudService : IWordCloudService
{
    private readonly ITextFetcher _textFetcher;

    public QuickChartWordCloudService(ITextFetcher textFetcher)
    {
        _textFetcher = textFetcher;
    }

    public async Task<string> BuildUrlAsync(string fileId)
    {
        var text = await _textFetcher.GetTextAsync(fileId);
        
        var normalized = Regex.Replace(text.ToLowerInvariant(), @"\p{P}+", " ");
        var words = Regex.Replace(normalized, @"\s+", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var freq = words
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(80)
            .ToDictionary(g => g.Key, g => g.Count());

        var expanded = string.Join(" ", freq.SelectMany(kv => Enumerable.Repeat(kv.Key, kv.Value)));
        var url = "https://quickchart.io/wordcloud?text=" + Uri.EscapeDataString(expanded);

        return url;
    }
}