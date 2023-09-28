using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using StockAnalyzer.Core.Domain;

namespace StockAnalyzer.CrossPlatform.Services;

public interface IStockStreamService
{
    IAsyncEnumerable<StockPrice> GetAllStockPrices(CancellationToken cancellationToken = default);
}

public class MockStockStreamService : IStockStreamService
{
    public async IAsyncEnumerable<StockPrice> 
        GetAllStockPrices([EnumeratorCancellation]
            CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "MSFT", Change = 0.5m };
        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "MSFT", Change = 0.5m };
        await Task.Delay(500, cancellationToken);
        
        yield return new StockPrice { Identifier = "GOOG", Change = 0.2m };
        await Task.Delay(500, cancellationToken);
        
        yield return new StockPrice { Identifier = "GOOG", Change = 0.3m };
        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "MSFT", Change = 0.5m };
        await Task.Delay(500, cancellationToken);

    }
}

public class StockDiskStreamService : IStockStreamService
{
    public async IAsyncEnumerable<StockPrice> 
        GetAllStockPrices([EnumeratorCancellation]
                            CancellationToken cancellationToken = default)
    {
        using var stream = new StreamReader(File.OpenRead("StockPrices_SMall.csv"));

        await stream.ReadLineAsync();

        while (await stream.ReadLineAsync() is string line)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            yield return StockPrice.FromCSV(line);
        }
    }
}