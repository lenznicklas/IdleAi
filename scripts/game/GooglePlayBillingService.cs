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


	private sealed class ProductRuntimeInfo
	{
		public string FormattedPrice { get; init; } =
			"";

		public string PurchaseOptionId { get; init; } =
			"";
	}


	private GodotObject? _billing;


	private readonly Dictionary<
		string,
		ProductRuntimeInfo
	> _products =
		new(
			StringComparer.Ordinal
		);


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


	private string _purchaseFlowProductId =
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


	public bool PurchaseFlowActive =>
		_purchaseFlowActive;


	public string PurchaseFlowProductId =>
		_purchaseFlowProductId;


	public GooglePlayBillingService()
	{
		LoadLedger();
	}


	// ==================================================
	// PUBLIC PRODUCT STATE
	// ==================================================

	public bool HasProduct(
		string productId)
	{
		return _products.ContainsKey(
			productId
		);
	}


	public bool CanPurchase(
		string productId)
	{
		return
			IsAvailable
			&& _connected
			&& HasProduct(
				productId
			)
			&& !_purchaseFlowActive
			&& _consumingPurchaseTokens.Count == 0;
	}


	public string GetFormattedPrice(
		string productId)
	{
		return
			_products.TryGetValue(
				productId,
				out ProductRuntimeInfo? info
			)
				? info.FormattedPrice
				: "";
	}


	public string GetStatusText(
		string productId)
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
				_purchaseFlowProductId.Equals(
					productId,
					StringComparison.Ordinal
				)
					? "PURCHASE IN PROGRESS..."
					: "WAITING FOR CURRENT PURCHASE...";
		}


		if (_consumingPurchaseTokens.Count > 0)
		{
			return
				"FINALIZING PURCHASE...";
		}


		if (
			_queryingProducts
			&& !HasProduct(
				productId
			)
		)
		{
			return
				"LOADING GOOGLE PLAY PRICE...";
		}


		if (!HasProduct(productId))
		{
			return
				"PRODUCT NOT AVAILABLE FROM GOOGLE PLAY";
		}


		string price =
			GetFormattedPrice(
				productId
			);


		if (
			!string.IsNullOrWhiteSpace(
				price
			)
		)
		{
			return
				"GOOGLE PLAY  •  "
				+ price;
		}


		return
			"GOOGLE PLAY READY";
	}


	public string GetButtonText(
		string productId)
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
				_purchaseFlowProductId.Equals(
					productId,
					StringComparison.Ordinal
				)
					? "PURCHASE IN PROGRESS..."
					: "PLEASE WAIT...";
		}


		if (_consumingPurchaseTokens.Count > 0)
		{
			return
				"FINALIZING...";
		}


		if (
			_queryingProducts
			&& !HasProduct(
				productId
			)
		)
		{
			return
				"LOADING PRICE...";
		}


		if (!HasProduct(productId))
		{
			return
				"PRODUCT UNAVAILABLE";
		}


		string price =
			GetFormattedPrice(
				productId
			);


		if (
			!string.IsNullOrWhiteSpace(
				price
			)
		)
		{
			return
				"BUY  •  "
				+ price;
		}


		return
			"BUY";
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
		 * Google Play Billing only exists in Android builds.
		 * Keeping desktop safe lets the normal Godot editor run
		 * the Shop without the native Android singleton.
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
		 * constructor. The C# integration talks directly to the
		 * same native singleton.
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


		_purchaseFlowProductId =
			"";


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


		_purchaseFlowProductId =
			"";


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


		_purchaseFlowProductId =
			"";


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


		_products.Clear();


		_billing.Call(
			"queryProductDetails",
			IapCatalog.GetProductIds(),
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
				!IapCatalog.TryGet(
					productId,
					out _
				)
			)
			{
				continue;
			}


			ProductRuntimeInfo runtime =
				ReadOneTimeOffer(
					product
				);


			_products[
				productId
			] =
				runtime;


			GD.Print(
				"Google Play product ready: ",
				productId,
				" | price=",
				runtime.FormattedPrice,
				" | purchaseOptionId=",
				runtime.PurchaseOptionId
			);
		}


		foreach (
			IapProductDefinition definition
				in IapCatalog.GetAll()
		)
		{
			if (
				!_products.ContainsKey(
					definition.ProductId
				)
			)
			{
				GD.PushWarning(
					"Google Play did not return product details for "
						+ definition.ProductId
				);
			}
		}


		Changed?.Invoke();
	}


	private static ProductRuntimeInfo ReadOneTimeOffer(
		GodotDictionary product)
	{
		if (
			!product.ContainsKey(
				"one_time_purchase_offer_details_list"
			)
		)
		{
			return new ProductRuntimeInfo();
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
			return new ProductRuntimeInfo();
		}


		GodotArray offers =
			offersValue.AsGodotArray();


		if (offers.Count == 0)
		{
			return new ProductRuntimeInfo();
		}


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


			/*
			 * Prefer the normal/base purchase option over a
			 * limited promotional offer.
			 */
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
		{
			return new ProductRuntimeInfo();
		}


		return new ProductRuntimeInfo
		{
			FormattedPrice =
				GetString(
					selected,
					"formatted_price"
				),


			/*
			 * This is the purchase-option ID returned by Google.
			 * For your first pack this can be "10001". It is NOT
			 * treated as a separate product ID.
			 */
			PurchaseOptionId =
				GetString(
					selected,
					"purchase_option_id"
				)
		};
	}


	// ==================================================
	// PURCHASE
	// ==================================================

	public void Purchase(
		string productId)
	{
		if (
			!IapCatalog.TryGet(
				productId,
				out IapProductDefinition definition
			)
		)
		{
			MessageRequested?.Invoke(
				"Unknown Data Shard product."
			);

			return;
		}


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


		if (
			!_products.TryGetValue(
				productId,
				out ProductRuntimeInfo? product
			)
		)
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
				productId,
				product.PurchaseOptionId,
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


			_purchaseFlowProductId =
				definition.ProductId;


			Changed?.Invoke();

			return;
		}


		string debugMessage =
			GetString(
				result,
				"debug_message"
			);


		GD.PushWarning(
			"Could not launch Google Play purchase for "
				+ productId
				+ ": "
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


		_purchaseFlowProductId =
			"";


		Changed?.Invoke();
	}


	private void OnPurchaseUpdated(
		GodotDictionary response)
	{
		_purchaseFlowActive =
			false;


		_purchaseFlowProductId =
			"";


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
			 * Already rewarded. Only retry consumption.
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


	private static bool TryGetSupportedProductId(
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
				IapCatalog.TryGet(
					id,
					out _
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
			GD.PushWarning(
				"Google Play consume failed: "
					+ GetString(
						response,
						"debug_message"
					)
			);


			MessageRequested?.Invoke(
				"Purchased Shards were saved. Google Play will finalize the purchase automatically."
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
				// Keep the original error as the useful one.
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
