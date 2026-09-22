namespace IdleAi;

public enum PipelineStage
{
	Compute,
	Data,
	Model,
	Output
}

public sealed class PipelineData
{
	public int ComputeLevel { get; set; } = 1;
	public int DataLevel { get; set; } = 1;
	public int ModelLevel { get; set; } = 1;
	public int OutputLevel { get; set; } = 1;

	public int GetLevel(PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute => ComputeLevel,
			PipelineStage.Data => DataLevel,
			PipelineStage.Model => ModelLevel,
			PipelineStage.Output => OutputLevel,
			_ => 1
		};
	}

	public void SetLevel(PipelineStage stage, int level)
	{
		int safeLevel = System.Math.Max(1, level);

		switch (stage)
		{
			case PipelineStage.Compute:
				ComputeLevel = safeLevel;
				break;
			case PipelineStage.Data:
				DataLevel = safeLevel;
				break;
			case PipelineStage.Model:
				ModelLevel = safeLevel;
				break;
			case PipelineStage.Output:
				OutputLevel = safeLevel;
				break;
		}
	}

	public void Reset()
	{
		ComputeLevel = 1;
		DataLevel = 1;
		ModelLevel = 1;
		OutputLevel = 1;
	}

	public PipelineSaveData ToSaveData()
	{
		return new PipelineSaveData
		{
			ComputeLevel = ComputeLevel,
			DataLevel = DataLevel,
			ModelLevel = ModelLevel,
			OutputLevel = OutputLevel
		};
	}

	public void LoadFromSaveData(PipelineSaveData data)
	{
		ComputeLevel = System.Math.Max(1, data.ComputeLevel);
		DataLevel = System.Math.Max(1, data.DataLevel);
		ModelLevel = System.Math.Max(1, data.ModelLevel);
		OutputLevel = System.Math.Max(1, data.OutputLevel);
	}
}
