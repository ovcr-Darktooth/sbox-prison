using Sandbox;
using System.Collections.Generic;
using System.Text.Json;
namespace Overcreep;

public enum BlockType
{
    Invalid = -1,
	Normal = 0,
    VIP
}

public sealed class Backpack : Component
{
	[Property] public OvcrServer OvcrServer { get; set; } 
	[Property] public Enchantments Enchantments { get; set; } 
	[Property] public Currencies Currencies { get; set; } 
	public Dictionary<string, double> _inventory;
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

        /*if (!IsProxy && !hasLoaded && OvcrServer.isAuth && nextLoadInventory <= 0f)
        {
            Log.Info("Trying to load player backpack");
            GetDB();
            nextLoadInventory = 5f;
        }*/

        //todo: vendre le backpack, saveDB quand fini
    }

    private void SaveDB()
    {
        if (!IsProxy && hasLoaded)
        {
            // Log.Info("Saving to database");

            string jsonInventory = JsonSerializer.Serialize(_inventory);

            WebSocketUtility.ChangeJsonTagValue(saveInventoryMessage, "backpack", jsonInventory);

            if (OvcrServer.IsValid())
                OvcrServer.SendMessage(saveInventoryMessage);
        }
    }

    public void GetDB()
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
                BlockType blockType = GetBlockTypeFromString(blockProperty.Name);

                if (double.TryParse(blockProperty.Value.GetString(), out var quantity))
                    AddBlock(blockType, quantity);
                else
                    Log.Info($"Invalid quantity for block type '{blockType}': {blockProperty.Value}");
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
            WebSocketUtility.AddJsonTag(saveInventoryMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());
            WebSocketUtility.AddJsonTag(saveInventoryMessage, "backpack", "{}");

            getInventoryMessage.UseJsonTags = true;
            WebSocketUtility.AddJsonTag(getInventoryMessage, "action", "getBackpack");
            WebSocketUtility.AddJsonTag(getInventoryMessage, "playerId", GameObject.Network.Owner.SteamId.ToString());

            _inventory = new Dictionary<string, double>();

            //AddBlock(BlockType.Normal, 0);
            //AddBlock("Stone", 0);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public void AddBlock(BlockType blockType, double quantity)
    {
        if (!IsProxy && hasLoaded)
        {
            string blockDbName = GetBlockTypeTextSaveDB(blockType);
            if (_inventory.ContainsKey(blockDbName))
                _inventory[blockDbName] += quantity;
            else
                _inventory[blockDbName] = quantity;

            Log.Info($"{quantity} {blockDbName}(s) added to backpack. Total: {_inventory[blockDbName]}.");
        }
    }

    public void RemoveBlock(BlockType blockType, double quantity)
    {
        if (!IsProxy && hasLoaded) 
        {
            string blockDbName = GetBlockTypeTextSaveDB(blockType);
            if (_inventory.ContainsKey(blockDbName))
            {
                _inventory[blockDbName] -= quantity;
                if (_inventory[blockDbName] <= 0)
                {
                    _inventory.Remove(blockDbName);
                    Log.Info($"{blockDbName} removed from backpack.");
                }
                else
                    Log.Info($"{quantity} {blockDbName}(s) removed from backpack. Remaining: {_inventory[blockDbName]}.");
            }
        }
    }

    public double GetBlockCount(string blockType)
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


    public string GetBlockTypeTextSaveDB(BlockType blockValue)
	{
		switch (blockValue)
		{
			case BlockType.Normal:
				return "normal";
			default:
				return "invalid";
		}
	}

    public BlockType GetBlockTypeFromString(string blockName)
	{
		switch (blockName.ToLower())
		{
			case "dollar":
				return BlockType.Normal;
			default:
				return BlockType.Invalid;
		}
	}
}
