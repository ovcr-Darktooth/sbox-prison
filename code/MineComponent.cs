using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Sandbox;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.Sdf;
using System.Numerics;


public sealed class MineComponent : Component, Component.ITriggerListener
{

	[Property] public GameObject WorldPrefab { get; set; }
	[Property]
	public GameObject entityStart {get; set;}
	[Property]
	public GameObject entityEnd {get; set;}
	[Property] 
	public NameTagPanel Panel_Left { get; set; }
	[Property] 
	public NameTagPanel Panel_Right { get; set; }
	[Property] 
	public NameTagPanel Panel_Front { get; set; }
	[Property] 
	public NameTagPanel Panel_Back { get; set; }
	[Property]
	public int idMine;
	[Property]
	public Sdf3DVolume mineVolume { get; set; }
	public Sdf3DWorld mineWorld { get; set; }
	[Property]
	public int LevelMine { get; set; } = 1;
	public float ActualPercent {get;set;}
	public float ResetPercent { get; } = 40f;

	private int LastModificationCount = 0;
	//public List<MineBlockLines> MineBlockLines { get; set; }
	public string BlockMaterial { get; set; }
	//AxeX(Rouge)
	public int Longueur { get; set; }
	//AxeY(Vert)
	public int Largeur { get; set; }
	//AxeZ(Bleu)
	public int Hauteur { get; set; }

	private Dictionary<string, GameObject> playersInside = new Dictionary<string, GameObject>();
	private Dictionary<string, GameObject> playersAround = new Dictionary<string, GameObject>();

	private TimeSince timeSinceReset = 0;
	private TimeSince timeSincePercentage = 0;

	// Matrice de données représentant la forme
    private int[,] shapeMatrix = new int[,]
    {
        { 0, 1, 1, 1, 0 },
        { 0, 0, 0, 0, 0 },
        { 1, 1, 1, 1, 1 }
    };

    // Taille d'un bloc
    private float blockSizeF = 32f;
	private int blockSize = 32;

	private Vector3 gizmoTest = Vector3.Zero;

	protected override void OnAwake()
	{
		base.OnAwake();

	}

