# slStreamUtils
![build](https://github.com/sergioloff/slStreamUtils/actions/workflows/BuildAndTestOnPush.yml/badge.svg) 


slStreamUtils is a set of tools that try to improve the (de)serialization performance over an I/O **stream**.
Amongst other improvements over the standard FileStream class, it also offers multithreaded stream (de)serialization support for both [protobuf-net](https://github.com/protobuf-net/protobuf-net) and [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp). 
For MessagePack, it introduces framing over the stream without breaking compatibility with the underlying protocol (allowing to exchange files between different languages and OS), but might require some adaptation of your class definitions. 
For Protobuf, which already has native message framing support, no modifications are needed.
Please read the [post](https://slstreamutils.blogspot.com) for the underlying implementation details.

## Supported Runtimes
- .NET Standard 2.0, 2.1 and newer

## NuGet packages
- The base package, [slStreamUtils](https://www.nuget.org/packages/slStreamUtils) [![NuGet slStreamUtils](https://img.shields.io/nuget/v/slStreamUtils.svg)](https://www.nuget.org/packages/slStreamUtils), contains the Stream extensions
- The package [slStreamUtilsProtobuf](https://www.nuget.org/packages/slStreamUtilsProtobuf) [![NuGet slStreamUtilsProtobuf](https://img.shields.io/nuget/v/slStreamUtilsProtobuf.svg)](https://www.nuget.org/packages/slStreamUtilsProtobuf), extending Protobuf-net for framed multi-threaded stream serialization
- The package [slStreamUtilsMessagePack](https://www.nuget.org/packages/slStreamUtilsMessagePack) [![NuGet slStreamUtilsMessagePack](https://img.shields.io/nuget/v/slStreamUtilsMessagePack.svg)](https://www.nuget.org/packages/slStreamUtilsMessagePack), extending MessagePack-CSharp for framed multi-threaded stream serialization

# Benchmarks and usage samples

Bencharks are available through the projects under [Benchmarks/slStreamUtilsBenchmark](https://github.com/sergioloff/slStreamUtils/tree/master/Benchmarks)

Two data sets are used: 

* Large Objects – a collection of 4096 objects, each taking ~16KB, for a combined size of ~67MB
* Small Objects – a collection of 123K objects, each taking 550B, for a combined size of aprox. 66MB 

## Protobuf - collection of undetermined length 

This benchmark measures the read and write speed of a collection of objects of the same type T and of unknown length (as for example iterating an IEnumerable<T>) to/from a MemoryStream.

![Protobuf - performance chart for collection of undetermined length](https://raw.githubusercontent.com/sergioloff/slStreamUtils/master/PB_coll.png)
	
It compares the native implementations using Protobuf-net
	
```csharp
foreach (var obj in arr)
	ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, obj, ProtoBuf.PrefixStyle.Base128, 1);

X obj;
while ((obj = ProtoBuf.Serializer.DeserializeWithLengthPrefix<X>(stream, ProtoBuf.PrefixStyle.Base128, 1)) != null)
	yield return obj;
```

vs parallel implementation in slStreamUtils using an increasingly larger # of threads
    
```csharp
await using var ser = new CollectionSerializerAsync<X>(stream, new FIFOWorkerConfig(maxConcurrentTasks: 2));
foreach (var item in arr)
	await ser.SerializeAsync(item);

using var ds = new CollectionDeserializerAsync<X>(new FIFOWorkerConfig(maxConcurrentTasks: 2));
await foreach (var item in ds.DeserializeAsync(stream))
	yield return item.Item;
```

## MessagePack - collection of undetermined length 

This benchmark measures the read and write speed of a collection of objects of the same type T and of unknown length (as for example iterating an IEnumerable<T>) to/from a MemoryStream.

![MessagePack - performance chart for collection of undetermined length](https://raw.githubusercontent.com/sergioloff/slStreamUtils/master/MP_coll.png)
	
It compares the native implementations using MessagePack-CSharp
	
```csharp
using var stream = File.Create(fileName);
foreach (var obj in arr)
	await MessagePackSerializer.SerializeAsync<Frame<X>>(stream, obj);

using var sr = new MessagePackStreamReader(stream);
while (await sr.ReadAsync(CancellationToken.None) is ReadOnlySequence<byte> msgpack)
	yield return MessagePackSerializer.Deserialize<Frame<X>>(msgpack);
```

vs parallel implementation in slStreamUtils using an increasingly larger # of threads
    
```csharp
await using var ser = new CollectionSerializerAsync<X>(stream, maxConcurrentTasks: 2);
foreach (var item in arr)
	await ser.SerializeAsync(item);

using var ds = new CollectionDeserializerAsync<X>(maxConcurrentTasks: 2);
await foreach (var item in ds.DeserializeAsync(stream))
	yield return item;
```

Note: It's natural to see MessagePack's original implementation being significantly slower because we’re using MessagePack.MessagePackStreamReader to read from a stream of undetermined length. This method will scan ahead and parse the stream until it finds the end of the current element, returning that segment of bytes, and only then will we deserialize it using MessagePackSerializer.Deserialize, ultimately resulting in having to do the same work twice, which is inevitable since we don't have any framing information telling us where the current message block ends. If we were deserializing buffers of a known size, we’d only have to call MessagePackSerializer.Deserialize, which would be much faster, as can be seen in the next benchmark.

## MessagePack - fixed-size arrays
	
This benchmark measures the read and write speed of an object containing fields of type Frame<T>[] to/from a MemoryStream.

![performance chart for MessagePack with fixed-size arrays](https://raw.githubusercontent.com/sergioloff/slStreamUtils/master/MP_par.png)

Original type:

## MessagePack - fixed-size arrays
	
This benchmark measures the read and write speed of an object containing fields of type Frame<T>[] to/from a MemoryStream.

![performance chart for MessagePack with fixed-size arrays](https://raw.githubusercontent.com/sergioloff/slStreamUtils/master/MP_par.png)

Original type:

```csharp
[MessagePackObject]
public class SomeClass
{
	// some fields
	[Key(0)]
	public AnotherClass[] arr;
	// some more fields
}
```

New, frame-enabled type:
	
```csharp
[MessagePackObject]
public class SomeClass
{
	// some fields
	[Key(0)]
	public Frame<AnotherClass>[] arr; // this will be (de)serialized in parallel since it's wrapped in *Frame<>*
	// some more fields
}
```

Notice how we've wrapped *AnotherClass* with the struct Frame, which is little else besides an int field and a ref to AnotherClass:

```csharp
[MessagePackObject]
public struct Frame<T>
{
	[Key(0)]
	public int BufferLength { get; internal set; }
	[Key(1)]
	public T Item { get; set; }
}
```

To (de)serialize an instance of SomeClass using MessagePack-CSharp's original implementation, one would write

```csharp
await MessagePackSerializer.SerializeAsync(stream, obj, opts);

var newObj = await MessagePackSerializer.DeserializeAsync<ArrayX>(stream, opts);
```

And to make the above code run in parallel, just add the new Frame resolver to your options

```csharp
var opts = new FrameParallelOptions(totWorkerThreads, 
	MessagePackSerializerOptions.Standard.WithResolver(FrameResolverPlusStandarResolver.Instance));
```

To (de)serialize an instance of SomeClass using MessagePack-CSharp's original implementation, one would write

```csharp
await MessagePackSerializer.SerializeAsync(stream, obj, opts);

var newObj = await MessagePackSerializer.DeserializeAsync<ArrayX>(stream, opts);
    newObj = await MessagePackSerializer.DeserializeAsync<ArrayX>(s2, opts); // this will process ArrayX.arr in parallel while loading since it has framing data
```
And to make the above code run in parallel, just add the new Frame resolver to your options
Besides the examples in the Examples and Benchmark folder, I recomend to [read my blog](https://slstreamutils.blogspot.com/) which contains more in-depth information about the techniques used, particularly for the stream framing.
```csharp
var opts = new FrameParallelOptions(totWorkerThreads, 
	MessagePackSerializerOptions.Standard.WithResolver(FrameResolverPlusStandarResolver.Instance));
```

