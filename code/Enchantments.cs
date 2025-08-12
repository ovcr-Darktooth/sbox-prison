using Sandbox;
using System.Collections.Generic;
using System.Numerics;
using static WebSocketUtility;
using System.Text.Json;
using System;
using Sandbox.UI.Construct;
namespace Overcreep;

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
	[Property] public OvcrServer OvcrServer { get; set; }
	private WebsocketMessage saveEnchantmentsMessage {get;set;} = new();
	private WebsocketMessage getEnchantmentsMessage {get;set;} = new();

	private TimeUntil nextSaveDB = 5f;
	//private TimeUntil nextLoadEnchants = 0f;
	public bool hasLoaded = false;
	public bool hasLoadError = false;
	public bool isMenuOpen = false;

	public Dictionary<string, int> _enchants;


	protected override void OnUpdate()
	{
		if (!IsProxy && hasLoaded && OvcrServer.isAuth && nextSaveDB <= 0f)
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

		/*if (!IsProxy && !hasLoaded && OvcrServer.isAuth && nextLoadEnchants <= 0f)
		{ 
			Log.Info("Trying to load player enchants");
			GetDB();			
			nextLoadEnchants = 5f;
		}*/

		if (Input.Pressed("Enchant_menu") && !IsProxy)
			isMenuOpen = !isMenuOpen;
			
	}
	

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			saveEnchantmentsMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "action", "updateEnchants");
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());
			WebSocketUtility.AddJsonTag(saveEnchantmentsMessage, "enchants", "{}");

			getEnchantmentsMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(getEnchantmentsMessage, "action", "getEnchants");
			WebSocketUtility.AddJsonTag(getEnchantmentsMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());

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
			// Ne peut pas dépasser le niveau d'enchantement
			if (_enchants[enchantDbName] + nbLevel > GetMaxEnchantLevel(enchantDbName))
			{
				Log.Info($"Cannot bypass enchanment level {enchantDbName}. Actual: {_enchants[enchantDbName]}, Wanted: {_enchants[enchantDbName]+nbLevel}, Max: {GetMaxEnchantLevel(enchantDbName)}");
				return;
			}

			// Définir le coût d'amélioration pour cet enchantement
			BigInteger cost = GetEnchantUpgradeCost(enchantDbName,nbLevel);

			// Vérifier si le joueur a assez d'etokens
			BigInteger currentBalance = Currencies.GetBalance(CurrenciesEnum.EToken);
			if (currentBalance < cost)
			{
				Log.Info($"Not enough E-Tokens to upgrade {enchantDbName}. Required: {cost}, Available: {currentBalance}");
				return;
			}

			// Retirer les E-Tokens nécessaires
			//cost is a BigInteger
			bool withdrawalSuccess = Currencies.WithdrawCurrency(CurrenciesEnum.EToken, (BigInteger)cost);
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
				return 0;
		} 
	}

	private BigInteger GetEnchantUpgradeCost(string enchantmentKey, int nbOfLevel = 1)
	{
		BigInteger total = 0;
		for(int i=1 ; i <= nbOfLevel; i++)
		{
			float tmpTotal = 0;
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
			total += (BigInteger)(tmpTotal + enchantRule);
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

	public int GetMaxEnchantLevel(string enchantmentKey)
	{
		switch (enchantmentKey)
		{
			case "jackhammer":
				return 5000;
			case "laser":
				return 5000;
			case "fortune":
				return 250;
			case "efficiency":
				return 100;
			default:
				Log.Info($"[GetMaxEnchantLevel] Unknown enchantment key: {enchantmentKey}");
				return 0;
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


	private void saveDB()
	{
		if (!IsProxy && hasLoaded)
		{
			//Log.Info("Saving to database");

			string jsonCurrencies = JsonSerializer.Serialize(_enchants);

			WebSocketUtility.ChangeJsonTagValue(saveEnchantmentsMessage, "enchants", jsonCurrencies);

			if (OvcrServer.IsValid())
				OvcrServer.SendMessage(saveEnchantmentsMessage);	
		}
	}

	public void GetDB()
	{
		if (!IsProxy && OvcrServer.IsValid())
			OvcrServer.SendMessage(getEnchantmentsMessage);
	}

	public void LoadEnchants(JsonElement enchants)
    {
        if (enchants.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty enchantProperty in enchants.EnumerateObject())
            {
				Enchants enchantEnum = GetEnchantEnumFromString(enchantProperty.Name);
                if (enchantEnum != Enchants.Invalid)
                {
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
