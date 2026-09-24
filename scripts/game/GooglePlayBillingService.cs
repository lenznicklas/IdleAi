using Godot;

using GodotArray =
	Godot.Collections.Array;

using GodotDictionary =
	Godot.Collections.Dictionary;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace IdleAi;


public sealed class GooglePlayBillingService
{
	private const string PluginName =
		"GodotGooglePlayBilling";

	private const string ProductTypeInApp =
		"inapp";


	/*
	 * You described the Play Console product as
	 * "data_shard_100 with id 10001".
	 *
	 * Google Play purchases are started with the Product ID.
	 * To make this work with either interpretation, both values
	 * are queried. Whichever one Google actually returns is used.
	 */
	public const string PreferredProductId =
		"data_shard_100";

	public const string AlternateProductId =
		"10001";


	private const string LedgerPath =
		"user://idle_ai_iap_ledger.json";

	private const string TemporaryLedgerPath =
		"user://idle_ai_iap_ledger.tmp.json";


	private const int BillingResponseOk =
		0;

	private const int BillingResponseUserCanceled =
		1;

	private const int PurchaseStatePurchased =
		1;

	private const int PurchaseStatePending =
		2;


	private GodotObject? _billing;


	private readonly HashSet<string>
		_processedPurchaseTokens =
			new(
				StringComparer.Ordinal
			);


	private readonly HashSet<string>
		_processingPurchaseTokens =
			new(
				StringComparer.Ordinal
			);


	private readonly HashSet<string>
		_consumingPurchaseTokens =
			new(
				StringComparer.Ordinal
			);


	private bool _initialized;

	private bool _connected;

	private bool _queryingProducts;

	private bool _purchaseFlowActive;


	private string _activeProductId =
		"";

	private string _purchaseOptionId =
		"";

	private string _formattedPrice =
		"";


	public event Action? Changed;

	public event Action<string>? MessageRequested;

	public event Action<
		string,
		string,
		int
	>? ConsumablePurchaseReady;


	public bool IsAvailable =>
		OS.GetName() == "Android"
		&& _billing != null;


	public bool IsConnected =>
		_connected;


	public bool HasProduct =>
		!string.IsNullOrWhiteSpace(
			_activeProductId
		);


	public bool PurchaseFlowActive =>
		_purchaseFlowActive;


	public bool CanPurchase =>
		IsAvailable
		&& _connected
		&& HasProduct
		&& !_purchaseFlowActive
		&& _consumingPurchaseTokens.Count == 0;


	public string ActiveProductId =>
		_activeProductId;


	public string FormattedPrice =>
		_formattedPrice;


	public string StatusText
	{
		get
		{
			if (OS.GetName() != "Android")
			{
				return
					"GOOGLE PLAY BILLING IS AVAILABLE IN THE ANDROID BUILD";
			}


			if (_billing == null)
			{
				return
					"GOOGLE PLAY BILLING PLUGIN NOT FOUND";
			}


			if (!_connected)
			{
				return
					"CONNECTING TO GOOGLE PLAY...";
			}


			if (_purchaseFlowActive)
			{
				return
					"PURCHASE IN PROGRESS...";
			}


			if (_consumingPurchaseTokens.Count > 0)
			{
				return
					"FINALIZING PURCHASE...";
			}


			if (_queryingProducts)
			{
				return
					"LOADING GOOGLE PLAY PRICE...";
			}


			if (!HasProduct)
			{
				return
					"PRODUCT NOT AVAILABLE FROM GOOGLE PLAY";
			}


			if (
				!string.IsNullOrWhiteSpace(
					_formattedPrice
				)
			)
			{
				return
					"100 DATA SHARDS  •  "
					+ _formattedPrice;
			}


			return
				"100 DATA SHARDS  •  GOOGLE PLAY READY";
		}
	}


