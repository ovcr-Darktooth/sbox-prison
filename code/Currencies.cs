using System.Net.WebSockets;
using System.Net;
using System.Collections.Generic;
using Sandbox;
using System.Numerics;
using static WebSocketUtility;
using System;
using System.Linq;
using System.Text.Json;
namespace Overcreep;

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
	private TimeUntil nextLoadCurrencies = 3f;
	[Property] public OvcrServer OvcrServer { get; set; } 
	public bool hasLoaded = false;
	public bool hasLoadError = false;

	private WebsocketMessage saveCurrenciesMessage {get;set;} = new();
	private WebsocketMessage getCurrenciesMessage {get;set;} = new();

	protected override void OnUpdate()
	{
		if (!IsProxy && hasLoaded && OvcrServer.isAuth && nextSaveDB <= 0f)
		{
			//DisplayBalances();
			saveDB();
			nextSaveDB = 5f;
		}

		if (!IsProxy && !hasLoaded && OvcrServer.isAuth && nextLoadCurrencies <= 0f)
		{
			Log.Info("Trying to load player currencies");
			GetDB();
			nextLoadCurrencies = 5f;
		}
	}

	private void saveDB()
	{
		if (!IsProxy && hasLoaded)
		{
			//Log.Info("Saving to database");

			string jsonCurrencies = JsonSerializer.Serialize(_balances);

			WebSocketUtility.ChangeJsonTagValue(saveCurrenciesMessage, "currencies", jsonCurrencies);

			if (OvcrServer.IsValid())
				OvcrServer.SendMessage(saveCurrenciesMessage);
		}
	}

	private void GetDB()
	{
		if (!IsProxy && OvcrServer.IsValid())
			OvcrServer.SendMessage(getCurrenciesMessage);
	}

	public void LoadBalances(JsonElement balances)
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
			saveCurrenciesMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(saveCurrenciesMessage, "action", "updateBalance");
			WebSocketUtility.AddJsonTag(saveCurrenciesMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());
			WebSocketUtility.AddJsonTag(saveCurrenciesMessage, "currencies", "{}");

			getCurrenciesMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(getCurrenciesMessage, "action", "getBalances");
			WebSocketUtility.AddJsonTag(getCurrenciesMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());

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

			_balances[saveDbName] = Math.Round(_balances[saveDbName], 2);
		}
    }

    public bool WithdrawCurrency(CurrenciesEnum currencyEnum, double amount)
    {
		if (!IsProxy)
		{
			string saveDbName = GetCurrencyTextSaveDB(currencyEnum);
			if (_balances.ContainsKey(saveDbName) && _balances[saveDbName] >= amount)
			{
				_balances[saveDbName] = Math.Round(_balances[saveDbName] - amount, 2);
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