# PolyMessage

Experimental RPC library for sending messages supporting different  transports and formats.

# How to use it

### Create contracts, messages and optionally DTOs

```C#
public interface IProductServiceContract : IPolyContract
{
    [PolyRequestResponse]
    Task<GetCheapestProductsResponse> GetCheapestProducts(GetCheapestProductsRequest request);
}

[PolyMessage]
[DataContract]
public sealed class GetCheapestProductsRequest
{
    [DataContract(Order = 1)] public int TopCount { get; set; }
    [DataContract(Order = 2)] public string Barcode { get; set; }
}

[PolyMessage]
[DataContract]
public sealed class GetCheapestProductsResponse
{
    [DataContract(Order = 1)] public List<ProductDto> Products { get; set; } = new List<ProductDto>();
}

[DataContract]
public sealed class ProductDto
{
    [DataContract(Order = 1)] public string Name { get; set; }
    [DataContract(Order = 2)] public decimal Price { get; set; }
    [DataContract(Order = 3)] public string Currency { get; set; }
}

```

### Create contract implementation and server

```C#
public class ProductService : IProductServiceContract
{
    public PolyConnection Connection { get; set; }

    public Task<GetCheapestProductsResponse> GetCheapestProducts(GetCheapestProductsRequest request)
    {
        // implement the endpoint
    }
}

public static class Server
{
    public static async Task Main()
    {
        IServiceProvider serviceProvider = BuildServiceProvider();
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        PolyFormat format = new Utf8JsonFormat();
        PolyTransport transport = new TcpTransport(new Uri("tcp://localhost:10678/"), loggerFactory);
        using PolyHost host = new PolyHost(transport, format, serviceProvider);
        host.AddContract<IProductServiceContract>();

        await host.StartAsync();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        IServiceCollection services = new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                loggingBuilder.AddDebug();
                loggingBuilder.AddConsole();
            });
        services.AddScoped<IProductServiceContract, ProductService>();

        return services.BuildServiceProvider();
    }
}
```

### Create client

```C#
public static class Client
{
    private static async Task Start(ClientOptions options)
    {
        IServiceProvider serviceProvider = BuildServiceProvider();
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        PolyFormat format = new Utf8JsonFormat();
        PolyTransport transport = new TcpTransport(new Uri("tcp://localhost:10678/"), loggerFactory);
        using PolyClient client = new PolyClient(transport, format, loggerFactory);
        client.AddContract<IProductServiceContract>();

        await client.ConnectAsync();
        IProductServiceContract proxy = client.Get<IProductServiceContract>();
        
        GetCheapestProductsResponse response = await proxy.GetCheapestProducts(new GetCheapestProductsRequest
        {
            TopCount = 10, Barcode = "..."
        });
    }

    private static IServiceProvider BuildServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            loggingBuilder.AddDebug();
            loggingBuilder.AddConsole();
        });

        return services.BuildServiceProvider();
    }
}
```