	public string ButtonText
	{
		get
		{
			if (!IsAvailable)
			{
				return
					"ANDROID / GOOGLE PLAY ONLY";
			}


			if (!_connected)
			{
				return
					"CONNECTING...";
			}


			if (_purchaseFlowActive)
			{
				return
					"PURCHASE IN PROGRESS...";
			}


			if (_consumingPurchaseTokens.Count > 0)
			{
				return
					"FINALIZING...";
			}


			if (_queryingProducts)
			{
				return
					"LOADING PRICE...";
			}


			if (!HasProduct)
			{
				return
					"PRODUCT UNAVAILABLE";
			}


			if (
				!string.IsNullOrWhiteSpace(
					_formattedPrice
				)
			)
			{
				return
					"BUY  •  "
					+ _formattedPrice;
			}


			return
				"BUY  •  100 SHARDS";
		}
	}


	public GooglePlayBillingService()
	{
		LoadLedger();
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		if (_initialized)
			return;


		_initialized =
			true;


		/*
		 * The native Android singleton does not exist in the
		 * desktop editor. Keeping this Android-only means the
		 * Shop remains usable while testing the game on PC.
		 */
		if (OS.GetName() != "Android")
		{
			Changed?.Invoke();

			return;
		}


		if (!Engine.HasSingleton(PluginName))
		{
			GD.PushWarning(
				PluginName
				+ " singleton not found. "
				+ "Make sure the billing plugin is enabled."
			);


			Changed?.Invoke();

			return;
		}


		_billing =
			Engine.GetSingleton(
				PluginName
			);


		if (_billing == null)
		{
			Changed?.Invoke();

			return;
		}


		ConnectSignals();


		/*
		 * BillingClient.gd normally calls initPlugin() in its
		 * constructor. This C# integration talks directly to the
		 * exact same native singleton, so we initialize it here.
		 */
		_billing.Call(
			"initPlugin"
		);


		_billing.Call(
			"startConnection"
		);


		Changed?.Invoke();
	}


	private void ConnectSignals()
	{
		if (_billing == null)
			return;


		_billing.Connect(
			"connected",
			Callable.From(
				OnConnected
			)
		);


		_billing.Connect(
			"disconnected",
			Callable.From(
				OnDisconnected
			)
		);


		_billing.Connect(
			"connect_error",
			Callable.From<long, string>(
				OnConnectError
			)
		);


		_billing.Connect(
			"query_product_details_response",
			Callable.From<GodotDictionary>(
				OnQueryProductDetailsResponse
			)
		);


		_billing.Connect(
			"query_purchases_response",
			Callable.From<GodotDictionary>(
				OnQueryPurchasesResponse
			)
		);


		_billing.Connect(
			"on_purchase_updated",
			Callable.From<GodotDictionary>(
				OnPurchaseUpdated
			)
		);


		_billing.Connect(
			"consume_purchase_response",
			Callable.From<GodotDictionary>(
				OnConsumePurchaseResponse
			)
		);
	}


	private void OnConnected()
	{
		_connected =
			true;


		_purchaseFlowActive =
			false;


		QueryProducts();

		QueryOwnedPurchases();


		Changed?.Invoke();
	}


	private void OnDisconnected()
	{
		_connected =
			false;


		_purchaseFlowActive =
			false;


		_queryingProducts =
			false;


		Changed?.Invoke();
	}


	private void OnConnectError(
		long responseCode,
		string debugMessage)
	{
		_connected =
			false;


		_purchaseFlowActive =
			false;


		_queryingProducts =
			false;


		GD.PushWarning(
			"Google Play Billing connection error "
				+ responseCode
				+ ": "
				+ debugMessage
		);


		MessageRequested?.Invoke(
			"Google Play Billing connection failed."
		);


		Changed?.Invoke();
	}


	// ==================================================
	// PRODUCT DETAILS
	// ==================================================