	protected override void OnStart()
	{		
		base.OnStart();	
		Network.DropOwnership();

		if(entityStart.IsValid() && entityEnd.IsValid())
		{
			//mineWorld = Scene.GetAllComponents<Sdf3DWorld>().FirstOrDefault(); //1 sdfworld
			//mineWorld = Scene.CreateObject(true)
			GameObject clone = WorldPrefab.Clone(new Transform(Vector3.Zero), name: "SdfWorld_" + idMine);
			//mineWorld = clone.Components.Get<Sdf3DWorld>();
			mineWorld = Scene.GetAllObjects(true).Where(go => go.Name.Equals("SdfWorld_"+idMine)).FirstOrDefault().Components.Get<Sdf3DWorld>();//.GetAllComponents<Sdf3DWorld>().FirstOrDefault();
			mineWorld.GameObject.NetworkSpawn();
			//clone.NetworkSpawn();
			//mineWorld.GameObject.Network.DropOwnership();
			Transform.Position = entityStart.Transform.Position 
											+ (Vector3.Backward * 16) 
											+ (Vector3.Down * 16)
											- (Vector3.Left * 16)
											+ (Vector3.Backward * (entityStart.Transform.Position.x - entityEnd.Transform.Position.x) );
		
			float differenceZ = entityEnd.Transform.Position.z - entityStart.Transform.Position.z;
			float differenceX = entityStart.Transform.Position.x - entityEnd.Transform.Position.x;
			float differenceY = entityEnd.Transform.Position.y - entityStart.Transform.Position.y;
			/*Log.Info( $"Dif Z :{differenceZ}" );
			Log.Info( $"Dif Z/32 (nb blocs technique) :{differenceZ / blockSize}" ); // compté 24
			Log.Info( $"Dif X (rouge) :{differenceX}" );
			Log.Info( $"Dif X/32 (nb blocs technique) :{differenceX / blockSize}" ); // compté 24
			Log.Info( $"Dif Y (vert) :{differenceY}" );
			Log.Info( $"Dif Y/32 (nb blocs technique) :{differenceY / blockSize}" ); // compté 24*/
			Hauteur = (int)differenceZ / blockSize + 1;
			Longueur = (int)differenceX / blockSize + 1; // on compte mal a cause de la position prise en compte
			Largeur = (int)differenceY / blockSize + 1; // pareil donc + 1
			Log.Info( $"Hauteur finale : {Hauteur}" );
			Log.Info( $"Longueur finale : {Longueur}" );
			Log.Info( $"Largeur finale : {Largeur}" );
			Log.Info( $"NbBlocs:  {Hauteur * Longueur * Largeur}");

			if (idMine != 0)
				_ = regenMine();


			Components.GetInChildren<BoxCollider>().Transform.Position = (entityStart.Transform.Position + entityEnd.Transform.Position) / 2;
			Components.GetInChildren<BoxCollider>().Scale = (Hauteur + Longueur + Largeur) * 26.5f;
			//TODO: refaire un autre colliderbox trigger pour vraiment les tp, et utiliser celui la pour donner le status du world a ceux qui sont a cotés
			Components.GetInChildren<BoxCollider>().OnTriggerEnter = this.OnAroundMineTriggerEnter;
			Components.GetInChildren<BoxCollider>().OnTriggerExit = this.OnAroundMineTriggerExit;

			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().Transform.Position = (entityStart.Transform.Position + entityEnd.Transform.Position) / 2;
			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().Scale = new Vector3(Longueur*32f,Largeur*32f,Hauteur*32f);
			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().OnTriggerEnter = this.OnInsideMineTriggerEnter;
			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().OnTriggerExit = this.OnInsideMineTriggerExit;

			int hauteurPanel = 125;
			Panel_Left.Transform.Position =  new Vector3(Transform.Position.x + Longueur*blockSize/2, Transform.Position.y, entityEnd.Transform.Position.z + hauteurPanel);
			Panel_Right.Transform.Position = new Vector3(Transform.Position.x + Longueur*blockSize/2, Transform.Position.y + Largeur*blockSize, entityEnd.Transform.Position.z + hauteurPanel);
			Panel_Front.Transform.Position = new Vector3(Transform.Position.x, Transform.Position.y + Largeur*blockSize/ 2, entityEnd.Transform.Position.z + hauteurPanel);
			Panel_Back.Transform.Position =  new Vector3(Transform.Position.x + Longueur*blockSize, Transform.Position.y + Largeur*blockSize/ 2, entityEnd.Transform.Position.z + hauteurPanel);


			Panel_Left.Level = LevelMine.ToString();
			Panel_Right.Level = LevelMine.ToString();
			Panel_Front.Level = LevelMine.ToString();
			Panel_Back.Level = LevelMine.ToString();

			/*Panel_Left.Percentage = ActualPercent;
			Panel_Right.Percentage = ActualPercent;
			Panel_Front.Percentage = ActualPercent;
			Panel_Back.Percentage = ActualPercent;*/

			timeSinceReset = 0;
			timeSincePercentage = 0;
		}
	}

	public async Task AddCube(Vector3 pos)
	{
		var cube = new BoxSdf3D(Vector3.Zero, blockSizeF, 0f).Transform(pos);
		await mineWorld.AddAsync(cube, mineVolume);
	}

	[Broadcast]
	public void RemoveCube(Vector3 pos)
	{
		if (!IsProxy)
		{
			//mineWorld.Network.TakeOwnership();
			var cube = new BoxSdf3D(Vector3.Zero, 32f, 0f).Transform(pos);
			mineWorld.SubtractAsync(cube, mineVolume);
		}
	}

	[Broadcast]
	public void RemoveLayer(Vector3 pos)
	{
		if (!IsProxy)
		{
			//var cube = new BoxSdf3D(Vector3.Zero, 32f, 0f).Transform(pos);
			Vector3 layerPos = new Vector3(entityEnd.Transform.Position.x, entityStart.Transform.Position.y, pos.z);
			var cube = new BoxSdf3D(Vector3.Zero, 32f*Longueur*Largeur, 0f).Transform(layerPos);
			mineWorld.SubtractAsync(cube, mineVolume);
		}
	}



	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(entityStart.IsValid() && entityEnd.IsValid())
		{			
			var posTrace = Scene.Trace.Ray( entityStart.Transform.Position, entityEnd.Transform.Position ).Run();
			Gizmo.Draw.Color = Color.Red;
  			Gizmo.Draw.LineThickness = 3;
			Gizmo.Draw.Line(posTrace.StartPosition, posTrace.EndPosition);
			
			if (timeSinceReset > 300f) // 60 * 5 = 300
			{
				resetMine();
				timeSinceReset = 0;
			}

			if (timeSincePercentage > 3f)
			{
				updateMinePercentage();
				timeSincePercentage = 0;
			}
			//mineWorld.GameObject.Network.DropOwnership();
		}
		/*else 
		{
			Log.Info("pas valide");
		}*/

