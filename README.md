# PolyMessage

Experimental RPC library supporting different client-server transports and message formats. Supports .NET Standard 2.0 and provides .NET Core integration. Similar to WCF in terms of usage.

## Core features

* Definition of contracts and messages is declarative via .NET attributes
* Read-only access to the connection on client and server sides
* Client-side proxy is generated using Castle.Core dynamic proxy
* Each server can host a subset of the contracts
* Each client can support more than one proxy
* Integrated with .NET Core logging on client and server sides
* Integrated with .NET dependency injection on server-side
* Consistent API allowing switching between different message formats and underlying transports, as long as the new transport supports the communication pattern - e.g. request-response

## Formats

Formats rely on widely used .NET libraries.

* [protobuf-net](https://github.com/protobuf-net/protobuf-net)
* [Newtonsoft.Json](https://www.newtonsoft.com/json)
* [Utf8Json](https://github.com/neuecc/Utf8Json)
* [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)

## Transports

### TCP

* Supports the request-response communication pattern
* Uses a custom protocol based on a single TCP connection and length-prefixing
* Server is implemented via the async-await paradigm allowing thread pool threads reuse
* Supports TLS

### IPC (in progress)

* Supports the request-response communication pattern
* Uses a custom protocol based on a local **named-pipe** for control and a **memory-mapped file** for data transfer
* Secure communication via named-pipe and memory-mapped file

# Example

## Step 1: Create contracts, messages and optionally DTOs

```C#
public interface IProductServiceContract : IPolyContract
{
	[PolyRequestResponse]
	Task<GetCheapestProductsResponse> GetCheapestProducts(GetCheapestProductsRequest request);
}

[PolyMessage][DataContract]
public sealed class GetCheapestProductsRequest
{
	[DataMember(Order = 1)] public int TopCount { get; set; }
	[DataMember(Order = 2)] public string Barcode { get; set; }
}

[PolyMessage][DataContract]
public sealed class GetCheapestProductsResponse
{
	[DataMember(Order = 1)] public List<ProductDto> Products { get; set; } = new List<ProductDto>();
}

[DataContract]
public sealed class ProductDto
{
	[DataMember(Order = 1)] public string Name { get; set; }
	[DataMember(Order = 2)] public decimal Price { get; set; }
	[DataMember(Order = 3)] public string Currency { get; set; }
}
```

## Step 2: Create service implementation and server

```C#
public class ProductService : IProductServiceContract
{
	public PolyConnection Connection { get; set; }

	public async Task<GetCheapestProductsResponse> GetCheapestProducts(GetCheapestProductsRequest request)
	{
		var response = new GetCheapestProductsResponse();
		response.Products.Add(new ProductDto {Name = "milk", Price = 3.50M, Currency = "EUR"});
		return response;
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
		return new ServiceCollection()
			.AddLogging(loggingBuilder =>
			{
				loggingBuilder.SetMinimumLevel(LogLevel.Debug);
				loggingBuilder.AddConsole();
			})
			.AddScoped<IProductServiceContract, ProductService>()
			.BuildServiceProvider();
	}
}
```

## Create client

```C#
public static class Client
{
	public static async Task Main()
	{
		IServiceProvider serviceProvider = BuildServiceProvider();
		ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

		PolyFormat format = new Utf8JsonFormat();
		PolyTransport transport = new TcpTransport(new Uri("tcp://localhost:10678/"), loggerFactory);
		using PolyClient client = new PolyClient(transport, format, loggerFactory);
		client.AddContract<IProductServiceContract>();

		await client.ConnectAsync();
		IProductServiceContract proxy = client.Get<IProductServiceContract>();

		GetCheapestProductsResponse response = await proxy.GetCheapestProducts(
			new GetCheapestProductsRequest {TopCount = 10, Barcode = "milk"});
	}

	private static IServiceProvider BuildServiceProvider()
	{
		return new ServiceCollection()
			.AddLogging(loggingBuilder =>
			{
				loggingBuilder.SetMinimumLevel(LogLevel.Debug);
				loggingBuilder.AddConsole();
			})
			.BuildServiceProvider();
	}
}
```