	private void QueryProducts()
	{
		if (
			_billing == null
			|| !_connected
		)
		{
			return;
		}


		_queryingProducts =
			true;


		_activeProductId =
			"";


		_purchaseOptionId =
			"";


		_formattedPrice =
			"";


		_billing.Call(
			"queryProductDetails",
			new string[]
			{
				PreferredProductId,
				AlternateProductId
			},
			ProductTypeInApp
		);


		Changed?.Invoke();
	}


	private void OnQueryProductDetailsResponse(
		GodotDictionary response)
	{
		_queryingProducts =
			false;


		int responseCode =
			GetInt(
				response,
				"response_code",
				-1
			);


		if (responseCode != BillingResponseOk)
		{
			GD.PushWarning(
				"Google Play product query failed: "
					+ GetString(
						response,
						"debug_message"
					)
			);


			Changed?.Invoke();

			return;
		}


		if (
			!response.ContainsKey(
				"product_details"
			)
		)
		{
			Changed?.Invoke();

			return;
		}


		Variant detailsValue =
			response[
				"product_details"
			];


		if (
			detailsValue.VariantType
				!= Variant.Type.Array
		)
		{
			Changed?.Invoke();

			return;
		}


		GodotArray details =
			detailsValue.AsGodotArray();


		GodotDictionary? preferred =
			null;


		GodotDictionary? alternate =
			null;


		foreach (
			Variant value
				in details
		)
		{
			if (
				value.VariantType
					!= Variant.Type.Dictionary
			)
			{
				continue;
			}


			GodotDictionary product =
				value.AsGodotDictionary();


			string productId =
				GetString(
					product,
					"product_id"
				);


			if (
				productId.Equals(
					PreferredProductId,
					StringComparison.Ordinal
				)
			)
			{
				preferred =
					product;
			}
			else if (
				productId.Equals(
					AlternateProductId,
					StringComparison.Ordinal
				)
			)
			{
				alternate =
					product;
			}
		}


		GodotDictionary? selected =
			preferred
			?? alternate;


		if (selected == null)
		{
			_activeProductId =
				"";


			GD.PushWarning(
				"Google Play did not return product details for "
					+ PreferredProductId
					+ " or "
					+ AlternateProductId
					+ "."
			);


			Changed?.Invoke();

			return;
		}


		_activeProductId =
			GetString(
				selected,
				"product_id"
			);


		ReadOneTimeOffer(
			selected
		);


		GD.Print(
			"Google Play product ready: ",
			_activeProductId,
			" | price=",
			_formattedPrice,
			" | purchaseOptionId=",
			_purchaseOptionId
		);


		Changed?.Invoke();
	}


	private void ReadOneTimeOffer(
		GodotDictionary product)
	{
		_purchaseOptionId =
			"";


		_formattedPrice =
			"";


		if (
			!product.ContainsKey(
				"one_time_purchase_offer_details_list"
			)
		)
		{
			return;
		}


		Variant offersValue =
			product[
				"one_time_purchase_offer_details_list"
			];


		if (
			offersValue.VariantType
				!= Variant.Type.Array
		)
		{
			return;
		}


		GodotArray offers =
			offersValue.AsGodotArray();


		if (offers.Count == 0)
			return;


		GodotDictionary? firstOffer =
			null;


		GodotDictionary? standardOffer =
			null;


		foreach (
			Variant value
				in offers
		)
		{
			if (
				value.VariantType
					!= Variant.Type.Dictionary
			)
			{
				continue;
			}


			GodotDictionary offer =
				value.AsGodotDictionary();


			firstOffer ??=
				offer;


			string offerId =
				GetString(
					offer,
					"offer_id"
				);


			if (
				string.IsNullOrWhiteSpace(
					offerId
				)
			)
			{
				standardOffer =
					offer;

				break;
			}
		}


		GodotDictionary? selected =
			standardOffer
			?? firstOffer;


		if (selected == null)
			return;


		_formattedPrice =
			GetString(
				selected,
				"formatted_price"
			);


		_purchaseOptionId =
			GetString(
				selected,
				"purchase_option_id"
			);
	}


