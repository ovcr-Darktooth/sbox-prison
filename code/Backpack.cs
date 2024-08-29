using Sandbox;
using System.Collections.Generic;
using System.Text.Json;

public sealed class Backpack : Component
{
	[Property] public OvcrServer OvcrServer { get; set; } 
	[Property] public Enchantments Enchantments { get; set; } 
	[Property] public Currencies Currencies { get; set; } 
	public Dictionary<string, int> _inventory;
    private TimeUntil nextSaveDB = 5f;
    private TimeUntil nextLoadInventory = 3f;
    public bool hasLoaded = false;
    public bool hasLoadError = false;

    private WebsocketMessage saveInventoryMessage { get; set; } = new();
    private WebsocketMessage getInventoryMessage { get; set; } = new();

    protected override void OnUpdate()
    {
        if (!IsProxy && hasLoaded && OvcrServer.isAuth && nextSaveDB <= 0f)
        {
            // DisplayInventory();
            SaveDB();
            nextSaveDB = 5f;
        }

        if (!IsProxy && !hasLoaded && OvcrServer.isAuth && nextLoadInventory <= 0f)
        {
            Log.Info("Trying to load player inventory");
            GetDB();
            nextLoadInventory = 5f;
        }
    }

    private void SaveDB()
    {
        if (!IsProxy && hasLoaded)
        {
            // Log.Info("Saving to database");

            string jsonInventory = JsonSerializer.Serialize(_inventory);

            WebSocketUtility.ChangeJsonTagValue(saveInventoryMessage, "inventory", jsonInventory);

            if (OvcrServer.IsValid())
                OvcrServer.SendMessage(saveInventoryMessage);
        }
    }

    private void GetDB()
    {
        if (!IsProxy && OvcrServer.IsValid())
            OvcrServer.SendMessage(getInventoryMessage);
    }

    public void LoadInventory(JsonElement inventory)
    {
        if (inventory.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty blockProperty in inventory.EnumerateObject())
            {
                string blockType = blockProperty.Name;

                if (int.TryParse(blockProperty.Value.GetString(), out var quantity))
                {
                    AddBlock(blockType, quantity);
                }
                else
                {
                    Log.Info($"Invalid quantity for block type '{blockType}': {blockProperty.Value}");
                }
            }
        }
        else
        {
            Log.Info("Invalid inventory format received");
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        if (!IsProxy)
        {
            saveInventoryMessage.UseJsonTags = true;
            WebSocketUtility.AddJsonTag(saveInventoryMessage, "action", "updateBackpack");
            WebSocketUtility.AddJsonTag(saveInventoryMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());
            WebSocketUtility.AddJsonTag(saveInventoryMessage, "backpack", "{}");

            getInventoryMessage.UseJsonTags = true;
            WebSocketUtility.AddJsonTag(getInventoryMessage, "action", "getBackpack");
            WebSocketUtility.AddJsonTag(getInventoryMessage, "playerId", GameObject.Network.OwnerConnection.SteamId.ToString());

            _inventory = new Dictionary<string, int>();

            AddBlock("normalBlocks", 0);
            //AddBlock("Stone", 0);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public void AddBlock(string blockType, int quantity)
    {
        if (!IsProxy)
        {
            if (_inventory.ContainsKey(blockType))
                _inventory[blockType] += quantity;
            else
                _inventory[blockType] = quantity;

            Log.Info($"{quantity} {blockType}(s) added to backpack. Total: {_inventory[blockType]}.");
        }
    }

    public void RemoveBlock(string blockType, int quantity)
    {
        if (!IsProxy && _inventory.ContainsKey(blockType))
        {
            _inventory[blockType] -= quantity;
            if (_inventory[blockType] <= 0)
            {
                _inventory.Remove(blockType);
                Log.Info($"{blockType} removed from backpack.");
            }
            else
            {
                Log.Info($"{quantity} {blockType}(s) removed from backpack. Remaining: {_inventory[blockType]}.");
            }
        }
    }

    public int GetBlockCount(string blockType)
    {
        if (_inventory.ContainsKey(blockType))
        {
            return _inventory[blockType];
        }
        return 0;
    }

    private void DisplayInventory()
    {
        foreach (var item in _inventory)
        {
            Log.Info($"Block Type: {item.Key}, Quantity: {item.Value}");
        }
    }
}
