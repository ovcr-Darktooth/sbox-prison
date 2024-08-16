using System.Net.WebSockets;
using System.Net;
using System.Collections.Generic;
using Sandbox;
using System.Numerics;
//using System.Web.;
//using nolankicks.websocket_tools;
//using WebSocket;
//using WebSocketTools;
//using Sandbox.WebSocketTools;
using static WebSocketUtility;
using System;
using System.Linq;

public enum CurrenciesEnum
{
	Dollars,
    EToken
}

public sealed class Currencies : Component
{
	public Dictionary<string, double> _balances;
	private TimeUntil nextSaveDB = 5f;	
	private WebsocketTools Websocket;
	//[Property]
	//public WebsocketMessage Message;
	//public WebsocketMessage InternalMessage {get;set;} = new();

	protected override void OnUpdate()
	{
		if (!IsProxy && nextSaveDB <= 0f)
		{
			//DisplayBalances();
			saveDB();
			nextSaveDB = 5f;
			//Log.Info(GameObject.Network.OwnerConnection.SteamId);
			//Log.Info(GameObject.Name); 
		}
	}

	private async void saveDB()
	{
		if (!IsProxy)
		{
			Log.Info("Saving to database");
			/*if (Message.message == "{\"action\": \"updateBalance\",\"playerId\": \"76561198087405083\",\"currency\": \"dollar\",\"amount\": 150}")
				Message.message = "{\"action\": \"updateBalance\",\"playerId\": \""+ GameObject.Network.OwnerConnection.SteamId +"\",\"currency\": \"dollar\",\"amount\": 100}";
			else
				Message.message = "{\"action\": \"updateBalance\",\"playerId\": \""+ GameObject.Network.OwnerConnection.SteamId +"\",\"currency\": \"dollar\",\"amount\": 150}";*/
				//Log.Info(Message.jsonTags);

				/*Message.jsonTags.ForEach(jTag => Log.Info($"test {jTag.tag == "amount"}"));
				Message.jsonTags.ForEach(jTag => Log.Info($"test {jTag.amount}"));
				JsonTags test = Message.jsonTags.Find(jTag => jTag.tag == "amount");
				JsonTags firstTag = Message.jsonTags.First();*/
				//Log.Info(Websocket.message.jsonTags.Find(tag => tag.tag == "amount").value);
				//Log.Info(Message.jsonTags.Find(tag => tag.tag == "amount").value);
			/*if (Message.jsonTags.Find(tag => tag.tag == "amount").value == "100")
				Message.jsonTags.Find(tag => tag.tag == "amount").value = "150";
			else
				Message.jsonTags.Find(tag => tag.tag == "amount").value = "100";*/ 
			
			if (Websocket.message.jsonTags.Find(jTag => jTag.tag == "amount").value == "100")
				WebSocketUtility.ChangeJsonTagValue(Websocket.message,"amount","150");
				//Websocket.message.jsonTags.Find(tag => tag.tag == "amount").value = "150";
			else
				WebSocketUtility.ChangeJsonTagValue(Websocket.message,"amount","100");
				//Websocket.message.jsonTags.Find(tag => tag.tag == "amount").value = "100";

				Log.Info(Websocket.message.jsonTags.Find(jTag => jTag.tag == "amount").value);

			Log.Info("message usejsontags" + Websocket.message.UseJsonTags);
			Log.Info(Websocket.message is null); 

			await WebSocketUtility.SendAsync(Websocket);
		}
	}

	private void OnWSMessageReceived(string message)
    {
        Log.Info("Server responded: " + message);
    }


	protected override void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			Websocket = new WebsocketTools();
			Websocket.url = "ws://localhost:8080";

			/*WebSocketUtility.AddJsonTag(InternalMessage,"action","updateBalance");
			WebSocketUtility.AddJsonTag(InternalMessage,"playerId","76561198087405083");
			WebSocketUtility.AddJsonTag(InternalMessage,"currency","dollar");
			WebSocketUtility.AddJsonTag(InternalMessage,"amount","100");*/

			/*Websocket.message.jsonTags.Add(new JsonTags{tag = "action", value = "updateBalance"});
			Websocket.message.jsonTags.Add(new JsonTags{tag = "playerId", value = "76561198087405083"});
			Websocket.message.jsonTags.Add(new JsonTags{tag = "currency", value = "dollar"});
			Websocket.message.jsonTags.Add(new JsonTags{tag = "amount", value = "100"});*/

			//Websocket.message = Message;//InternalMessage;
			//Log.Info(Websocket.message is null);
			//Websocket.message = InternalMessage;

			Websocket.message = new WebsocketMessage();
			Websocket.message.UseJsonTags = true;
			Log.Info(Websocket.message is null); 

			WebSocketUtility.AddJsonTag(Websocket.message,"action","updateBalance");
			WebSocketUtility.AddJsonTag(Websocket.message,"playerId","76561198087405083");
			WebSocketUtility.AddJsonTag(Websocket.message,"currency","dollar");
			WebSocketUtility.AddJsonTag(Websocket.message,"amount","100");

			Log.Info(Websocket.message is null);

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
