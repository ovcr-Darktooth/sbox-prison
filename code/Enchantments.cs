using Sandbox;
using System.Collections.Generic;
using static WebSocketUtility;
using System.Text.Json;


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
	private WebsocketMessage saveEnchantmentsMessage {get;set;} = new();
	private WebsocketMessage getEnchantmentsMessage {get;set;} = new();

	private TimeUntil nextSaveDB = 5f;
	private TimeUntil nextLoadEnchants = 0f;
	private bool hasLoaded = false;

	public Dictionary<string, int> _enchants;


	protected override void OnUpdate()
	{
		if (!IsProxy && hasLoaded && nextSaveDB <= 0f)
		{
			saveDB();
			nextSaveDB = 5f;
		}

		if (!IsProxy && !hasLoaded && nextLoadEnchants <= 0f)
		{
			Log.Info("Trying to load player enchants");
			Websocket.message = getEnchantmentsMessage;

			_ = WebSocketUtility.SendAsync(Websocket);
			nextLoadEnchants = 5f;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			Websocket = new WebsocketTools();
			Websocket.url = "wss://overcreep.loca.lt";
			//Websocket.url = "ws://136.243.63.156:10706";
			//Websocket.url = "ws://localhost:8080";

			saveEnchantmentsMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "action", "updateEnchants");
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "enchants", "{}");

			getEnchantmentsMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(getEnchantmentsMessage, "action", "getEnchants");
			WebSocketUtility.AddJsonTag(getEnchantmentsMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());

			Websocket.onMessageReceived = OnWSMessageReceived;

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

	public float getDefaultChanceOfEnchant(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return 1;
			case Enchants.Laser:
				return 1;
			case Enchants.Fortune:
				return 1;
			case Enchants.Efficiency:
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


	public float getPriceOfEnum(Enchants enchantment, int level)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return 5;
			case Enchants.Laser:
				return 5;
			case Enchants.Fortune:
				return 15;
			case Enchants.Efficiency:
				return 20;
			default:
				return 0.0f;
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
			Log.Info("Saving to database");

			string jsonCurrencies = JsonSerializer.Serialize(_enchants);

			Websocket.message = saveEnchantmentsMessage;

			WebSocketUtility.ChangeJsonTagValue(Websocket.message, "enchants", jsonCurrencies);

			await WebSocketUtility.SendAsync(Websocket);
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
                        case "enchantsUpdated":
                            // Ne rien faire ou ajouter votre logique si nécessaire
                            break;

                        case "getEnchants":
                            if (root.TryGetProperty("enchants", out JsonElement enchantsElement))
                            {
                                LoadEnchants(enchantsElement);
								hasLoaded = true;
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
