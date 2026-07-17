using System.Text.Json.Serialization;
using Trackify.Domain.Trains;

namespace Trackify.Infrastructure.Persistence;

/// <summary>Source-generated (trim/AOT-safe) JSON contract for the train store; enums are written as names.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<TrainConfig>))]
internal sealed partial class TrainStoreJsonContext : JsonSerializerContext;
