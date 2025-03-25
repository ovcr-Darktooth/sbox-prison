using Sandbox;
using System;
using XMovement;
namespace Overcreep;

public partial class OvcrPlayerControllerX : PlayerWalkControllerComplex
{
	public UInt64 SteamId { get; private set; } = 0;
	[Property] public Currencies Currencies { get; set; }
	[Property] public Enchantments Enchantments { get; set; }
	[Property] public Multiplicators Multiplicators { get; set; } 

	public void SetSteamId(ulong steamId)
    {
        // Assure-toi que le Steam ID n'est défini qu'une seule fois
        if (SteamId == 0)
        {
            SteamId = steamId;
			Log.Info($"Steamid {GameObject.Name} : {SteamId}");
        }
    }

	protected override void OnStart()
	{
		if ( !IsProxy )
			SetSteamId(GameObject.Network.Owner.SteamId);	
			
		base.OnStart();
	}
}
