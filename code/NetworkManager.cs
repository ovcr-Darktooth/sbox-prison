using System;
using System.Linq;
using Sandbox;
using System.Collections.Generic;
using Sandbox.Network;

namespace Sandbox;

[Group( "Arena" )]
[Title( "Network Manager")]
public class NetworkManager : Component, Component.INetworkListener
{
	[Property] public PrefabScene PlayerPrefab { get; set; }
	[Property] public GameObject MinePrefab { get; set; }
	private bool areMinesInit = false;

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}
		
		base.OnStart();
	}

	void INetworkListener.OnActive( Connection connection )
	{

		Log.Info("aremineinit:"+ areMinesInit);
		

		if (!areMinesInit)
			initMines(connection);

		var player = PlayerPrefab.Clone();
		player.Name = connection.DisplayName;
		player.NetworkSpawn( connection );
	}


	void initMines(Connection channel)
	{
		Log.Info("Initialisation mines");
		var lesMinesPhysics = Scene.GetAllObjects( true ).Where( go => go.Name.StartsWith("mine_physics"));
		
		List<int> lesMinesAInit = new List<int>(); 

		foreach ( GameObject preMinePhysics in lesMinesPhysics )
		{
			var splitted = preMinePhysics.Name.Split("_");

			
			if (splitted.Length == 4) 
			{
				int idMine = int.Parse(splitted[3]);
				if (!lesMinesAInit.Contains(idMine))
				{
					lesMinesAInit.Add(idMine);	
				}
			}
		}

		Log.Info("count ainit:"+ lesMinesAInit.Count());
		foreach (int initMine in lesMinesAInit)
		{
			if (initMine != 0)
			{
				Log.Info(lesMinesPhysics.Where(go => go.Name.Equals("mine_physics_debut_" + initMine)).SingleOrDefault());
				var entityStart = lesMinesPhysics.Where(go => go.Name.Equals("mine_physics_debut_" + initMine)).SingleOrDefault();
				var entityEnd = lesMinesPhysics.Where(go => go.Name.Equals("mine_physics_fin_" + initMine)).SingleOrDefault();

				GameObject mine = MinePrefab.Clone(entityStart.Transform.World, name: "Mine_" + initMine);
				mine.Components.Get<MineComponent>().entityStart = entityStart;
				mine.Components.Get<MineComponent>().entityEnd = entityEnd;
				mine.Components.Get<MineComponent>().idMine = initMine;

				//mine.Transform.Position = (entityStart.Transform.Position + entityEnd.Transform.Position) / 2;
				//mine.Transform.Position = entityStart.Transform.Position;
				//mine.Components.GetInChildren<BoxCollider>().Transform.Position = (entityStart.Transform.Position + entityEnd.Transform.Position) / 2;
				
				
				mine.NetworkSpawn();
				//mine.Network.DropOwnership();
			}
			
		}

		areMinesInit = true;
	}
}
