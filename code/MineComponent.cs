using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Sandbox;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.Sdf;


public sealed class MineComponent : Component, Component.ITriggerListener
{

	[Property] public GameObject WorldPrefab { get; set; }
	[Property]
	public GameObject entityStart {get; set;}
	[Property]
	public GameObject entityEnd {get; set;}
	[Property]
	public int idMine;
	[Property]
	public Sdf3DVolume mineVolume { get; set; }
	//[Property]
	public Sdf3DWorld mineWorld { get; set; }

	public int LevelMine { get; set; }
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

	private TimeSince timeSinceReset = 0;
	private TimeSince timeSincePercentage = 0;

	private TimeUntil timeUntilResetAvailable = 10f;

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
			{
				for ( int i = 0; i < Hauteur; i++ )
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
													+ (Vector3.Right * blockSize * k));*/
						}
					}
				}
			}

			Components.GetInChildren<BoxCollider>().Transform.Position = (entityStart.Transform.Position + entityEnd.Transform.Position) / 2;
			Components.GetInChildren<BoxCollider>().Scale = (Hauteur + Longueur + Largeur) * 26.5f;

			Components.GetInChildren<BoxCollider>().OnTriggerEnter = this.OnInsideMineTriggerEnter;
			Components.GetInChildren<BoxCollider>().OnTriggerExit = this.OnInsideMineTriggerExit;

			timeSinceReset = 0;
			timeSincePercentage = 0;
			timeUntilResetAvailable = 10f;
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

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(entityStart.IsValid() && entityEnd.IsValid())
		{			
			var posTrace = Scene.Trace.Ray( entityStart.Transform.Position, entityEnd.Transform.Position ).Run();
			Gizmo.Draw.Color = Color.Red;
  			Gizmo.Draw.LineThickness = 3;
			Gizmo.Draw.Line(posTrace.StartPosition, posTrace.EndPosition);
			
			if (timeSinceReset > 300f)
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
			Log.Info(timeUntilResetAvailable);
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

	public void resetMine()
	{

		if (timeUntilResetAvailable <= 0)
		{
			Log.Info("ResetMine !");
			//TP players
			teleportPlayers();

			//Regen mine
			for ( int i = 0; i < Hauteur; i++ )
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
												+ (Vector3.Right * blockSize * k));*/
					}
				}
			}

			LastModificationCount = mineWorld.ModificationCount;
			ActualPercent = 100f;
			timeUntilResetAvailable = 10f;
		}
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
			Log.Info($"[Mine:{idMine}] % of blocks remaining: {ActualPercent}");

			if (ActualPercent < ResetPercent)	
				resetMine();
		}
	}

}