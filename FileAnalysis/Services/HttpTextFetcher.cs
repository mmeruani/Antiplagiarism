namespace FileAnalysis.Services;

public class HttpTextFetcher : ITextFetcher
{
    private readonly HttpClient _http;

    public HttpTextFetcher(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetTextAsync(string fileId)
    {
        var response = await _http.GetAsync($"/files/{fileId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}