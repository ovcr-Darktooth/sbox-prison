using Sandbox;
using System;
using System.Text.Json;

public sealed class OvcrServer : Component
{
	private WebsocketTools Websocket;
	private TimeUntil nextAuth = 2f;
	public bool isAuth = false;
	public bool hasLoadError = false;
	private WebsocketMessage authMessage {get;set;} = new();
	[Property] public Currencies Currencies { get; set; }
	[Property] public Enchantments Enchantments { get; set; }
	[Property] public Multiplicators Multiplicators { get; set; } 
	[Property] public Backpack Backpack { get; set; } 

	
	protected override void OnUpdate()
	{
		if (!IsProxy && !isAuth && nextAuth <= 0f)
		{
			Log.Info("Trying to auth");
			Websocket.message = authMessage;

			WSAuth();
			nextAuth = 3f;
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
			WebSocketUtility.AddJsonTag(authMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());
			WebSocketUtility.AddJsonTag(authMessage, "token", authToken);

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

	public async void SendMessage(WebsocketMessage message)
	{
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