	// ==================================================
	// PURCHASE
	// ==================================================

	public void Purchase()
	{
		if (_billing == null)
		{
			MessageRequested?.Invoke(
				"Google Play Billing is not available."
			);

			return;
		}


		if (!_connected)
		{
			MessageRequested?.Invoke(
				"Google Play is still connecting."
			);

			return;
		}


		if (_consumingPurchaseTokens.Count > 0)
		{
			QueryOwnedPurchases();


			MessageRequested?.Invoke(
				"Finishing the previous purchase first."
			);

			return;
		}


		if (!HasProduct)
		{
			QueryProducts();


			MessageRequested?.Invoke(
				"Loading the Google Play product. Try again when the price appears."
			);

			return;
		}


		if (_purchaseFlowActive)
			return;


		Variant resultValue =
			_billing.Call(
				"purchase",
				_activeProductId,
				_purchaseOptionId,
				"",
				false
			);


		if (
			resultValue.VariantType
				!= Variant.Type.Dictionary
		)
		{
			MessageRequested?.Invoke(
				"Google Play could not start the purchase."
			);

			return;
		}


		GodotDictionary result =
			resultValue.AsGodotDictionary();


		int responseCode =
			GetInt(
				result,
				"response_code",
				-1
			);


		if (responseCode == BillingResponseOk)
		{
			_purchaseFlowActive =
				true;


			Changed?.Invoke();

			return;
		}


		string debugMessage =
			GetString(
				result,
				"debug_message"
			);


		GD.PushWarning(
			"Could not launch Google Play purchase: "
				+ responseCode
				+ " "
				+ debugMessage
		);


		MessageRequested?.Invoke(
			responseCode
				== BillingResponseUserCanceled
					? "Purchase cancelled."
					: "Google Play could not start the purchase."
		);


		Changed?.Invoke();
	}


	private void OnPurchaseUpdated(
		GodotDictionary response)
	{
		_purchaseFlowActive =
			false;


		int responseCode =
			GetInt(
				response,
				"response_code",
				-1
			);


		if (responseCode == BillingResponseUserCanceled)
		{
			MessageRequested?.Invoke(
				"Purchase cancelled."
			);


			Changed?.Invoke();

			return;
		}


		if (responseCode != BillingResponseOk)
		{
			GD.PushWarning(
				"Google Play purchase failed: "
					+ responseCode
					+ " "
					+ GetString(
						response,
						"debug_message"
					)
			);


			MessageRequested?.Invoke(
				"Purchase failed. No Data Shards were added."
			);


			Changed?.Invoke();

			return;
		}


		ProcessPurchaseResponse(
			response
		);


		Changed?.Invoke();
	}


	private void QueryOwnedPurchases()
	{
		if (
			_billing == null
			|| !_connected
		)
		{
			return;
		}


		_billing.Call(
			"queryPurchases",
			ProductTypeInApp,
			false
		);
	}


	private void OnQueryPurchasesResponse(
		GodotDictionary response)
	{
		int responseCode =
			GetInt(
				response,
				"response_code",
				-1
			);


		if (responseCode != BillingResponseOk)
		{
			GD.PushWarning(
				"Could not query existing Google Play purchases: "
					+ GetString(
						response,
						"debug_message"
					)
			);

			return;
		}


		ProcessPurchaseResponse(
			response
		);


		Changed?.Invoke();
	}


	private void ProcessPurchaseResponse(
		GodotDictionary response)
	{
		if (
			!response.ContainsKey(
				"purchases"
			)
		)
		{
			return;
		}


		Variant purchasesValue =
			response[
				"purchases"
			];


		if (
			purchasesValue.VariantType
				!= Variant.Type.Array
		)
		{
			return;
		}


		GodotArray purchases =
			purchasesValue.AsGodotArray();


		foreach (
			Variant value
				in purchases
		)
		{
			if (
				value.VariantType
					!= Variant.Type.Dictionary
			)
			{
				continue;
			}


			ProcessPurchase(
				value.AsGodotDictionary()
			);
		}
	}


