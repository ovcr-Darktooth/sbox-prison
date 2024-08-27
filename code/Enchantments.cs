using Sandbox;
using System.Collections.Generic;
using static WebSocketUtility;
using System.Text.Json;
using System;
using Sandbox.UI.Construct;

public enum Enchants
{
	Invalid = -1,
    Jackhammer = 0,
    Laser,
    Fortune,
    Efficiency
}



public sealed class Enchantments : Component
{
	[Property]
	public Currencies Currencies;
	private WebsocketTools Websocket;
	private WebsocketMessage authEnchantsMessage {get;set;} = new();
	private WebsocketMessage saveEnchantmentsMessage {get;set;} = new();
	private WebsocketMessage getEnchantmentsMessage {get;set;} = new();

	private TimeUntil nextSaveDB = 5f;
	private TimeUntil nextLoadEnchants = 0f;
	private TimeUntil nextAuth = 2f;
	private bool hasLoaded = false;
	public bool hasLoadError = false;
	private bool isAuth = false;
	
	public bool isMenuOpen = false;

	public Dictionary<string, int> _enchants;


	protected override void OnUpdate()
	{
		if (!IsProxy && hasLoaded && isAuth && nextSaveDB <= 0f)
		{
			try {
				saveDB();
			}
			catch
			{
				Log.Info("Error on sending enchants WS for save");
			}
			
			nextSaveDB = 5f;
		}

		if (!IsProxy && !hasLoaded && isAuth && nextLoadEnchants <= 0f)
		{
			Log.Info("Trying to load player enchants");
			Websocket.message = getEnchantmentsMessage;

			GetDB();			

			nextLoadEnchants = 5f;
		}

		if (!IsProxy && !isAuth && nextAuth <= 0f)
		{
			Log.Info("Trying to auth");
			Websocket.message = authEnchantsMessage;

			WSAuth();
			nextAuth = 3f;
		}

		if (Input.Pressed("Enchant_menu") && !IsProxy)
			isMenuOpen = !isMenuOpen;
			
	}
	

	protected override async void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			Websocket = new WebsocketTools();
			Websocket.url = "ws://websocket.overcreep.ovh:10706";
			//Websocket.url = "ws://localhost:8080";

			var enchantsToken = await Sandbox.Services.Auth.GetToken( "enchants" );

			authEnchantsMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(authEnchantsMessage, "action", "auth");
			WebSocketUtility.AddJsonTag(authEnchantsMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());
			WebSocketUtility.AddJsonTag(authEnchantsMessage, "token", enchantsToken);

			saveEnchantmentsMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "action", "updateEnchants");
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "enchants", "{}");

			getEnchantmentsMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(getEnchantmentsMessage, "action", "getEnchants");
			WebSocketUtility.AddJsonTag(getEnchantmentsMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());

			Websocket.onMessageReceived = OnWSMessageReceived;

			Websocket.message = getEnchantmentsMessage;	

			_enchants = new Dictionary<string, int>();

			AddEnchant(Enchants.Jackhammer, 5);
			AddEnchant(Enchants.Laser, 5);
			AddEnchant(Enchants.Fortune, 5);
			AddEnchant(Enchants.Efficiency, 5);
		}
	}

	public void AddEnchant(Enchants enchant, int amount)
    {
		if (!IsProxy)
		{
			string saveDbName = GetEnchantmentTextSaveDB(enchant);
			if (_enchants.ContainsKey(saveDbName))
				_enchants[saveDbName] += amount;
			else
				_enchants[saveDbName] = amount;
		}
    }

	public void SetEnchant(Enchants enchant, int amount)
    {
		if (!IsProxy)
		{
			string saveDbName = GetEnchantmentTextSaveDB(enchant);
			_enchants[saveDbName] = amount;
		}
    }

	public void UpgradeEnchant(string enchantDbName, int nbLevel=1)
	{

		//Log.Info($"Click enchant: {enchantDbName}");
		if (!IsProxy)
		{
			// Définir le coût d'amélioration pour cet enchantement
			float cost = GetEnchantUpgradeCost(enchantDbName,nbLevel);

			// Vérifier si le joueur a assez d'etokens
			double currentBalance = Currencies.GetBalance(CurrenciesEnum.EToken);
			if (currentBalance < cost)
			{
				Log.Info($"Not enough E-Tokens to upgrade {enchantDbName}. Required: {cost}, Available: {currentBalance}");
				return;
			}

			// Retirer les E-Tokens nécessaires
			bool withdrawalSuccess = Currencies.WithdrawCurrency(CurrenciesEnum.EToken, cost);
			if (!withdrawalSuccess)
			{
				Log.Info($"Failed to withdraw E-Tokens for upgrading {enchantDbName}.");
				return;
			}

			// Mettre à jour l'enchantement
			if (_enchants.ContainsKey(enchantDbName))
			{
				_enchants[enchantDbName]++;
				Log.Info($"Successfully upgraded {enchantDbName}. New level: {_enchants[enchantDbName]}");
			}
			else
				Log.Info($"Enchantments key {enchantDbName} not found.");
		}
	}

	public float getDefaultChanceOfEnchant(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return 0.001f;
			case Enchants.Laser:
				return 1;
			case Enchants.Fortune: //no uses
				return 1;
			case Enchants.Efficiency: //no uses
				return 1;
			default:
				return 0.0f;
		} 
	}

	public float getChanceOfEnchant(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return getDefaultChanceOfEnchant(Enchants.Jackhammer) * _enchants[GetEnchantmentTextSaveDB(Enchants.Jackhammer)];
			case Enchants.Laser:
				return getDefaultChanceOfEnchant(Enchants.Laser) * _enchants[GetEnchantmentTextSaveDB(Enchants.Laser)];
			case Enchants.Fortune:
				return getDefaultChanceOfEnchant(Enchants.Fortune) * _enchants[GetEnchantmentTextSaveDB(Enchants.Fortune)];
			case Enchants.Efficiency:
				return getDefaultChanceOfEnchant(Enchants.Efficiency) * _enchants[GetEnchantmentTextSaveDB(Enchants.Efficiency)];
			default:
				return 0.0f;
		} 
	}

	private float GetEnchantUpgradeCost(string enchantmentKey, int nbOfLevel = 1)
	{
		float total = 0f;
		for(int i=1 ; i <= nbOfLevel; i++)
		{
			float tmpTotal = 0f;
			float enchantRule = GetEnchantUpgradeRule(enchantmentKey, _enchants[enchantmentKey]+i);
			switch (enchantmentKey)
			{
				case "jackhammer":
					tmpTotal = 10000f;
					break;
				case "laser":
					tmpTotal = 15000f;
					break;
				case "fortune":
					tmpTotal = 50000f;
					break;
				case "efficiency":
					tmpTotal = 25000f;
					break;
				default:
					Log.Info($"[GetEnchantUpgradeCost] Unknown enchantment key: {enchantmentKey}");
					tmpTotal = 0f;
					break;
			}
			total += (tmpTotal + enchantRule);
		}
		return total;
	}

	private float GetEnchantUpgradeRule(string enchantmentKey, int levelToCalculate)
	{
		if (_enchants.ContainsKey(enchantmentKey))
		{
			switch (enchantmentKey)
			{
				case "jackhammer":
					return 20f+(0.0018f*20f*levelToCalculate);
				case "laser":
					return 15000f;
				case "fortune":
					return 50000f;
				case "efficiency":
					return _enchants[enchantmentKey] * 100f;
				default:
					Log.Info($"[GetEnchantUpgradeRule] Unknown enchantment key: {enchantmentKey}");
					return 0f;
			}
		}
		else
		{
			Log.Info($"[GetEnchantUpgradeRule] Player enchants doesn't contain enchantment key: {enchantmentKey}");
			return -1f;
		}
	}

	public bool CanUpgradeEnchant(string enchantmentKey)
	{
		switch (enchantmentKey)
		{
			case "jackhammer":
				return true;
			case "laser":
				return false;
			case "fortune":
				return false;
			case "efficiency":
				return false;
			default:
				Log.Info($"[CanUpgradeEnchant] Unknown enchantment key: {enchantmentKey}");
				return false;
		}
	}

	public static string GetEnchantmentText(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return "Jackhammer";
			case Enchants.Laser:
				return "Laser";
			case Enchants.Fortune:
				return "Fortune";
			case Enchants.Efficiency:
				return "Efficiency";
			default:
				return "Enchant not valid";
		}
	}

	public static string GetEnchantmentTextSaveDB(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return "jackhammer";
			case Enchants.Laser:
				return "laser";
			case Enchants.Fortune:
				return "fortune";
			case Enchants.Efficiency:
				return "efficiency";
			default:
				return "invalid";
		}
	}

	public Enchants GetEnchantEnumFromString(string currencyText)
	{
		switch (currencyText.ToLower())
		{
			case "jackhammer":
				return Enchants.Jackhammer;
			case "laser":
				return Enchants.Laser;
			case "fortune":
				return Enchants.Fortune;
			case "efficiency":
				return Enchants.Efficiency;
			default:
				return Enchants.Invalid;
		}
	}


	private async void saveDB()
	{
		if (!IsProxy && hasLoaded)
		{
			//Log.Info("Saving to database");

			string jsonCurrencies = JsonSerializer.Serialize(_enchants);

			Websocket.message = saveEnchantmentsMessage;

			WebSocketUtility.ChangeJsonTagValue(Websocket.message, "enchants", jsonCurrencies);

			await Task.RunInThreadAsync( async () =>
			{
				try
				{
					await WebSocketUtility.SendAsync( Websocket );
				}
				catch ( Exception ex )
				{
					hasLoadError = true;
					Log.Info($"[Enchants]saveDB, error on websocket: {ex.Message}");
				}
			} );
			
		}
	}

	private async void GetDB()
	{
		if (!IsProxy)
		{
			await Task.RunInThreadAsync( async () =>
			{
				try
				{
					await WebSocketUtility.SendAsync( Websocket );
				}
				catch ( Exception ex )
				{
					hasLoadError = true;
					Log.Info($"[Enchants]GetDB, error on websocket: {ex.Message}");
				}
			} );
		}
	}

	private async void WSAuth()
	{
		if (!IsProxy)
		{
			await Task.RunInThreadAsync( async () =>
			{
				try
				{
					await WebSocketUtility.SendAsync( Websocket );
				}
				catch ( Exception ex )
				{
					hasLoadError = true;
					Log.Info($"[Enchants]WSAuth, error on websocket: {ex.Message}");
				}
			} );
		}
	}

	private void OnWSMessageReceived(string message)
    {
        Log.Info("Server responded: " + message);

		try
        {
            // Désérialiser le message en un JsonDocument
            using (JsonDocument doc = JsonDocument.Parse(message))
            {
                JsonElement root = doc.RootElement;

                // Vérifier le type d'action
                if (root.TryGetProperty("action", out JsonElement actionElement))
                {
                    string action = actionElement.GetString();

                    switch (action)
                    {
						case "auth":
							isAuth = true;
							break;
                        case "enchantsUpdated":
                            // Ne rien faire ou ajouter votre logique si nécessaire
                            break;

                        case "getEnchants":
                            if (root.TryGetProperty("enchants", out JsonElement enchantsElement))
                            {
                                LoadEnchants(enchantsElement);
								hasLoaded = true;
								hasLoadError = false;
                            }
                            else
                                Log.Info("Enchants property is missing");
                            break;

                        default:
                            Log.Info("Unknown action: " + action);
                            break;
                    }
                }
                else
                    Log.Info("Action property is missing");
            }
        }
        catch (JsonException ex)
        {
            Log.Info("Error parsing server response: " + ex.Message);
        }
    }

	private void LoadEnchants(JsonElement enchants)
    {
        if (enchants.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty enchantProperty in enchants.EnumerateObject())
            {
				Enchants enchantEnum = GetEnchantEnumFromString(enchantProperty.Name);
                if (enchantEnum != Enchants.Invalid)
                {
                    // Convertir la valeur en double, ou utiliser une valeur par défaut en cas d'erreur
                    if (int.TryParse(enchantProperty.Value.GetString(), out var amount))
                        SetEnchant(enchantEnum, amount);
                    else
                        Log.Info($"Invalid amount for enchant '{enchantProperty.Name}': {enchantProperty.Value}");
                }
                else
                    Log.Info($"Unknown enchant type received: {enchantProperty.Name}");
            }
        }
        else
            Log.Info("Invalid enchants format received");
    }
}
