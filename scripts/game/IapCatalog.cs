using System;
using System.Collections.Generic;

namespace IdleAi;


public sealed record IapProductDefinition(
	string ProductId,
	int ShardAmount,
	string DisplayName,
	string Description,
	string ImagePath
);


public static class IapCatalog
{
	private static readonly IReadOnlyList<IapProductDefinition>
		Products =
		[
			new(
				"data_shard_100",
				100,
				"STARTER PACK",
				"100 Data Shards.",
				"res://assets/shop/hundred_shards.png"
			),

			new(
				"data_shard_400",
				400,
				"SMALL STACK",
				"400 Data Shards.",
				"res://assets/shop/shards_400.png"
			),

			new(
				"data_shard_900",
				900,
				"LARGE STACK",
				"900 Data Shards.",
				"res://assets/shop/shards_900.png"
			),

			new(
				"data_shard_2000",
				2000,
				"DATA VAULT",
				"2,000 Data Shards.",
				"res://assets/shop/shards_2000.png"
			),

			new(
				"data_shard_5500",
				5500,
				"QUANTUM VAULT",
				"5,500 Data Shards.",
				"res://assets/shop/shards_5500.png"
			)
		];


	public static IReadOnlyList<IapProductDefinition> GetAll()
	{
		return Products;
	}


	public static string[] GetProductIds()
	{
		string[] result =
			new string[
				Products.Count
			];


		for (
			int i = 0;
			i < Products.Count;
			i++
		)
		{
			result[
				i
			] =
				Products[
					i
				]
				.ProductId;
		}


		return result;
	}


	public static bool TryGet(
		string? productId,
		out IapProductDefinition definition)
	{
		if (
			string.IsNullOrWhiteSpace(
				productId
			)
		)
		{
			definition =
				null!;

			return false;
		}


		foreach (
			IapProductDefinition product
				in Products
		)
		{
			if (
				product.ProductId.Equals(
					productId,
					StringComparison.Ordinal
				)
			)
			{
				definition =
					product;

				return true;
			}
		}


		definition =
			null!;

		return false;
	}
}