	private void ProcessPurchase(
		GodotDictionary purchase)
	{
		if (
			!TryGetSupportedProductId(
				purchase,
				out string productId
			)
		)
		{
			return;
		}


		int purchaseState =
			GetInt(
				purchase,
				"purchase_state",
				0
			);


		string token =
			GetString(
				purchase,
				"purchase_token"
			);


		if (
			string.IsNullOrWhiteSpace(
				token
			)
		)
		{
			GD.PushWarning(
				"Google Play purchase did not include a purchase token."
			);

			return;
		}


		if (purchaseState == PurchaseStatePending)
		{
			MessageRequested?.Invoke(
				"Purchase is pending. Shards will be added after Google Play confirms payment."
			);

			return;
		}


		if (purchaseState != PurchaseStatePurchased)
			return;


		if (
			_processedPurchaseTokens.Contains(
				token
			)
		)
		{
			/*
			 * The Shards for this exact token have already been
			 * granted. If Google still reports it as owned, only
			 * retry consumption; never grant a second time.
			 */
			ConsumePurchase(
				token
			);

			return;
		}


		if (
			!_processingPurchaseTokens.Add(
				token
			)
		)
		{
			return;
		}


		int quantity =
			Math.Max(
				1,
				GetInt(
					purchase,
					"quantity",
					1
				)
			);


		ConsumablePurchaseReady?.Invoke(
			productId,
			token,
			quantity
		);
	}


	private bool TryGetSupportedProductId(
		GodotDictionary purchase,
		out string productId)
	{
		productId =
			"";


		if (
			!purchase.ContainsKey(
				"product_ids"
			)
		)
		{
			return false;
		}


		Variant idsValue =
			purchase[
				"product_ids"
			];


		string[] ids;


		try
		{
			ids =
				idsValue.AsStringArray();
		}
		catch
		{
			return false;
		}


		foreach (
			string id
				in ids
		)
		{
			if (
				id.Equals(
					PreferredProductId,
					StringComparison.Ordinal
				)
				|| id.Equals(
					AlternateProductId,
					StringComparison.Ordinal
				)
			)
			{
				productId =
					id;

				return true;
			}
		}


		return false;
	}


	// ==================================================
	// GRANT + CONSUME
	// ==================================================

	/*
	 * IapShopController calls this only after:
	 *
	 * 1. ShopService granted the Shards.
	 * 2. GameUiController.StateChanged triggered Game.SaveGame().
	 *
	 * The token is persisted in this separate ledger before the
	 * Google Play consumable is consumed.
	 */
	public void CompleteConsumableGrant(
		string purchaseToken)
	{
		if (
			string.IsNullOrWhiteSpace(
				purchaseToken
			)
		)
		{
			return;
		}


		_processedPurchaseTokens.Add(
			purchaseToken
		);


		_processingPurchaseTokens.Remove(
			purchaseToken
		);


		SaveLedger();


		ConsumePurchase(
			purchaseToken
		);


		Changed?.Invoke();
	}


	private void ConsumePurchase(
		string purchaseToken)
	{
		if (
			_billing == null
			|| !_connected
			|| string.IsNullOrWhiteSpace(
				purchaseToken
			)
		)
		{
			return;
		}


		if (
			!_consumingPurchaseTokens.Add(
				purchaseToken
			)
		)
		{
			return;
		}


		_billing.Call(
			"consumePurchase",
			purchaseToken
		);


		Changed?.Invoke();
	}


