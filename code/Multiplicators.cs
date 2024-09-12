using Sandbox;
using System;
using static WebSocketUtility;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

public enum Boosters
{
	Invalid = -1,
	Dollars = 0,
    EToken
	//dans le futur : 
	//skill booster
	//gang booster ?
}

public enum Multiplicator
{
    Invalid = -1,
	Dollars = 0,
    EToken
	//dans le futur : 
	//skill booster
	//gang booster ?
}


public sealed class Multiplicators : Component
{
	[Property] public OvcrServer OvcrServer { get; set; } 
	[Property] public Currencies Currencies { get; set; } 
	[Property] public Enchantments Enchantments { get; set; } 
	public bool hasLoaded = false;
	public bool hasLoadError = false;
	private TimeUntil nextLoadMultiplicators = 3f;
    private TimeUntil nextCheckBoosters = 5f;
    private TimeUntil nextCleanUpBoosters = 1f;
	private WebsocketMessage getBoostersMessage {get;set;} = new();
	private Dictionary<Boosters, List<(float duration, float multiplicator)>> availableBoosters = new();
    private Dictionary<Boosters, (TimeUntil timeUntilExpiration, float multiplicator)> activeBoosters = new();
	
	//niveaux ?
	//récupérer le composant qui va gérer les niveaux

	//currencies pour chaque monnaie pour obtenir un multiplicateur sur la monnaie
		

	protected override void OnUpdate()
	{
		if (!IsProxy && hasLoaded && nextCheckBoosters <= 0f)
		{
			DisplayBoosters();
            CleanUpActiveBoosters();
			nextCheckBoosters = 5f;
		}

        if (!IsProxy && hasLoaded && nextCleanUpBoosters <= 0f)
        {
            CleanUpActiveBoosters();
            nextCleanUpBoosters = 1f;
        }
            

		if (!IsProxy && !hasLoaded && nextLoadMultiplicators <= 0f)
		{
			Log.Info("Trying to load player boosters");
			//Websocket.message = getCurrenciesMessage;

			//boosters not yet implemented
			//GetDB();
			nextLoadMultiplicators = 5f;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			getBoostersMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(getBoostersMessage, "action", "getBoosters");
			WebSocketUtility.AddJsonTag(getBoostersMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());


            //DEBUG:
            AddActiveBooster(Boosters.Dollars, 10f, 2f);
            AddActiveBooster(Boosters.EToken, 12f, 2f);

            AddAvailableBooster(Boosters.Dollars, 50f, 2f);
            AddAvailableBooster(Boosters.Dollars, 60f, 2f);
            AddAvailableBooster(Boosters.EToken, 70f, 2f);
            AddAvailableBooster(Boosters.EToken, 80f, 2f);
            hasLoaded = true;
            //END OF DEBUG
		}
	}

	private void GetDB()
	{
		if (!IsProxy && OvcrServer.IsValid())
			OvcrServer.SendMessage(getBoostersMessage);
	}


	public float getMultiplicator(Multiplicator multiplicator)
	{
        
        float totalMultiplicator = 1f;
        Boosters booster;

        switch(multiplicator)
        {
            case Multiplicator.Dollars:
            //level (raw +0.5$/lvl ->nope aussi, sa sa sera ajouté directement a la base, 100/250/500 ? +0.1 mult)

            //Booster
            booster = GetBoosterFromMultiplicator(multiplicator);
            totalMultiplicator += getBoosterMultiplicator(booster);

            //backpack enchant
            //outpost (dans le fuuuuutur)
            break;

            case Multiplicator.EToken:
            //Booster
            booster = GetBoosterFromMultiplicator(multiplicator);
            totalMultiplicator += getBoosterMultiplicator(booster);
            //enchants (etokenmaster -> nope, etokenmaster donne des ET direct)
            //outpost (dans le fuuuuutur)
            break;

            default:
            //Invalid type
            break;
        }

		return totalMultiplicator;
	}

	private float getBoosterMultiplicator(Boosters booster)
	{
        if (activeBoosters.ContainsKey(booster) && activeBoosters[booster].timeUntilExpiration > 0)
            return activeBoosters[booster].multiplicator;
		return 0.0f;
	}


	public void ActivateBooster(Boosters boosterType)
    {
        if (availableBoosters.ContainsKey(boosterType) && availableBoosters[boosterType].Count > 0)
        {
            var booster = availableBoosters[boosterType][0];
            availableBoosters[boosterType].RemoveAt(0);

            if (activeBoosters.ContainsKey(boosterType))
            {
                var currentBooster = activeBoosters[boosterType];
				if (currentBooster.multiplicator == booster.multiplicator)
				{
					currentBooster.timeUntilExpiration += booster.duration;
					activeBoosters[boosterType] = currentBooster;

					Log.Info($"Booster {boosterType} actif mis à jour : durée prolongée de {booster.duration} secondes, multiplicateur actuel : {currentBooster.multiplicator}.");
				} 
				else
				{
					Log.Info($"Le booster actif n'est pas du même multiplicateur, impossible de l'activer");
				}
            }
            else
            {
                activeBoosters[boosterType] = (booster.duration, booster.multiplicator);
                Log.Info($"Booster {boosterType} activé avec un multiplicateur de {booster.multiplicator} pour une durée de {booster.duration} secondes.");
            }
        }
        else
        {
            Log.Info($"Aucun booster disponible pour le type {boosterType}.");
        }
    }

