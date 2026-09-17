using System;

namespace IdleAi;

public static class NumberFormatter
{
	private static readonly string[] Suffixes =
	[
		"",
		"K",
		"M",
		"B",
		"T",
		"Qa",
		"Qi",
		"Sx",
		"Sp",
		"Oc",
		"No",
        "Dc"
	];


	public static string Format(
		double value)
	{
		if (double.IsNaN(value))
			return "NaN";


		if (double.IsPositiveInfinity(value))
			return "∞";


		if (double.IsNegativeInfinity(value))
			return "-∞";


		double absoluteValue =
			Math.Abs(value);


		if (absoluteValue < 1000.0)
		{
			return FormatSmallNumber(
				value
			);
		}


		int suffixIndex =
			0;


		double scaledValue =
			value;


		while (
			Math.Abs(scaledValue) >= 1000.0
			&& suffixIndex < Suffixes.Length - 1
		)
		{
			scaledValue /=
				1000.0;


			suffixIndex++;
		}


		// Falls wir über Decillion hinauskommen,
		// verwenden wir wissenschaftliche Schreibweise.
		if (
			suffixIndex == Suffixes.Length - 1
			&& Math.Abs(scaledValue) >= 1000.0
		)
		{
			return value.ToString(
                "0.00E+0"
			);
		}


		return FormatScaledNumber(
			scaledValue
		)
		+ Suffixes[
			suffixIndex
		];
	}


	private static string FormatSmallNumber(
		double value)
	{
		double absoluteValue =
			Math.Abs(value);


		if (absoluteValue >= 100.0)
		{
			return value.ToString(
                "F0"
			);
		}


		if (absoluteValue >= 10.0)
		{
			return value.ToString(
                "F1"
			);
		}


		return value.ToString(
            "F2"
		);
	}


	private static string FormatScaledNumber(
		double value)
	{
		double absoluteValue =
			Math.Abs(value);


		if (absoluteValue >= 100.0)
		{
			return value.ToString(
                "F0"
			);
		}


		if (absoluteValue >= 10.0)
		{
			return value.ToString(
                "F1"
			);
		}


		return value.ToString(
            "F2"
		);
	}
}
