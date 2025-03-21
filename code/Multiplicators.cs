using Sandbox;
using System;
using static WebSocketUtility;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
namespace Overcreep;

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
    private bool activateNew = true;
	public bool hasLoaded = false;
	public bool hasLoadError = false;
    public bool isMenuOpen = false;
	private TimeUntil nextLoadMultiplicators = 5f;
    private TimeUntil nextCheckBoosters = 5f;
    private TimeUntil nextCleanUpBoosters = 1f;
    private TimeUntil nextPendingBoosterSend = 3f;
	private WebsocketMessage getBoostersMessage {get;set;} = new();
    private WebsocketMessage updateActiveBoostersMessage {get;set;} = new();
    private WebsocketMessage giveBoosterMessage {get;set;} = new();
    private WebsocketMessage activateBoosterMessage {get;set;} = new();

    private List<(Boosters type, float duration, float multiplicator)> pendingBoosters = new();
	public Dictionary<Boosters, List<(float duration, float multiplicator)>> availableBoosters = new();
    public Dictionary<Boosters, (TimeUntil timeUntilExpiration, float multiplicator)> activeBoosters = new();
	
	//niveaux ?
	//récupérer le composant qui va gérer les niveaux

	//currencies pour chaque monnaie pour obtenir un multiplicateur sur la monnaie
		

	protected override void OnUpdate()
	{

        if (Input.Pressed("Boosters_menu") && !IsProxy)
            ToggleMenu();

        if (!IsProxy && hasLoaded && OvcrServer.isAuth && pendingBoosters.Count > 0 && nextPendingBoosterSend <= 0f)
        {
            var (type, duration, multiplicator) = pendingBoosters[0];

            SendBoosterToServer(type, duration, multiplicator);
            pendingBoosters.RemoveAt(0);
            
            // Réinitialiser le timer
            nextPendingBoosterSend = 0.1f;
            
            Log.Info($"Envoi du booster en attente {type} {duration} {multiplicator} au serveur. Reste {pendingBoosters.Count} boosters en attente.");
        }

		if (!IsProxy && hasLoaded && OvcrServer.isAuth && nextCheckBoosters <= 0f)
		{
			//DisplayBoosters();
            CleanUpActiveBoosters();
			nextCheckBoosters = 5f;
		}

        if (!IsProxy && hasLoaded && OvcrServer.isAuth && nextCleanUpBoosters <= 0f)
        {
            CleanUpActiveBoosters();

            if (activateNew)
            {
                ActivateBooster(Boosters.EToken, 70f, 2f);
                activateNew = false;
            }
                
            nextCleanUpBoosters = 1f;
            //SendActiveBoosters();
        }
            

		if (!IsProxy && !hasLoaded && OvcrServer.isAuth && nextLoadMultiplicators <= 0f)
		{
			Log.Info("Trying to load player boosters");
			//Websocket.message = getCurrenciesMessage;

			//boosters not yet implemented
			//GetDB();

			//DEBUG:
            //AddActiveBooster(Boosters.Dollars, 10f, 2f);
            //AddActiveBooster(Boosters.EToken, 12f, 2f);

            GiveBooster(Boosters.Dollars, 50f, 2f);
            //GiveBooster(Boosters.Dollars, 60f, 2f);
            GiveBooster(Boosters.EToken, 70f, 2f);
            //GiveBooster(Boosters.EToken, 80f, 2f);
            hasLoaded = true;
            //END OF DEBUG  

			nextLoadMultiplicators = 999f;
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			getBoostersMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(getBoostersMessage, "action", "getBoosters");
			WebSocketUtility.AddJsonTag(getBoostersMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());


            updateActiveBoostersMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "action", "updateActiveBoosters");
            WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "activeBoosters", "{}");
			WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());


            giveBoosterMessage.UseJsonTags = true;
            WebSocketUtility.AddJsonTag(giveBoosterMessage, "action", "giveBooster");
            WebSocketUtility.AddJsonTag(giveBoosterMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());
            WebSocketUtility.AddJsonTag(giveBoosterMessage, "boosterType", "");
            WebSocketUtility.AddJsonTag(giveBoosterMessage, "duration", "");
            WebSocketUtility.AddJsonTag(giveBoosterMessage, "multiplicator", "");


            activateBoosterMessage.UseJsonTags = true;
            WebSocketUtility.AddJsonTag(activateBoosterMessage, "action", "useBooster");
            WebSocketUtility.AddJsonTag(activateBoosterMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());
            WebSocketUtility.AddJsonTag(activateBoosterMessage, "boosterType", "");
            WebSocketUtility.AddJsonTag(activateBoosterMessage, "multiplicator", "");
            WebSocketUtility.AddJsonTag(activateBoosterMessage, "duration", "");

		}
	}

    protected override void OnDisabled()
    { 
        if (!IsProxy)
        {
            //Log.Info("Ondisabled multiplicators");
            //DisplayBoosters();
        }

        base.OnDisabled();
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
            //enchants (etokenmaster -> nope, etokenmaster donne des ET direct, mais cela donne une idée)
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


    public async void ActivateBooster(Boosters boosterType, float targetDuration, float targetMultiplicator)
    {
        // Vérifier si nous avons des boosters disponibles du type demandé
        if (!availableBoosters.ContainsKey(boosterType) || availableBoosters[boosterType].Count == 0)
        {
            Log.Info($"Aucun booster disponible pour le type {boosterType}.");
            return;
        }

        // Chercher un booster correspondant aux critères
        var boosterIndex = availableBoosters[boosterType].FindIndex(b => 
            b.duration == targetDuration && 
            b.multiplicator == targetMultiplicator);

        if (boosterIndex == -1)
        {
            Log.Info($"Aucun booster trouvé avec durée={targetDuration} et multiplicateur={targetMultiplicator}");
            return;
        }

        var boosterToActivate = availableBoosters[boosterType][boosterIndex];
        availableBoosters[boosterType].RemoveAt(boosterIndex);

        // Vérifier si un booster est déjà actif
        if (activeBoosters.ContainsKey(boosterType))
        {
            var currentBooster = activeBoosters[boosterType];
            if (currentBooster.multiplicator == targetMultiplicator)
            {
                // Prolonger la durée du booster actif
                currentBooster.timeUntilExpiration += targetDuration;
                activeBoosters[boosterType] = currentBooster;
                Log.Info($"Booster {boosterType} actif mis à jour : durée prolongée de {targetDuration} secondes, multiplicateur : {currentBooster.multiplicator}");

                WebSocketUtility.ChangeJsonTagValue(activateBoosterMessage, "boosterType", boosterType.ToString());
                WebSocketUtility.ChangeJsonTagValue(activateBoosterMessage, "multiplicator", targetMultiplicator.ToString());
                WebSocketUtility.ChangeJsonTagValue(activateBoosterMessage, "duration", targetDuration.ToString());

                if (OvcrServer.IsValid())
                {
                    try 
                    {
                        await OvcrServer.SendMessageAsync(activateBoosterMessage);
                        Log.Info($"Message d'activation du booster envoyé au serveur");
                    }
                    catch (Exception ex)
                    {
                        Log.Info($"Erreur lors de l'envoi du message d'activation : {ex.Message}");
                    }
                }
            }
            else
            {
                // Remettre le booster dans la liste des disponibles car multiplicateur différent
                availableBoosters[boosterType].Add(boosterToActivate);
                Log.Info($"Impossible d'activer le booster : un booster avec un multiplicateur différent ({currentBooster.multiplicator}x) est déjà actif");
            }
        }
        else
        {
            // Activer le booster localement
            activeBoosters[boosterType] = (targetDuration, targetMultiplicator);
            Log.Info($"Nouveau booster {boosterType} activé avec multiplicateur {targetMultiplicator}x pour {targetDuration} secondes");

            // Préparer et envoyer le message au serveur
            WebSocketUtility.ChangeJsonTagValue(activateBoosterMessage, "boosterType", boosterType.ToString());
            WebSocketUtility.ChangeJsonTagValue(activateBoosterMessage, "multiplicator", targetMultiplicator.ToString());
            WebSocketUtility.ChangeJsonTagValue(activateBoosterMessage, "duration", targetDuration.ToString());

            if (OvcrServer.IsValid())
            {
                try 
                {
                    await OvcrServer.SendMessageAsync(activateBoosterMessage);
                    Log.Info($"Message d'activation du booster envoyé au serveur");
                }
                catch (Exception ex)
                {
                    Log.Info($"Erreur lors de l'envoi du message d'activation : {ex.Message}");
                }
            }
        }
    }

	public void ActivateBoosterV1(Boosters boosterType)
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

    //debug, will give boosters through crates & calculated from server
    public void GiveBooster(Boosters boosterType, float duration, float multiplicator)
    {
        
        if (!availableBoosters.ContainsKey(boosterType))
            availableBoosters[boosterType] = new List<(float, float)>();

        availableBoosters[boosterType].Add((duration, multiplicator));

        if (OvcrServer.isAuth)
        {
            Log.Info("OvcrServer.isAuth");
            Log.Info($"Booster directement envoyé au serveur {boosterType} {duration} {multiplicator}");
            SendBoosterToServer(boosterType, duration, multiplicator);
        }
        else
        {
            // Stocker le booster pour l'envoyer plus tard
            pendingBoosters.Add((boosterType, duration, multiplicator));
            Log.Info($"Booster {boosterType} mis en attente jusqu'à l'authentification.");
        }

        Log.Info($"Booster {boosterType} ajouté à la liste des disponibles avec un multiplicateur de {multiplicator} pour une durée de {duration} secondes.");
    }
    
	public void AddAvailableBooster(Boosters boosterType, float duration, float multiplicator)
    {
        if (!availableBoosters.ContainsKey(boosterType))
            availableBoosters[boosterType] = new List<(float, float)>();

        availableBoosters[boosterType].Add((duration, multiplicator));
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
            Log.Info("Format de boosters invalide reçu");
    }

    public void SendActiveBoosters()
    {
        Log.Info("OvcrServer.IsValid()" + OvcrServer.IsValid());
        if (!IsProxy && OvcrServer.IsValid())
        {
            Log.Info("SendActiveboosters(multiplicators)");
			OvcrServer.SendMessage(updateActiveBoostersMessage);
        } else if (!IsProxy && !OvcrServer.IsValid())
        {
            Log.Info("SendActive, peut pas send les boosters");
        }
    }


    private async Task SendBoosterToServer(Boosters boosterType, float duration, float multiplicator)
    {
        WebSocketUtility.ChangeJsonTagValue(giveBoosterMessage, "boosterType", boosterType.ToString());
        WebSocketUtility.ChangeJsonTagValue(giveBoosterMessage, "duration", duration.ToString());
        WebSocketUtility.ChangeJsonTagValue(giveBoosterMessage, "multiplicator", multiplicator.ToString());

        //log parameters
        Log.Info($"BoosterType: {boosterType.ToString()}, Duration: {duration.ToString()}, Multiplicator: {multiplicator.ToString()}");
        
        await OvcrServer.SendMessageAsync(giveBoosterMessage);
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

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        Log.Info("togglemenu" + isMenuOpen);
    }

    public bool CanActivateBooster(Boosters boosterType, float multiplicator)
    {
        // Si un booster est déjà actif, vérifier que c'est le même multiplicateur
        if (activeBoosters.ContainsKey(boosterType))
        {
            return activeBoosters[boosterType].multiplicator == multiplicator;
        }
        
        // Si pas de booster actif, vérifier qu'on en a un disponible
        return availableBoosters.ContainsKey(boosterType) && 
            availableBoosters[boosterType].Any(b => b.multiplicator == multiplicator);
    }
}
