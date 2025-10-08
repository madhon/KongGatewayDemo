namespace Marvel;

using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(defaults: JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(string))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;