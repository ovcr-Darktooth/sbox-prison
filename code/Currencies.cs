using System.Collections.Generic;
using Sandbox;

public enum CurrenciesEnum
{
    Dollars,
    EToken
}

public sealed class Currencies : Component
{

	public Dictionary<string, decimal> _balances;
	protected override void OnUpdate()
	{
		//DisplayBalances();
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			_balances = new Dictionary<string, decimal>();

			AddCurrency(CurrenciesEnum.Dollars, 123);
			AddCurrency(CurrenciesEnum.EToken, 456);
		}
	}

	public void AddCurrency(CurrenciesEnum currencyEnum, decimal amount)
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

    public bool WithdrawCurrency(CurrenciesEnum currencyEnum, decimal amount)
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

    public decimal GetBalance(CurrenciesEnum currencyEnum)
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

	public static string GetCurrencyTextBig(CurrenciesEnum currencyValue)
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
