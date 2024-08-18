using System.Net.WebSockets;
using System.Net;
using System.Collections.Generic;
using Sandbox;
using System.Numerics;
using static WebSocketUtility;
using System;
using System.Linq;
using System.Text.Json;

public enum CurrenciesEnum
{
	Invalid = -1,
	Dollars = 0,
    EToken
}

public sealed class Currencies : Component
{
	public Dictionary<string, double> _balances;
	private TimeUntil nextSaveDB = 5f;
	private TimeUntil nextLoadCurrencies = 0f;
	private WebsocketTools Websocket;
	private bool hasLoaded = false;

	private WebsocketMessage saveCurrenciesMessage {get;set;} = new();
	private WebsocketMessage getCurrenciesMessage {get;set;} = new();

	protected override void OnUpdate()
	{
		if (!IsProxy && hasLoaded && nextSaveDB <= 0f)
		{
			//DisplayBalances();
			saveDB();
			nextSaveDB = 5f;
		}

		if (!IsProxy && !hasLoaded && nextLoadCurrencies <= 0f)
		{
			Log.Info("Trying to load player currencies");
			Websocket.message = getCurrenciesMessage;

			_ = WebSocketUtility.SendAsync(Websocket);
			nextLoadCurrencies = 5f;
		}
	}

	private async void saveDB()
	{
		if (!IsProxy && hasLoaded)
		{
			Log.Info("Saving to database");

			string jsonCurrencies = JsonSerializer.Serialize(_balances);

			Websocket.message = saveCurrenciesMessage;

			WebSocketUtility.ChangeJsonTagValue(Websocket.message, "currencies", jsonCurrencies);

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
                        case "balanceUpdated":
                            // Ne rien faire ou ajouter votre logique si nécessaire
                            break;

                        case "getBalances":
                            // Charger les monnaies depuis la réponse
                            if (root.TryGetProperty("balances", out JsonElement balancesElement))
                            {
                                LoadBalances(balancesElement);
								hasLoaded = true;
                            }
                            else
                                Log.Info("Balances property is missing");
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

	private void LoadBalances(JsonElement balances)
    {
        if (balances.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty currencyProperty in balances.EnumerateObject())
            {
				CurrenciesEnum currencyEnum = GetCurrencyEnumFromString(currencyProperty.Name);
                if (currencyEnum != CurrenciesEnum.Invalid)
                {
                    // Convertir la valeur en double, ou utiliser une valeur par défaut en cas d'erreur
                    if (double.TryParse(currencyProperty.Value.GetString(), out var amount))
                        AddCurrency(currencyEnum, amount);
                    else
                        Log.Info($"Invalid amount for currency '{currencyProperty.Name}': {currencyProperty.Value}");
                }
                else
                    Log.Info($"Unknown currency type received: {currencyProperty.Name}");
            }
        }
        else
            Log.Info("Invalid balances format received");
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

			saveCurrenciesMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(saveCurrenciesMessage, "action", "updateBalance");
			WebSocketUtility.AddJsonTag(saveCurrenciesMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());
			WebSocketUtility.AddJsonTag(saveCurrenciesMessage, "currencies", "{}");

			getCurrenciesMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(getCurrenciesMessage, "action", "getBalances");
			WebSocketUtility.AddJsonTag(getCurrenciesMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());

			Websocket.onMessageReceived = OnWSMessageReceived;

			_balances = new Dictionary<string, double>();

			AddCurrency(CurrenciesEnum.Dollars, 0);
			AddCurrency(CurrenciesEnum.EToken, 0);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	public void AddCurrency(CurrenciesEnum currencyEnum, double amount)
    {
		if (!IsProxy)
		{
			string saveDbName = GetCurrencyTextSaveDB(currencyEnum);
			if (_balances.ContainsKey(saveDbName))
				_balances[saveDbName] += amount;
			else
				_balances[saveDbName] = amount;
		}
    }

    public bool WithdrawCurrency(CurrenciesEnum currencyEnum, double amount)
    {
		if (!IsProxy)
		{
			string saveDbName = GetCurrencyTextSaveDB(currencyEnum);
			if (_balances.ContainsKey(saveDbName) && _balances[saveDbName] >= amount)
			{
				_balances[saveDbName] -= amount;
				return true;
			}
			return false;
		}
		return false;
    }

    public double GetBalance(CurrenciesEnum currencyEnum)
    {
		if(!IsProxy)
		{
			string saveDbName = GetCurrencyTextSaveDB(currencyEnum);
			return _balances.ContainsKey(saveDbName) ? _balances[saveDbName] : 0;
		}
		return -1;
    }

    public void DisplayBalances()
    {
		if (!IsProxy)
		{
			Log.Info("Wallet Balances:");
			foreach (var entry in _balances)
				Log.Info($"{entry.Key}: {entry.Value}");
		}
    }


	public string GetCurrencyTextSymbol(CurrenciesEnum currencyValue)
	{
		switch (currencyValue)
		{
			case CurrenciesEnum.Dollars:
				return "$";
			case CurrenciesEnum.EToken:
				return "ET";
			default:
				return "Currency not valid";
		}
	}

	public string GetCurrencyTextBig(CurrenciesEnum currencyValue)
	{
		switch (currencyValue)
		{
			case CurrenciesEnum.Dollars:
				return "Dollars";
			case CurrenciesEnum.EToken:
				return "E-Tokens";
			default:
				return "Currency not valid";
		}
	}

	public string GetCurrencyTextSaveDB(CurrenciesEnum currencyValue)
	{
		switch (currencyValue)
		{
			case CurrenciesEnum.Dollars:
				return "dollar";
			case CurrenciesEnum.EToken:
				return "etoken";
			default:
				return "invalid";
		}
	}

	public CurrenciesEnum GetCurrencyEnumFromString(string currencyText)
	{
		switch (currencyText.ToLower())
		{
			case "dollar":
				return CurrenciesEnum.Dollars;
			case "etoken":
				return CurrenciesEnum.EToken;
			default:
				return CurrenciesEnum.Invalid;
		}
	}
}



/*public class Currency
{
    public string Name { get; private set; }
    public string Symbol { get; private set; }

    public Currency(CurrenciesEnum enumValue)
    {
        Name = GetCurrencyTextSaveDB(enumValue);
        Symbol = GetCurrencyTextSymbol(enumValue);
    }

	public static string GetCurrencyTextSaveDB(CurrenciesEnum currencyValue)
	{
		switch (currencyValue)
		{
			case CurrenciesEnum.Dollars:
				return "dollar";
			case CurrenciesEnum.EToken:
				return "etoken";
			default:
				return "invalid";
		}
	}

	public static string GetCurrencyTextSymbol(CurrenciesEnum currencyValue)
	{
		switch (currencyValue)
		{
			case CurrenciesEnum.Dollars:
				return "$";
			case CurrenciesEnum.EToken:
				return "ET";
			default:
				return "Currency not valid";
		}
	}
}*/
