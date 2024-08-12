using System.Collections.Generic;
using Sandbox;
using System.Numerics;

public enum CurrenciesEnum
{
	Dollars,
    EToken
}

public sealed class Currencies : Component
{

	public Dictionary<string, double> _balances;
	private TimeUntil nextSaveDB = 10f;	
	private WebSocket ws;

	protected override void OnUpdate()
	{
		if (nextSaveDB <= 0f)
		{
			DisplayBalances();
			saveDB();
			nextSaveDB = 10f;
			//Log.Info();
		}
	}

	private void saveDB()
	{
		if (!IsProxy)
		{
			//Log.Info("Saving to database");
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			_balances = new Dictionary<string, double>();

			AddCurrency(CurrenciesEnum.Dollars, 0);
			AddCurrency(CurrenciesEnum.EToken, 0);

			/*ws = new WebSocket("ws://localhost:8080");
			ws.OnMessage += (sender, e) => {
				Log.Info($"Message from server: {e.Data}");
			};
			ws.Connect();

			// Example to send a message
			ws.Send(JsonSerializer.Serialize(new { action = "getUser", username = "player1" }));*/
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		//ws?.Close();
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
		string saveDbName = GetCurrencyTextSaveDB(currencyEnum);
        return _balances.ContainsKey(saveDbName) ? _balances[saveDbName] : 0;
    }

    public void DisplayBalances()
    {
        Log.Info("Wallet Balances:");
        foreach (var entry in _balances)
            Log.Info($"{entry.Key}: {entry.Value}");
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
