# PolyMessage

RPC communication library based on .NET Standard. Allows creation of microservices using a client-server architecture. Supports:
* Request-response pattern
* Different transports based on communication protocols
* Different message formats using data encoding standards

## Example ([**code below**](#code-example))

It could be used to create microservices using the request-response pattern with TCP as a transport and Google Protobuf as message format.

## Core features

* Shared contracts defining the microservices
  * Operations with a type based on the communication pattern - e.g. request-response
  * Main messages - e.g. request and response
  * Optional additional DTOs as part of more complex message hierarchies
  * Similar to the good old WCF
* Easy and quick declarative definition of contracts via .NET attributes
* Consistent API allowing easy switching between:
  * Message formats
  * Underlying transports
* Widely used message formats are supported out of the box (listed below)
* TCP transport is supported out of the box with IPC one in progress (more details below)
* Extension points:
  * Message formats
  * Underlying transports
* Built using .NET Standard 2.0
* Integrated with .NET logging on client and server sides
* Integrated with .NET dependency injection on server-side
* Dynamic client-side proxy generation
* Each server can host all or a subset of the contracts
* Same client can serve more than one proxy using the same connection
* Read-only access to the connection on client and server sides

## Message formats

Formats rely on widely used .NET libraries. All of them use declarative attributes. They have their own ones and they support .NET based attributes such as `[DataContract]`, `[DataMember]` etc.

* [protobuf-net](https://github.com/protobuf-net/protobuf-net)
* [Newtonsoft.Json](https://www.newtonsoft.com/json)
* [Utf8Json](https://github.com/neuecc/Utf8Json)
* [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)

## Transports

### TCP

* Supports the request-response pattern
* Uses a custom protocol based on a single TCP connection and length-prefixing
* Server is implemented via the async-await paradigm allowing thread pool threads reuse
* Server can be configured to disconnect:
  * clients being idle more than a specified timeout
  * clients which receive data more slowly than a specified timeout
* Secure communication via TLS

### IPC (in progress)

* Supports the request-response pattern
* Uses a custom protocol based on a local named-pipe for control and a memory-mapped file for performant data transfer
* Secure communication via named-pipe's and memory-mapped file's built-in security

## Verification

### Automated testing

* Integration tests using (single or multiple) client(s) and server instances:
  * Contract - validation
  * Connection - addresses, state, read-only access on client and server sides
  * Request-response pattern - single/multiple endpoints, single/multiple messages, performance
  * Message format - messages which contain: nothing (empty), large arrays, large number of objects, large strings
  * TCP transport - TLS security, timeouts
  * Service implementation instance disposal
* Micro tests:
  * IL generation

### Load testing

The code under `\load-testing` folder can be used to perform load testing in a specific environment.

TODO: post the result using the environment in place

# Code example

## Step 1: Define contracts, messages and optionally DTOs

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
		IServiceProvider serviceProvider = new ServiceCollection()
			.AddLogging(loggingBuilder =>
			{
				loggingBuilder.SetMinimumLevel(LogLevel.Debug);
				loggingBuilder.AddConsole();
			})
			.AddScoped<IProductServiceContract, ProductService>()
			.BuildServiceProvider();
		ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

		PolyFormat format = new ProtobufNetFormat();
		PolyTransport transport = new TcpTransport(new Uri("tcp://127.0.0.1:10678/"), loggerFactory);
		using PolyHost host = new PolyHost(transport, format, serviceProvider);
		host.AddContract<IProductServiceContract>();

		await host.StartAsync();
	}
}
```

## Step 3: Create client

```C#
public static class Client
{
	public static async Task Main()
	{
		IServiceProvider serviceProvider = new ServiceCollection()
			.AddLogging(loggingBuilder =>
			{
				loggingBuilder.SetMinimumLevel(LogLevel.Debug);
				loggingBuilder.AddConsole();
			})
			.BuildServiceProvider();
		ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

		PolyFormat format = new ProtobufNetFormat();
		PolyTransport transport = new TcpTransport(new Uri("tcp://127.0.0.1:10678/"), loggerFactory);
		using PolyClient client = new PolyClient(transport, format, loggerFactory);
		client.AddContract<IProductServiceContract>();

		await client.ConnectAsync();
		IProductServiceContract proxy = client.Get<IProductServiceContract>();

		var request = new GetCheapestProductsRequest {TopCount = 10, Barcode = "milk"};
		GetCheapestProductsResponse response = await proxy.GetCheapestProducts(request);
	}
}
```