	public void AddAvailableBooster(Boosters boosterType, float duration, float multiplicator)
    {
        if (!availableBoosters.ContainsKey(boosterType))
            availableBoosters[boosterType] = new List<(float, float)>();

        availableBoosters[boosterType].Add((duration, multiplicator));
        Log.Info($"Booster {boosterType} ajouté à la liste des disponibles avec un multiplicateur de {multiplicator} pour une durée de {duration} secondes.");
    }

    public void AddActiveBooster(Boosters boosterType, float duration, float multiplicator) 
    {
        activeBoosters[boosterType] = (duration, multiplicator);

        Log.Info($"Booster {boosterType} ajouté avec un multiplicateur de {multiplicator} pour une durée de {duration} secondes.");
    }

	public void LoadBoosters(JsonElement rootElement)
    {
        if (rootElement.TryGetProperty("boosters", out var boostersElement) &&
            boostersElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty boosterProperty in boostersElement.EnumerateObject())
            {
                if (Enum.TryParse(boosterProperty.Name, out Boosters boosterType) && boosterType != Boosters.Invalid)
                {
                    if (boosterProperty.Value.ValueKind == JsonValueKind.Object)
                    {
                        var boosterData = boosterProperty.Value;

                        if (boosterData.TryGetProperty("duration", out var durationProp) &&
                            boosterData.TryGetProperty("multiplicator", out var multiplicatorProp))
                        {
                            if (float.TryParse(durationProp.GetString(), out var duration) &&
                                float.TryParse(multiplicatorProp.GetString(), out var multiplicator))
                            {
                                AddAvailableBooster(boosterType, duration, multiplicator);
                            }
                            else
                            {
                                Log.Info($"Données invalides pour le booster disponible '{boosterType}'.");
                            }
                        }
                    }
                    else
                    {
                        Log.Info($"Format invalide pour le booster disponible '{boosterType}'.");
                    }
                }
                else
                {
                    Log.Info($"Type de booster inconnu reçu : {boosterProperty.Name}");
                }
            }
        }

        if (rootElement.TryGetProperty("activeBoosters", out var activeBoostersElement) &&
            activeBoostersElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty activeBoosterProperty in activeBoostersElement.EnumerateObject())
            {
                if (Enum.TryParse(activeBoosterProperty.Name, out Boosters boosterType) && boosterType != Boosters.Invalid)
                {
                    if (activeBoosterProperty.Value.ValueKind == JsonValueKind.Object)
                    {
                        var activeBoosterData = activeBoosterProperty.Value;

                        if (activeBoosterData.TryGetProperty("expirationTime", out var expirationProp) &&
                            activeBoosterData.TryGetProperty("multiplicator", out var multiplicatorProp))
                        {
                            if (DateTime.TryParse(expirationProp.GetString(), out var expirationTime) &&
                                float.TryParse(multiplicatorProp.GetString(), out var multiplicator))
                            {
                                float duration = CalculateTime(expirationTime);
                                AddActiveBooster(boosterType, duration, multiplicator);
                            }
                            else
                            {
                                Log.Info($"Données invalides pour le booster actif '{boosterType}'.");
                            }
                        }
                    }
                    else
                    {
                        Log.Info($"Format invalide pour le booster actif '{boosterType}'.");
                    }
                }
                else
                {
                    Log.Info($"Type de booster inconnu reçu : {activeBoosterProperty.Name}");
                }
            }
        }
        else
        {
            Log.Info("Format de boosters invalide reçu");
        }
    }


	public float CalculateTime(DateTime expirationTime)
    {
        TimeSpan timeSpan = expirationTime - DateTime.UtcNow;
        return (float)timeSpan.TotalSeconds;
    }

    public void DisplayBoosters()
    {
        Log.Info("=== Active Boosters ===");

        if (activeBoosters.Count == 0)
        {
            Log.Info("No active boosters.");
        }
        else
        {
            foreach (var booster in activeBoosters)
            {
                var boosterType = booster.Key;
                var (timeUntilExpiration, multiplicator) = booster.Value;
                Log.Info($"{boosterType}: {multiplicator}x multiplier, Time left: {timeUntilExpiration.Relative:F2} seconds");
            }
        }

        Log.Info("=== Available Boosters ===");

        if (availableBoosters.Count == 0)
            Log.Info("No available boosters.");
        else
        {
            foreach (var booster in availableBoosters)
            {
                var boosterType = booster.Key;
                foreach (var (duration, multiplicator) in booster.Value)
                {
                    Log.Info($"{boosterType}: {multiplicator}x multiplier, Duration: {duration} seconds");
                }
            }
        }

        Log.Info("======================");
    }

    public void CleanUpActiveBoosters()
    {
        var expiredBoosters = activeBoosters
            .Where(kvp => kvp.Value.timeUntilExpiration <= 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var boosterType in expiredBoosters)
        {
            activeBoosters.Remove(boosterType);
            Log.Info($"Booster {boosterType} has expired and has been removed.");
        }

        if (expiredBoosters.Count == 0)
            Log.Info("No boosters have expired.");
    }

	public string GetBoosterSaveDB(Boosters boosterValue)
	{
		switch (boosterValue)
		{
			case Boosters.Dollars:
				return "dollar";
			case Boosters.EToken:
				return "etoken";
			default:
				return "invalid";
		}
	}

    public Boosters GetBoosterFromMultiplicator(Multiplicator multiplicator)
    {
		switch (multiplicator)
		{
			case Multiplicator.Dollars:
				return Boosters.Dollars;
			case Multiplicator.EToken:
				return Boosters.EToken;
			default:
				return Boosters.Invalid;
		}
    }

}
