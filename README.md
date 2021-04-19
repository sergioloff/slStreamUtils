# slStreamUtils
![build](https://github.com/sergioloff/slStreamUtils/actions/workflows/BuildAndTestOnPush.yml/badge.svg) 


slStreamUtils is a set of tools that improve the (de)serialization performance over an I/O **stream**.
Amongst other improvements over the standard FileStream class, it also offers multithreaded stream (de)serialization support for both [protobuf-net](https://github.com/protobuf-net/protobuf-net) and [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp). It introduces framing over the stream without breaking compatibility with the underlying protocol (allowing to exchange files between different languages and OS), but requires some adaptation of your code. Please look for more details in the examples section.
Please read the [post](https://slstreamutils.blogspot.com) for further details.

## Supported Runtimes
- .NET Standard 2.0, 2.1

## NuGet packages
- The base package, [slStreamUtils](https://www.nuget.org/packages/slStreamUtils) [![NuGet slStreamUtils](https://img.shields.io/nuget/v/slStreamUtils.svg)](https://www.nuget.org/packages/slStreamUtils), contains the Stream extensions
- The package [slStreamUtilsProtobuf](https://www.nuget.org/packages/slStreamUtilsProtobuf) [![NuGet slStreamUtilsProtobuf](https://img.shields.io/nuget/v/slStreamUtilsProtobuf.svg)](https://www.nuget.org/packages/slStreamUtilsProtobuf), extending Protobuf-net for framed multi-threaded stream serialization
- The package [slStreamUtilsMessagePack](https://www.nuget.org/packages/slStreamUtilsMessagePack) [![NuGet slStreamUtilsMessagePack](https://img.shields.io/nuget/v/slStreamUtilsMessagePack.svg)](https://www.nuget.org/packages/slStreamUtilsMessagePack), extending MessagePack-CSharp for framed multi-threaded stream serialization



The following charts show the MB/s performance of both reading and writing a collection of unknown length from/to a file over a stream. Note: the improvements shown for the MessagePack protocol are in great part the result of introducing message framing, which removes the overhead of scanning the stream looking for the end of the current block. Also, the write benchmark is faster since it doesn’t include the object creation overhead, so it shouldn’t be compared with the read results (like oranges to apples).
![performance charts](https://raw.githubusercontent.com/sergioloff/slStreamUtils/master/readme_chart.png)

# Examples:
Framed, streaming serialization of a collection of objects using protobuf-net vs the new multithreaded implementation:

```csharp
using slProto = slStreamUtils.MultiThreadedSerialization.Protobuf;
// ...
private static IEnumerable<X> GetSampleArray()
{
    return Enumerable.Range(0, 10).Select(f => new X() { b1 = f % 2 == 0, i1 = f, l1 = f % 3 });
}
// ...
IEnumerable<X> arr = GetSampleArray();
using var s = File.Create(fileName);
//...

// original implementation
foreach (var obj in arr)
    ProtoBuf.Serializer.SerializeWithLengthPrefix(s, obj, ProtoBuf.PrefixStyle.Base128, 1);

// framed multithreaded implementation
await using var ser = new CollectionSerializerAsync<X>(s, new FIFOWorkerConfig(maxConcurrentTasks: 2));
foreach (var item in arr)
    await ser.SerializeAsync(item);

```

Framed, streaming deserialization of a collection of objects using protobuf-net vs the new multithreaded implementation:

```csharp
using slProto = slStreamUtils.MultiThreadedSerialization.Protobuf;
//...
using var s = File.OpenRead(fileName);

// original implementation
while (true)
{
    X obj = ProtoBuf.Serializer.DeserializeWithLengthPrefix<X>(s, ProtoBuf.PrefixStyle.Base128, 1);
    if (obj is null)
        break;
    yield return obj;
}

// framed multithreaded implementation
using var ds = new CollectionDeserializerAsync<X>(new FIFOWorkerConfig(maxConcurrentTasks: 2));
await foreach (var item in ds.DeserializeAsync(s))
    yield return item.Item;
```

MessagePack formatter and resolver for automatic framing header generation and multithreaded (de)serialization.

Example serializable class with automatic framing
```csharp
[MessagePackObject]
public class ArrayX 
{
    [Key(0)]
    public Frame<X>[] arr; // where X in any serializable type
    // ... more fields
}
```

Serialization using the new Frame formatter:
```csharp
ArrayX obj = new ArrayX(...)
int totWorkerThreads = 4;
var opts = new FrameParallelOptions(totWorkerThreads, MessagePackSerializerOptions.Standard.WithResolver(FrameResolverPlusStandarResolver.Instance));
using (var s1 = File.Create(fileName))
    await MessagePackSerializer.SerializeAsync(s1, obj, opts); // this will include framing header and process ArrayX.arr in parallel while saving
using (var s2 = File.OpenRead(fileName))
    newObj = await MessagePackSerializer.DeserializeAsync<ArrayX>(s2, opts); // this will process ArrayX.arr in parallel while loading since it has framing data
```

# Extra references
Besides the examples in the Examples and Benchmark folder, I recomend to [read my blog](https://slstreamutils.blogspot.com/) which contains more in-depth information about the techniques used, particularly for the stream framing.

