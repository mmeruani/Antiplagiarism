namespace FileAnalysis.Services;

public interface IWordCloudService
{
    Task<string> BuildUrlAsync(string fileId);
}