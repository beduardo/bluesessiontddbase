using System.Text.Json;

namespace LibraryAPI.Services;

public interface IBestSellersService
{
    Task<int> GetBookRankAsync(string isbn);
}


public class BestSellersService(HttpClient httpClient) : IBestSellersService
{
    public async Task<int> GetBookRankAsync(string isbn)
    {
        var response = await httpClient.GetAsync($"http://localhost:3000/bestsellers/{isbn}");
        
        if (!response.IsSuccessStatusCode)
            return 99;
        
        var content = await response.Content.ReadAsStringAsync();
        var bookData = JsonSerializer.Deserialize<BookData>(content);

        return bookData?.Rank ?? throw new InvalidOperationException("Rank not found in response.");
    }

    private class BookData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int Rank { get; set; }
    }
}
    