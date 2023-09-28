using StockAnalyzer.Core.Services;

namespace TestProject1;

public class UnitTest1
{
    [Fact]
    public async Task Can_Load_All_MSFT_Stocks()
    {
        var service = new MockStockService();
        var stocks = await service.GetStockPricesFor(
            "MSFT", CancellationToken.None);
        
        Assert.Equal(2, stocks.Count());
    }
}