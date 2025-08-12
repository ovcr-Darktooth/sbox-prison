using Sandbox;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Overcreep;


public class ActiveBoosterDTO
{
    public float TimeRemaining { get; set; }
    public float Multiplicator { get; set; }
}

public sealed class OvcrServer : Component
{
	private WebsocketTools Websocket;
	public WebSocket Socket { get; set; }
	public string ConnectionUri { get; set; }
	private TimeUntil nextAuth = 1f;
	private TimeUntil nextActiveBoosterCopy = 1f;
	private TimeUntil nextLoadEnchants = 0f;
	private TimeUntil nextLoadCurrencies = 1f;
	private TimeUntil nextLoadMultiplicators = 2f;
	private TimeUntil nextLoadInventory = 3f;
	public bool isAuth = false;
	public bool hasLoadError = false;
	private string authToken = "";
	private WebsocketMessage authMessage {get;set;} = new();
	private WebsocketMessage updateActiveBoostersMessage {get;set;} = new();
	[Property] public Currencies Currencies { get; set; }
	[Property] public Enchantments Enchantments { get; set; }
	[Property] public Multiplicators Multiplicators { get; set; } 
	[Property] public Backpack Backpack { get; set; } 

	private Dictionary<Boosters, (TimeUntil timeUntilExpiration, float multiplicator)> activeBoostersCopy = new();

	
	protected override void OnUpdate()
	{
		if (!IsProxy && !isAuth && authToken != "" && nextAuth <= 0f)
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

		//Now loading everything
		if (!IsProxy && !Enchantments.hasLoaded && isAuth && nextLoadEnchants <= 0f)
		{ 
			Log.Info("Trying to load player enchants");
			Enchantments.GetDB();
			nextLoadEnchants = 5f;
		}

		if (!IsProxy && Enchantments.hasLoaded && !Currencies.hasLoaded && isAuth && nextLoadCurrencies <= 0f)
		{
			Log.Info("Trying to load player currencies");
			Currencies.GetDB();
			nextLoadCurrencies = 5f;
		}

		if (!IsProxy && Currencies.hasLoaded && !Multiplicators.hasLoaded && isAuth && nextLoadMultiplicators <= 0f)
		{
			Log.Info("Trying to load player boosters");
			//Websocket.message = getCurrenciesMessage;

			Multiplicators.GetDB();

			//DEBUG:
            //AddActiveBooster(Boosters.Dollars, 10f, 2f);
            //AddActiveBooster(Boosters.EToken, 12f, 2f);

            //Multiplicators.GiveBooster(Boosters.Dollars, 50f, 2f);
            //GiveBooster(Boosters.Dollars, 60f, 2f);
            //Multiplicators.GiveBooster(Boosters.EToken, 70f, 2f);
            //GiveBooster(Boosters.EToken, 80f, 2f);
            //Multiplicators.hasLoaded = true;
            //END OF DEBUG  

			nextLoadMultiplicators = 5f;
		}


		if (!IsProxy && Multiplicators.hasLoaded && !Backpack.hasLoaded && isAuth && nextLoadInventory <= 0f)
        {
            Log.Info("Trying to load player backpack");
            Backpack.GetDB();
            nextLoadInventory = 5f;
        }
	}

	protected override async void OnStart()
	{
		base.OnStart();
		if (!IsProxy)
		{
			Websocket = new WebsocketTools();
			Websocket.url = "ws://websocket.overcreep.ovh:10476";
			//Websocket.url = "ws://localhost:8080";

			authMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(authMessage, "action", "auth");
			WebSocketUtility.AddJsonTag(authMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());
			WebSocketUtility.AddJsonTag(authMessage, "token", "");


			updateActiveBoostersMessage.UseJsonTags = true;
			WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "action", "updateActiveBoosters");
            WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "activeBoosters", "{}");
			WebSocketUtility.AddJsonTag(updateActiveBoostersMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());

			Websocket.onMessageReceived = OnWSMessageReceived;


			authToken = await Sandbox.Services.Auth.GetToken( "auth" );

			WebSocketUtility.AddJsonTag(authMessage, "token", authToken);
		}
	}

	//Lors de la déconnexion du joueur
	protected override void OnDisabled()
	{
		if (!IsProxy)
		{
			string jsonActiveBoosters = ConvertActiveBoostersToJson();
			DisplayBoosters();
			WebSocketUtility.ChangeJsonTagValue(updateActiveBoostersMessage, "activeBoosters", jsonActiveBoosters);
			SendMessage(updateActiveBoostersMessage);

			Backpack.SaveDB();
			Enchantments.SaveDB();
			//TODO: ajouter les autres trucs a sauvegarder

		}
		base.OnDisabled();
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
		if (!IsProxy && isAuth && !hasLoadError)
		{
			var clonedWs = new WebsocketTools
			{
				url = Websocket.url,
				onMessageReceived = Websocket.onMessageReceived,
				webSocket = Websocket.webSocket,
				isConnected = Websocket.isConnected,
				isSubscribed = Websocket.isSubscribed,
				message = CloneMessage(message)
			};

			await Task.RunInThreadAsync(async () =>
			{
				try
				{
					await WebSocketUtility.SendAsync(clonedWs);
				}
				catch (Exception ex)
				{
					hasLoadError = true;
					Log.Info($"[OvcrServer]SendMessage error: {ex.Message}");
				}
			});

			/*Websocket.message = message;
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
			} );*/
		}
		else 
		{
			Log.Info("Message pas envoyé");
		}
	}

	public async Task SendMessageAsync(WebsocketMessage message)
	{
		if (!IsProxy && isAuth && !hasLoadError)
		{
			var localWs = new WebsocketTools
			{
				url = Websocket.url,
				onMessageReceived = Websocket.onMessageReceived,
				webSocket = Websocket.webSocket,
				isConnected = Websocket.isConnected,
				isSubscribed = Websocket.isSubscribed,
				message = CloneMessage(message) // copie profonde
			};

			try
			{
				await WebSocketUtility.SendAsync(localWs);
			}
			catch (Exception ex)
			{
				hasLoadError = true;
				Log.Info($"[OvcrServer]SendMessageAsync error: {ex.Message}");
			}

			/*Websocket.message = message;
			try
			{
				await WebSocketUtility.SendAsync(Websocket);
			}
			catch (Exception ex)
			{
				hasLoadError = true;
				Log.Info($"[OvcrServer]SendMessageAsync, erreur sur websocket: {ex.Message}");
			}*/
		}
		else 
		{
			Log.Info("Message pas envoyé");
		}
	}

	private WebsocketMessage CloneMessage(WebsocketMessage original)
	{
		if (original == null)
			return null;

		var clone = new WebsocketMessage
		{
			UseJsonTags = original.UseJsonTags,
			message = original.message
		};

		if (original.jsonTags != null)
		{
			clone.jsonTags = original.jsonTags
				.Select(tag => new JsonTags
				{
					tag = tag.tag,
					value = tag.value
				})
				.ToList();
		}

		return clone;
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
						case "useBooster":
							break;
						case "updateActiveBoosters":
							//Do nothing
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
