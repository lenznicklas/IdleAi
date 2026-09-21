using System;

namespace IdleAi;

public static class NumberFormatter
{
	private static readonly string[] StandardSuffixes =
	[
		"",
		"K",
		"M",
		"B",
		"T"
	];


	// ==================================================
	// FORMAT
	// ==================================================

	public static string Format(
		double value)
	{
		if (double.IsNaN(value))
		{
			return "NaN";
		}


		if (double.IsPositiveInfinity(value))
		{
			return "∞";
		}


		if (double.IsNegativeInfinity(value))
		{
			return "-∞";
		}


		double absoluteValue =
			Math.Abs(
				value
			);


		// ==================================================
		// SMALL NUMBER
		// ==================================================

		if (absoluteValue < 1000.0)
		{
			return FormatSmallNumber(
				value
			);
		}


		// ==================================================
		// DETERMINE 1000 GROUP
		// ==================================================

		int group =
			(int)Math.Floor(
				Math.Log10(
					absoluteValue
				)
				/ 3.0
			);


		/*
		 * Protect against weird floating-point cases.
		 */
		group =
			Math.Max(
				0,
				group
			);


		double divisor =
			Math.Pow(
				1000.0,
				group
			);


		double scaledValue =
			value
			/ divisor;


		// ==================================================
		// NORMAL SUFFIXES
		// ==================================================

		if (
			group
			< StandardSuffixes.Length
		)
		{
			return FormatScaledNumber(
				scaledValue
			)
			+ StandardSuffixes[
				group
			];
		}


		// ==================================================
		// IDLE-MINER SUFFIXES
		// ==================================================

		/*
		 * group:
		 *
		 * 0 = no suffix
		 * 1 = K
		 * 2 = M
		 * 3 = B
		 * 4 = T
		 *
		 * therefore:
		 *
		 * 5 = aa
		 * 6 = ab
		 * 7 = ac
		 * ...
		 */
		int letterIndex =
			group
			- StandardSuffixes.Length;


		string suffix =
			CreateLetterSuffix(
				letterIndex
			);


		return FormatScaledNumber(
			scaledValue
		)
		+ suffix;
	}


	// ==================================================
	// LETTER SUFFIX
	// ==================================================

	private static string CreateLetterSuffix(
		int index)
	{
		/*
		 * Sequence:
		 *
		 * 0  -> aa
		 * 1  -> ab
		 * 2  -> ac
		 * ...
		 * 25 -> az
		 *
		 * 26 -> ba
		 * 27 -> bb
		 * ...
		 *
		 * 675 -> zz
		 */


		if (index < 0)
		{
			return "";
		}


		int first =
			index
			/ 26;


		int second =
			index
			% 26;


		/*
		 * double can reach beyond "zz".
		 *
		 * If that ever happens, extend the naming
		 * automatically to three or more letters.
		 */
		if (first >= 26)
		{
			return CreateExtendedLetterSuffix(
				index
			);
		}


		char firstLetter =
			(char)(
				'a'
				+ first
			);


		char secondLetter =
			(char)(
				'a'
				+ second
			);


		return
			firstLetter.ToString()
			+ secondLetter;
	}


	// ==================================================
	// EXTENDED SUFFIX
	// ==================================================

	private static string CreateExtendedLetterSuffix(
		int index)
	{
		/*
		 * Normally the game will never need this
		 * because double reaches its limit long before
		 * thousands of suffix groups.
		 *
		 * Still, this keeps the formatter generic.
		 *
		 * Examples after zz:
		 *
		 * aaa
		 * aab
		 * aac
		 * ...
		 */


		int value =
			index;


		string result =
			"";


		do
		{
			int digit =
				value
				% 26;


			char letter =
				(char)(
					'a'
					+ digit
				);


			result =
				letter
				+ result;


			value =
				value
				/ 26
				- 1;
		}
		while (value >= 0);


		/*
		 * Two-letter suffixes are the minimum.
		 */
		if (result.Length == 1)
		{
			result =
				"a"
				+ result;
		}


		return result;
	}


	// ==================================================
	// SMALL NUMBERS
	// ==================================================

	private static string FormatSmallNumber(
		double value)
	{
		double absoluteValue =
			Math.Abs(
				value
			);


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


	// ==================================================
	// SCALED NUMBERS
	// ==================================================

	private static string FormatScaledNumber(
		double value)
	{
		double absoluteValue =
			Math.Abs(
				value
			);


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
