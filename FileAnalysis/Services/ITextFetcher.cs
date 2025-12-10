using FileAnalysis.Domain;

namespace FileAnalysis.Services;

public interface ITextFetcher
{
    Task<string> GetTextAsync(string fileId);
}