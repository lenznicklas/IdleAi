namespace IdleAi;

public static class NumberFormatter
{
	public static string Format(
		double value)
	{
		if (
			value
			>= 1_000_000_000_000.0
		)
		{
			return $"{value / 1_000_000_000_000.0:F2}T";
		}


		if (
			value
			>= 1_000_000_000.0
		)
		{
			return $"{value / 1_000_000_000.0:F2}B";
		}


		if (
			value
			>= 1_000_000.0
		)
		{
			return $"{value / 1_000_000.0:F2}M";
		}


		if (
			value
			>= 1_000.0
		)
		{
			return $"{value / 1_000.0:F2}K";
		}


		if (value >= 100.0)
		{
			return $"{value:F0}";
		}


		if (value >= 10.0)
		{
			return $"{value:F1}";
		}


		return $"{value:F2}";
	}
}
