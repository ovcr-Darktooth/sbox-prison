using Sandbox;
using System;
using System.Text.Json;
using System.Collections.Generic;


public class ActiveBoosterDTO
{
    public float TimeRemaining { get; set; }
    public float Multiplicator { get; set; }
}

public sealed class OvcrServer : Component
{
	private WebsocketTools Websocket;
	private TimeUntil nextAuth = 2f;
	private TimeUntil nextActiveBoosterCopy = 1f;
	public bool isAuth = false;
	public bool hasLoadError = false;
	private WebsocketMessage authMessage {get;set;} = new();
	private WebsocketMessage updateActiveBoostersMessage {get;set;} = new();
	[Property] public Currencies Currencies { get; set; }
	[Property] public Enchantments Enchantments { get; set; }
	[Property] public Multiplicators Multiplicators { get; set; } 
	[Property] public Backpack Backpack { get; set; } 

	private Dictionary<Boosters, (TimeUntil timeUntilExpiration, float multiplicator)> activeBoostersCopy = new();

	
	protected override void OnUpdate()
	{
		if (!IsProxy && !isAuth && nextAuth <= 0f)
		{
			Log.Info("Trying to auth");
			Websocket.message = authMessage;

			WSAuth();
			nextAuth = 3f;
		}

		if (!IsProxy && isAuth && nextActiveBoosterCopy <= 0f)
		{
			//Log.Info("Copying active boosters");

			activeBoostersCopy = Multiplicators.activeBoosters;
			

			nextActiveBoosterCopy = 1f;
		}
		
	}

	protected override async void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			Websocket = new WebsocketTools();
			Websocket.url = "ws://websocket.overcreep.ovh:10706";
			//Websocket.url = "ws://localhost:8080";

			var authToken = await Sandbox.Services.Auth.GetToken( "auth" );

			authMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(authMessage, "action", "auth");
			WebSocketUtility.AddJsonTag(authMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());
			WebSocketUtility.AddJsonTag(authMessage, "token", authToken);


			updateActiveBoostersMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "action", "updateActiveBoosters");
            WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "activeBoosters", "{}");
			WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());

			Websocket.onMessageReceived = OnWSMessageReceived;
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
					Log.Info($"[OvcrServer]WSAuth, error on websocket: {ex.Message}");
				}
			} );
		}
	}
	private void DisplayBoosters()
	{ 
		Log.Info("=== Active Boosters ===");

        if (activeBoostersCopy.Count == 0)
        {
            Log.Info("No active boosters.");
        }
        else
        {
            foreach (var booster in activeBoostersCopy)
            {
                var boosterType = booster.Key;
                var (timeUntilExpiration, multiplicator) = booster.Value;
                Log.Info($"{boosterType}: {multiplicator}x multiplier, Time left: {timeUntilExpiration.Relative:F2} seconds");
            }
        }


        Log.Info("======================");
	}

	public async void SendMessage(WebsocketMessage message)
	{
		//Log.Info("Tentative d'envoi du message"); 
		if (!IsProxy && isAuth && !hasLoadError)
		{
			Websocket.message = message;
			await Task.RunInThreadAsync( async () =>
			{
				try
				{
					await WebSocketUtility.SendAsync( Websocket );
				}
				catch ( Exception ex )
				{
					hasLoadError = true;
					Log.Info($"[OvcrServer]WSAuth, error on websocket: {ex.Message}");
				}
			} );
		}
		else 
		{
			Log.Info("Message pas envoyé");
		}
	}

	protected override void OnDisabled()
	{
		Log.Info("OnDisabled ovcrserver");
		if (!IsProxy)
		{
			Log.Info("ovcrserver SendActiveboosters");
			string jsonActiveBoosters = ConvertActiveBoostersToJson();

			DisplayBoosters();

			WebSocketUtility.ChangeJsonTagValue(updateActiveBoostersMessage, "activeBoosters", jsonActiveBoosters);

			Log.Info("json envoyé:" + jsonActiveBoosters);

			SendMessage(updateActiveBoostersMessage);
		}
		base.OnDisabled();
	}

	public string ConvertActiveBoostersToJson()
	{
		// Créer un dictionnaire temporaire pour stocker les boosters sous forme sérialisable
		Dictionary<string, ActiveBoosterDTO> boostersToSerialize = new();

		// Boucler sur les boosters actifs et les convertir en DTO
		foreach (var booster in activeBoostersCopy)
		{
			var boosterType = booster.Key.ToString();
			var timeRemaining = booster.Value.timeUntilExpiration.Relative;  // Durée restante
			var multiplicator = booster.Value.multiplicator;

			// Ajouter au dictionnaire sérialisable
			boostersToSerialize[boosterType] = new ActiveBoosterDTO
			{
				TimeRemaining = timeRemaining,
				Multiplicator = multiplicator
			};
		}

		// Sérialiser en JSON
		string json = JsonSerializer.Serialize(boostersToSerialize);

		return json;
	}

	private void OnWSMessageReceived(string message)
    {
        Log.Info("Server responded: " + message);

		try
        {
            using (JsonDocument doc = JsonDocument.Parse(message))
            {
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("action", out JsonElement actionElement))
                {
                    string action = actionElement.GetString();

                    switch (action)
                    {
						case "auth":
							isAuth = true;
							break;
                        case "balanceUpdated":
                            break;
                        case "getBalances":
                            if (root.TryGetProperty("balances", out JsonElement balancesElement) && Currencies.IsValid())
                            {
								Currencies.hasLoaded = true;
								Currencies.hasLoadError = false;
                                Currencies.LoadBalances(balancesElement);
                            }
                            else
                                Log.Info("Balances property is missing");
                            break;
						case "enchantsUpdated":
                            break;
                        case "getEnchants":
                            if (root.TryGetProperty("enchants", out JsonElement enchantsElement) && Enchantments.IsValid())
                            {
                                Enchantments.hasLoaded = true;
								Enchantments.hasLoadError = false;
								Enchantments.LoadEnchants(enchantsElement);
                            }
                            else
                                Log.Info("Enchants property is missing");
                            break;
						case "boostersUpdated":
							break;
						case "getBoosters":
							if (root.TryGetProperty("boosters", out JsonElement boostersElement) && Multiplicators.IsValid())
                            {
								Multiplicators.hasLoaded = true;
								Multiplicators.hasLoadError = false;
                                Multiplicators.LoadBoosters(boostersElement);
                            }
                            else
                                Log.Info("Boosters property is missing");
							break;
						case "giveBooster":
							break;
						case "backpackUpdated":
							break;						
						case "getBackpack":
							if (root.TryGetProperty("backpack", out JsonElement backpackElement) && Backpack.IsValid())
                            {
								Backpack.hasLoaded = true;
								Backpack.hasLoadError = false;
                                Backpack.LoadInventory(backpackElement);
                            }
                            else
                                Log.Info("Backpack property is missing");
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
}