		if (gizmoTest != Vector3.Zero)
		{
			Gizmo.Draw.Color = Color.Green;
  			Gizmo.Draw.LineThickness = 3;
			Gizmo.Draw.LineBBox(BBox.FromPositionAndSize(gizmoTest, blockSizeF));
		}


	}

	void OnAroundMineTriggerEnter(Collider collider)
	{
		if (!playersAround.ContainsKey(collider.GameObject.Name) && collider.GameObject.Tags.Has("player"))
		{
			Log.Info("Enter");
			Log.Info(collider.GameObject.Name);
			playersAround.Add(collider.GameObject.Name,collider.GameObject);
			mineWorld.Enabled = true;
			
			Panel_Left.nbPlayers = playersAround.Count;
			Panel_Right.nbPlayers = playersAround.Count;
			Panel_Front.nbPlayers = playersAround.Count;
			Panel_Back.nbPlayers = playersAround.Count;
		}
	}

	void OnAroundMineTriggerExit(Collider collider)
	{
		if (playersAround.ContainsKey(collider.GameObject.Name))
		{
			Log.Info("Exit");
			Log.Info(collider.GameObject.Name);
			playersAround.Remove(collider.GameObject.Name);
			mineWorld.Enabled = false;

			Panel_Left.nbPlayers = playersAround.Count;
			Panel_Right.nbPlayers = playersAround.Count;
			Panel_Front.nbPlayers = playersAround.Count;
			Panel_Back.nbPlayers = playersAround.Count;
		}
	}

	void OnInsideMineTriggerEnter(Collider collider)
	{
		if (!playersInside.ContainsKey(collider.GameObject.Name) && collider.GameObject.Tags.Has("player"))
		{
			/*Log.Info("Enter");
			Log.Info(collider.GameObject.Name);*/
			playersInside.Add(collider.GameObject.Name,collider.GameObject);
		}
	}

	void OnInsideMineTriggerExit(Collider collider)
	{
		if (playersInside.ContainsKey(collider.GameObject.Name))
		{
			/*Log.Info("Exit");
			Log.Info(collider.GameObject.Name);*/
			playersInside.Remove(collider.GameObject.Name);
		}
	}

	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//------------------------Mine methods-------------------------

	public void initMine()
	{

	}

	public async Task regenMine()
	{
		if (idMine != 0)
		{
			var cube = new BoxSdf3D(Vector3.Zero, new Vector3(Longueur*blockSizeF,Largeur*blockSizeF,Hauteur*blockSizeF), 0f).Transform(Transform.Position);
			await mineWorld.AddAsync(cube, mineVolume);
		}
	}

	public void resetMine()
	{
		Log.Info("ResetMine !");
		//TP players
		teleportPlayers();

		//Regen mine
		_ = regenMine();			

		LastModificationCount = mineWorld.ModificationCount;
		ActualPercent = 100f;
	}

	//[Broadcast]
	public void teleportPlayers()
	{		
		foreach (KeyValuePair<string, GameObject> entry in playersInside)
		{
			if (!IsProxy && !entry.Value.Network.IsProxy)
			{
				//Log.Info(entry.Value.Transform.Position);
				entry.Value.Transform.Position = new Vector3(entry.Value.Transform.Position.x,entry.Value.Transform.Position.y, entityEnd.Transform.Position.z + 450);
				//Log.Info(entry.Value.Transform.Position);
			}				
		}		
	}

	public void updateMinePercentage() 
	{
		if (mineWorld.IsValid())
		{
			ActualPercent = mineWorld.ModificationCount - LastModificationCount;
			ActualPercent = 100f - (ActualPercent / (Hauteur * Longueur * Largeur) * 100f);
			//Log.Info($"[Mine:{idMine}] % of blocks remaining: {ActualPercent}");

			if (ActualPercent < ResetPercent)	
				resetMine();

			Panel_Left.Percentage = ActualPercent;
			Panel_Right.Percentage = ActualPercent;
			Panel_Front.Percentage = ActualPercent;
			Panel_Back.Percentage = ActualPercent;
		}
	}
}


/*
	/*for ( int i = 0; i < Hauteur; i++ )
			{
				for ( int j = 0; j < Longueur ; j++ )
				{
					for ( int k = 0; k < Largeur; k++ )
					{
						if (mineWorld.IsValid())
							_ = AddCube(Transform.Position
												+ (Vector3.Up * blockSize * i) 
												+ (Vector3.Forward * blockSize * j) 
												+ (Vector3.Left * blockSize * k));
						/*if (j == 1 || i == 1 || k == 1)
							_ = RemoveCube(Vector3.Zero
												+ (Vector3.Up * blockSize * i) 
												+ (Vector3.Backward * blockSize * j) 
												+ (Vector3.Right * blockSize * k));* /
					}
				}
			}* /
*/