	private void OnConsumePurchaseResponse(
		GodotDictionary response)
	{
		string token =
			GetString(
				response,
				"token"
			);


		if (
			!string.IsNullOrWhiteSpace(
				token
			)
		)
		{
			_consumingPurchaseTokens.Remove(
				token
			);
		}


		int responseCode =
			GetInt(
				response,
				"response_code",
				-1
			);


		if (responseCode == BillingResponseOk)
		{
			GD.Print(
				"Google Play consumable finalized: ",
				token
			);
		}
		else
		{
			/*
			 * Reward is already saved and the token remains in
			 * the ledger. A later queryPurchases() can therefore
			 * retry consume without granting the Shards again.
			 */
			GD.PushWarning(
				"Google Play consume failed: "
					+ GetString(
						response,
						"debug_message"
					)
			);


			MessageRequested?.Invoke(
				"100 Shards were saved. Google Play will finalize the purchase automatically."
			);
		}


		Changed?.Invoke();
	}


	// ==================================================
	// TOKEN LEDGER
	// ==================================================

	private void LoadLedger()
	{
		_processedPurchaseTokens.Clear();


		string path =
			ProjectSettings.GlobalizePath(
				LedgerPath
			);


		if (!File.Exists(path))
			return;


		try
		{
			string json =
				File.ReadAllText(
					path
				);


			IapLedger? ledger =
				JsonSerializer.Deserialize<IapLedger>(
					json
				);


			if (ledger == null)
				return;


			foreach (
				string token
					in ledger.ProcessedPurchaseTokens
			)
			{
				if (
					!string.IsNullOrWhiteSpace(
						token
					)
				)
				{
					_processedPurchaseTokens.Add(
						token
					);
				}
			}
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Could not load IAP token ledger: "
					+ exception.Message
			);
		}
	}


	private void SaveLedger()
	{
		string path =
			ProjectSettings.GlobalizePath(
				LedgerPath
			);


		string temporaryPath =
			ProjectSettings.GlobalizePath(
				TemporaryLedgerPath
			);


		try
		{
			IapLedger ledger =
				new()
				{
					ProcessedPurchaseTokens =
						_processedPurchaseTokens
							.OrderBy(
								token => token
							)
							.ToList()
				};


			string json =
				JsonSerializer.Serialize(
					ledger,
					new JsonSerializerOptions
					{
						WriteIndented =
							true
					}
				);


			using (
				FileStream stream =
					new(
						temporaryPath,
						FileMode.Create,
						System.IO.FileAccess.Write,
						FileShare.None
					)
			)
			using (
				StreamWriter writer =
					new(
						stream,
						new UTF8Encoding(
							encoderShouldEmitUTF8Identifier:
								false
						),
						bufferSize:
							4096,
						leaveOpen:
							true
					)
			)
			{
				writer.Write(
					json
				);


				writer.Flush();


				stream.Flush(
					flushToDisk:
						true
				);
			}


			File.Move(
				temporaryPath,
				path,
				overwrite:
					true
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Could not save IAP token ledger: "
					+ exception.Message
			);


			try
			{
				if (
					File.Exists(
						temporaryPath
					)
				)
				{
					File.Delete(
						temporaryPath
					);
				}
			}
			catch
			{
				// Keep the original save error as the useful one.
			}
		}
	}


	// ==================================================
	// DICTIONARY HELPERS
	// ==================================================

	private static string GetString(
		GodotDictionary dictionary,
		string key)
	{
		if (
			!dictionary.ContainsKey(
				key
			)
		)
		{
			return "";
		}


		Variant value =
			dictionary[
				key
			];


		if (
			value.VariantType
				== Variant.Type.Nil
		)
		{
			return "";
		}


		return value.AsString();
	}


	private static int GetInt(
		GodotDictionary dictionary,
		string key,
		int fallback)
	{
		if (
			!dictionary.ContainsKey(
				key
			)
		)
		{
			return fallback;
		}


		Variant value =
			dictionary[
				key
			];


		if (
			value.VariantType
				== Variant.Type.Nil
		)
		{
			return fallback;
		}


		return (int)
			value.AsInt64();
	}


	private sealed class IapLedger
	{
		public List<string>
			ProcessedPurchaseTokens
		{ get; set; } =
			[];
	}
}
