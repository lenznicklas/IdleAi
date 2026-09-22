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
	public int ComputeLevel { get; set; } =
		1;


	public int DataLevel { get; set; } =
		1;


	public int ModelLevel { get; set; } =
		1;


	public int OutputLevel { get; set; } =
		1;


	/*
	 * Material flow:
	 *
	 * Machines -> RawInputBuffer
	 * Compute  -> ComputeBuffer
	 * Data     -> DataBuffer
	 * Model    -> ModelBuffer
	 * Output   -> Tokens
	 */
	public double RawInputBuffer { get; set; }


	public double ComputeBuffer { get; set; }


	public double DataBuffer { get; set; }


	public double ModelBuffer { get; set; }


	/*
	 * Each pipeline stage runs in its own cycle.
	 *
	 * 0 means:
	 * - stage is currently idle
	 * - it will start a new cycle as soon as material is waiting
	 */
	public double ComputeCycleRemaining { get; set; }


	public double DataCycleRemaining { get; set; }


	public double ModelCycleRemaining { get; set; }


	public double OutputCycleRemaining { get; set; }


	/*
	 * Runtime-only values for the UI.
	 */
	public double LastMachineInputPerSecond { get; set; }


	public double LastTokenOutputPerSecond { get; set; }


	public int GetLevel(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				ComputeLevel,

			PipelineStage.Data =>
				DataLevel,

			PipelineStage.Model =>
				ModelLevel,

			PipelineStage.Output =>
				OutputLevel,

			_ =>
				1
		};
	}


	public void SetLevel(
		PipelineStage stage,
		int level)
	{
		int safeLevel =
			System.Math.Max(
				1,
				level
			);


		switch (stage)
		{
			case PipelineStage.Compute:
				ComputeLevel =
					safeLevel;
				break;


			case PipelineStage.Data:
				DataLevel =
					safeLevel;
				break;


			case PipelineStage.Model:
				ModelLevel =
					safeLevel;
				break;


			case PipelineStage.Output:
				OutputLevel =
					safeLevel;
				break;
		}
	}


	public double GetBufferBeforeStage(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				RawInputBuffer,

			PipelineStage.Data =>
				ComputeBuffer,

			PipelineStage.Model =>
				DataBuffer,

			PipelineStage.Output =>
				ModelBuffer,

			_ =>
				0.0
		};
	}


	public double GetCycleRemaining(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				ComputeCycleRemaining,

			PipelineStage.Data =>
				DataCycleRemaining,

			PipelineStage.Model =>
				ModelCycleRemaining,

			PipelineStage.Output =>
				OutputCycleRemaining,

			_ =>
				0.0
		};
	}


	public void SetCycleRemaining(
		PipelineStage stage,
		double value)
	{
		double safeValue =
			System.Math.Max(
				0.0,
				value
			);


		switch (stage)
		{
			case PipelineStage.Compute:
				ComputeCycleRemaining =
					safeValue;
				break;


			case PipelineStage.Data:
				DataCycleRemaining =
					safeValue;
				break;


			case PipelineStage.Model:
				ModelCycleRemaining =
					safeValue;
				break;


			case PipelineStage.Output:
				OutputCycleRemaining =
					safeValue;
				break;
		}
	}


	public void AddRawInput(
		double amount)
	{
		if (amount <= 0.0)
			return;


		RawInputBuffer +=
			amount;
	}


	public void Reset()
	{
		ComputeLevel =
			1;


		DataLevel =
			1;


		ModelLevel =
			1;


		OutputLevel =
			1;


		RawInputBuffer =
			0.0;


		ComputeBuffer =
			0.0;


		DataBuffer =
			0.0;


		ModelBuffer =
			0.0;


		ComputeCycleRemaining =
			0.0;


		DataCycleRemaining =
			0.0;


		ModelCycleRemaining =
			0.0;


		OutputCycleRemaining =
			0.0;


		LastMachineInputPerSecond =
			0.0;


		LastTokenOutputPerSecond =
			0.0;
	}


	public PipelineSaveData ToSaveData()
	{
		return new PipelineSaveData
		{
			ComputeLevel =
				ComputeLevel,

			DataLevel =
				DataLevel,

			ModelLevel =
				ModelLevel,

			OutputLevel =
				OutputLevel,

			RawInputBuffer =
				RawInputBuffer,

			ComputeBuffer =
				ComputeBuffer,

			DataBuffer =
				DataBuffer,

			ModelBuffer =
				ModelBuffer,

			ComputeCycleRemaining =
				ComputeCycleRemaining,

			DataCycleRemaining =
				DataCycleRemaining,

			ModelCycleRemaining =
				ModelCycleRemaining,

			OutputCycleRemaining =
				OutputCycleRemaining
		};
	}


	public void LoadFromSaveData(
		PipelineSaveData data)
	{
		ComputeLevel =
			System.Math.Max(
				1,
				data.ComputeLevel
			);


		DataLevel =
			System.Math.Max(
				1,
				data.DataLevel
			);


		ModelLevel =
			System.Math.Max(
				1,
				data.ModelLevel
			);


		OutputLevel =
			System.Math.Max(
				1,
				data.OutputLevel
			);


		RawInputBuffer =
			System.Math.Max(
				0.0,
				data.RawInputBuffer
			);


		ComputeBuffer =
			System.Math.Max(
				0.0,
				data.ComputeBuffer
			);


		DataBuffer =
			System.Math.Max(
				0.0,
				data.DataBuffer
			);


		ModelBuffer =
			System.Math.Max(
				0.0,
				data.ModelBuffer
			);


		ComputeCycleRemaining =
			System.Math.Max(
				0.0,
				data.ComputeCycleRemaining
			);


		DataCycleRemaining =
			System.Math.Max(
				0.0,
				data.DataCycleRemaining
			);


		ModelCycleRemaining =
			System.Math.Max(
				0.0,
				data.ModelCycleRemaining
			);


		OutputCycleRemaining =
			System.Math.Max(
				0.0,
				data.OutputCycleRemaining
			);


		LastMachineInputPerSecond =
			0.0;


		LastTokenOutputPerSecond =
			0.0;
	}
}
