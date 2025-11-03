using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.Cryptography;
using SpawnDev.WebFS.Host;

var builder = WebApplication.CreateBuilder(args);
var appName = AppDomain.CurrentDomain.FriendlyName.Split(".").Last();
var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), appName);
Console.WriteLine($"AppDataPath: {appDataPath}");
if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
builder.Services.AddBlazorJSRuntime();
builder.Services.AddSingleton<DotNetCrypto>();
builder.Services.AddSingleton<DokanService>();
builder.Services.AddSingleton(sp => (IPortableCrypto)sp.GetRequiredService<DotNetCrypto>());
var app = builder.Build();
await app.Services.StartBackgroundServices();
app.Run();




static void Deser()
{
    //var data = new byte[0];
    //{
    //    var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
    //    var myObject = new MyData { Id = 1, Name = "Test" };
    //    var bytes = MessagePackSerializer.Serialize(myObject, options);
    //    var deserializedObject = MessagePackSerializer.Deserialize<MyData>(bytes, options);

    //}

    //{
    //    var options = MessagePackSerializerOptions.Standard
    //   .WithResolver(CompositeResolver.Create(
    //       StandardResolver.Instance,
    //       TypelessContractlessStandardResolver.Instance // For dynamic types if needed
    //   ));

    //    byte[] serializedData = MessagePackSerializer.Serialize(data, options);

    //    // --- Deserializing one element at a time ---
    //    var reader = new MessagePackReader(serializedData);

    //    if (reader.IsArray)
    //    {
    //        int arrayLength = reader.ReadArrayHeader();
    //        Console.WriteLine($"Array contains {arrayLength} elements.");

    //        for (int i = 0; i < arrayLength; i++)
    //        {
    //            // In a real-world scenario, you would have a way to determine the type
    //            // For demonstration, we'll assume a pattern or a type discriminator exists
    //            if (i % 2 == 0) // Assume even indices are MyData
    //            {
    //                var myData = MessagePackSerializer.Deserialize<MyData>(ref reader, options);
    //                Console.WriteLine($"Deserialized MyData: Id={myData.Id}, Name={myData.Name}");
    //            }
    //            else // Assume odd indices are AnotherData
    //            {
    //                var anotherData = MessagePackSerializer.Deserialize<AnotherData>(ref reader, options);
    //                Console.WriteLine($"Deserialized AnotherData: IsActive={anotherData.IsActive}, Value={anotherData.Value}");
    //            }
    //        }
    //    }
    //}
}
public class MyData
{
    public int Id { get; set; }
    public string Name { get; set; }
}
public class AnotherData
{
    public int IsActive { get; set; }
    public string Value { get; set; }
}