using System.Text.Json.Serialization;

namespace IdleAi;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(SaveGameData))]
[JsonSerializable(typeof(RoomSaveData))]
[JsonSerializable(typeof(PipelineSaveData))]
[JsonSerializable(typeof(InfrastructureSaveData))]
[JsonSerializable(typeof(QuantumSaveData))]
[JsonSerializable(typeof(SlotSaveData))]
[JsonSerializable(typeof(StatsSaveData))]
internal partial class SaveJsonContext
	: JsonSerializerContext
{
}
