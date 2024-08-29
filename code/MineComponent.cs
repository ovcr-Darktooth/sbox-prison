using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Sandbox;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.Sdf;
using System.Numerics;
using Facepunch.Arena;


public sealed class MineComponent : Component, Component.ITriggerListener
{

	[Property] public GameObject WorldPrefab { get; set; }
	[HostSync]
	public GameObject entityStart {get; set;}
	[HostSync]
	public GameObject entityEnd {get; set;}
	[HostSync]
	public Vector3 entityStartPosition {get; set;}
	[HostSync]
	public Vector3 entityEndPosition {get; set;}
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
	public float blocsRestants {get;set;}

	//public List<MineBlockLines> MineBlockLines { get; set; }
	public string BlockMaterial { get; set; }
	//AxeX(Rouge)
	[HostSync]
	public int Longueur { get; set; }
	//AxeY(Vert)
	[HostSync]
	public int Largeur { get; set; }
	//AxeZ(Bleu)
	[HostSync]
	public int Hauteur { get; set; }

	private Dictionary<string, GameObject> playersInside = new Dictionary<string, GameObject>();
	private Dictionary<string, GameObject> playersAround = new Dictionary<string, GameObject>();

	private TimeSince timeSinceReset = 0;
	private TimeSince timeSincePercentage = 0;

	// Matrice de données représentant la forme (dans un seul tableau vertical), parcourir avec longeur / hauteur (voir calcul déja effectué)
	private int[] _flattenedShapeMatrix;

	[HostSync(Query = true)]
	public int[] FlattenedShapeMatrix
	{
		get => _flattenedShapeMatrix;
		set 
		{
			_flattenedShapeMatrix = value;
			_shapeMatrix = UnflattenMatrix(_flattenedShapeMatrix, Longueur, Largeur, Hauteur);
		}
	}

	private int[,,] _shapeMatrix;

	public int[,,] ShapeMatrix
	{
		get => _shapeMatrix;
		set
		{
			_shapeMatrix = value;
			FlattenedShapeMatrix = FlattenMatrix(_shapeMatrix);
		}
	}

	void SetShapeMatrix(int[,,] val)
	{
		_shapeMatrix = val;
		FlattenedShapeMatrix = FlattenMatrix(_shapeMatrix);
	}

    // Taille d'un bloc
    private float blockSizeF = 32f;
	private int blockSize = 32;

	private float blockSizeV = 50f;

	private Vector3 gizmoTest = Vector3.Zero;

	protected override void OnAwake()
	{
		base.OnAwake();
	}


	// Transforme ton tableau en un tableau unidimensionnel
	int[] FlattenMatrix(int[,,] matrix)
	{
		int length = matrix.GetLength(0) * matrix.GetLength(1) * matrix.GetLength(2);
		int[] flattened = new int[length];

		int index = 0;
		for (int x = 0; x < matrix.GetLength(0); x++)
		{
			for (int y = 0; y < matrix.GetLength(1); y++)
			{
				for (int z = 0; z < matrix.GetLength(2); z++)
				{
					flattened[index++] = matrix[x, y, z];
				}
			}
		}

		return flattened;
	}

	// Transforme un tableau unidimensionnel en un tableau multidimensionnel
	int[,,] UnflattenMatrix(int[] flattened, int longueur, int largeur, int hauteur)
	{
		int[,,] matrix = new int[longueur, largeur, hauteur];

		int index = 0;
		for (int x = 0; x < longueur; x++)
		{
			for (int y = 0; y < largeur; y++)
			{
				for (int z = 0; z < hauteur; z++)
				{
					matrix[x, y, z] = flattened[index++];
				}
			}
		}

		return matrix;
	}

	protected override void OnStart()
	{		
		base.OnStart();	
		Network.DropOwnership();

		if(entityStart.IsValid() && entityEnd.IsValid())
		{
			entityStartPosition = entityStart.Transform.Position;
			entityEndPosition = entityEnd.Transform.Position;
			//mineWorld = Scene.GetAllComponents<Sdf3DWorld>().FirstOrDefault(); //1 sdfworld
			//mineWorld = Scene.CreateObject(true)
			GameObject clone = WorldPrefab.Clone(new Transform(Vector3.Zero), name: "SdfWorld_" + idMine);
			//mineWorld = clone.Components.Get<Sdf3DWorld>();
			mineWorld = Scene.GetAllObjects(true).Where(go => go.Name.Equals("SdfWorld_"+idMine)).FirstOrDefault().Components.Get<Sdf3DWorld>();//.GetAllComponents<Sdf3DWorld>().FirstOrDefault();
			mineWorld.GameObject.NetworkSpawn();
			//clone.NetworkSpawn();
			mineWorld.GameObject.Network.DropOwnership();
			Transform.Position = entityStartPosition 
											+ (Vector3.Backward * 16) 
											+ (Vector3.Down * 16)
											- (Vector3.Left * 16)
											+ (Vector3.Backward * (entityStartPosition.x - entityEndPosition.x) );
		
			float differenceZ = entityEndPosition.z - entityStartPosition.z;
			float differenceX = entityStartPosition.x - entityEndPosition.x;
			float differenceY = entityEndPosition.y - entityStartPosition.y;
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

			blocsRestants = Hauteur * Longueur * Largeur;

			if (idMine != 0)
				_ = regenMine();


			//shapeMatrix = new int[Longueur,Largeur,Hauteur];
			/*SetShapeMatrix(new int[Longueur,Largeur,Hauteur]);

			for (int x = 0; x < Longueur; x++)
			{
				for (int y = 0; y < Largeur; y++)
				{
					for (int z = 0; z < Hauteur; z++)
					{
						shapeMatrix[x, y, z] = 1; // Bloc présent
					}
				}
			}

			FlattenedShapeMatrix = FlattenMatrix(ShapeMatrix);*/

			SetShapeMatrix(new int[Longueur, Largeur, Hauteur]);

			for (int x = 0; x < Longueur; x++)
			{
				for (int y = 0; y < Largeur; y++)
				{
					for (int z = 0; z < Hauteur; z++)
					{
						ShapeMatrix[x, y, z] = 1; // Bloc présent
					}
				}
			}

			// Synchronise la version aplatie
			FlattenedShapeMatrix = FlattenMatrix(ShapeMatrix);


			Components.GetInChildren<BoxCollider>().Transform.Position = (entityStartPosition + entityEndPosition) / 2;
			Components.GetInChildren<BoxCollider>().Scale = (Hauteur + Longueur + Largeur) * 26.5f;
			//TODO: refaire un autre colliderbox trigger pour vraiment les tp, et utiliser celui la pour donner le status du world a ceux qui sont a cotés
			Components.GetInChildren<BoxCollider>().OnTriggerEnter = this.OnAroundMineTriggerEnter;
			Components.GetInChildren<BoxCollider>().OnTriggerExit = this.OnAroundMineTriggerExit;

			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().Transform.Position = (entityStartPosition + entityEndPosition) / 2 ;//- (Vector3.Up * 32f);
			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().Scale = new Vector3(Longueur*blockSizeV, Largeur*blockSizeV, Hauteur*blockSizeV);
			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().OnTriggerEnter = this.OnInsideMineTriggerEnter;
			GameObject.GetAllObjects(true).Where(go => go.Name.Equals("Inside")).FirstOrDefault().Components.Get<BoxCollider>().OnTriggerExit = this.OnInsideMineTriggerExit;

			int hauteurPanel = 125;
			Panel_Left.Transform.Position =  new Vector3(Transform.Position.x + Longueur*blockSize/2, Transform.Position.y,entityEndPosition.z + hauteurPanel);
			Panel_Right.Transform.Position = new Vector3(Transform.Position.x + Longueur*blockSize/2, Transform.Position.y + Largeur*blockSize, entityEndPosition.z + hauteurPanel);
			Panel_Front.Transform.Position = new Vector3(Transform.Position.x, Transform.Position.y + Largeur*blockSize/ 2, entityEndPosition.z + hauteurPanel);
			Panel_Back.Transform.Position =  new Vector3(Transform.Position.x + Longueur*blockSize, Transform.Position.y + Largeur*blockSize/ 2, entityEndPosition.z + hauteurPanel);


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

	public int PreRemoveCube(Vector3 pos)
	{
		int compteBlocsSupp = 0;
		// Conversion de la position réelle à l'index de la matrice
		//int x = (int)(pos.x / blockSize);
		//int y = (int)(pos.y / blockSize);
		//int z = (int)(pos.z / blockSize);

		int x = (int)((entityStartPosition.x - pos.x ) / blockSize);
		int y = (int)((pos.y - entityStartPosition.y) / blockSize);
		int z = (int)((pos.z - entityStartPosition.z) / blockSize);

		// Vérifier les limites de la matrice pour éviter les erreurs d'index
		if (x >= 0 && x < Longueur && y >= 0 && y < Largeur && z >= 0 && z < Hauteur)
		{
			// Vérifier s'il y a encore un bloc à cet endroit
			int index = x * (Largeur * Hauteur) + y * Hauteur + z;
			if (FlattenedShapeMatrix[index] == 1)
				compteBlocsSupp++;
		}
		return compteBlocsSupp;
	}

	[Broadcast]
	public void RemoveCube(Vector3 pos)
	{
		if (!IsProxy)
		{
			// Conversion de la position réelle à l'index de la matrice
			//int x = (int)(pos.x / blockSize);
			//int y = (int)(pos.y / blockSize);
			//int z = (int)(pos.z / blockSize);

			int x = (int)((entityStartPosition.x - pos.x ) / blockSize);
			int y = (int)((pos.y - entityStartPosition.y) / blockSize);
			int z = (int)((pos.z - entityStartPosition.z) / blockSize);

			//Log.Info($"Suppresion bloc xyz {x}/{y}/{z}");

			// Vérifier les limites de la matrice pour éviter les erreurs d'index
			if (x >= 0 && x < Longueur && y >= 0 && y < Largeur && z >= 0 && z < Hauteur)
			{
				// Vérifier s'il y a encore un bloc à cet endroit
				int index = x * (Largeur * Hauteur) + y * Hauteur + z;

				if (FlattenedShapeMatrix[index] == 1)
				{
					// Mise à jour de la matrice et décrémentation du compteur de blocs
					FlattenedShapeMatrix[index] = 0;
					blocsRestants--;

					// Logique pour la suppression visuelle du bloc
					var cube = new BoxSdf3D(Vector3.Zero, 32f, 0f).Transform(pos);
					mineWorld.SubtractAsync(cube, mineVolume);
				}
			} 
		}
	}
	/*public void RemoveCube(Vector3 pos)
	{
		if (!IsProxy)
		{
			//mineWorld.Network.TakeOwnership();
			var cube = new BoxSdf3D(Vector3.Zero, 32f, 0f).Transform(pos);
			mineWorld.SubtractAsync(cube, mineVolume);

			blocsRestants = blocsRestants - 1;
		}
	}*/

	public int PreRemoveLayer(Vector3 pos)
	{
		int compteBlocsSupp = 0;
		// Position de départ de la mine
		Vector3 start = entityStartPosition;

		// Conversion de la position réelle à l'index de la matrice pour la coordonnée z
		int z = (int)((pos.z - start.z) / blockSize);

		if (z >= 0 && z < Hauteur)
		{
			for (int x = 0; x < Longueur; x++)
			{
				for (int y = 0; y < Largeur; y++)
				{
					int index = x * (Largeur * Hauteur) + y * Hauteur + z;

					if (index >= 0 && index < FlattenedShapeMatrix.Length)
					{
						if (FlattenedShapeMatrix[index] == 1)
							compteBlocsSupp++;
					}
				}
			}

			//Log.Info($"nb blocs avant supp:{compteBlocsSupp}");
		}
		return compteBlocsSupp;
	}

	[Broadcast]
	public void RemoveLayer(Vector3 pos)
	{
		if (!IsProxy)
		{
			// Position de départ de la mine
			Vector3 start = entityStartPosition;

			// Conversion de la position réelle à l'index de la matrice pour la coordonnée z
			int z = (int)((pos.z - start.z) / blockSize);

			//Log.Info($"Suppression de la couche z {z}");

			int compteBlocsSupp = 0;

			// Vérifier les limites de la matrice pour éviter les erreurs d'index
			if (z >= 0)
			{
				// Parcourir tous les blocs dans la couche z et les supprimer
				for (int x = 0; x < Longueur; x++)
				{
					for (int y = 0; y < Largeur; y++)
					{
						int index = x * (Largeur * Hauteur) + y * Hauteur + z;

						// Vérifier s'il y a encore un bloc à cet endroit
						if (FlattenedShapeMatrix[index] == 1)
						{
							// Mise à jour de la matrice et décrémentation du compteur de blocs
							FlattenedShapeMatrix[index] = 0;
							blocsRestants--;
							compteBlocsSupp++;
						}
					}
				}

				//Log.Info($"nb blocs supp:{compteBlocsSupp}");

				// Logique pour la suppression visuelle de la couche
				Vector3 layerPos = new Vector3(entityEndPosition.x, entityStartPosition.y, pos.z);
				//var cube = new BoxSdf3D(Vector3.Zero, new Vector3(shapeMatrix.GetLength(0) * blockSize, shapeMatrix.GetLength(1) * blockSize, blockSize), 0f).Transform(layerPos);
				var cube = new BoxSdf3D(Vector3.Zero, new Vector3(Largeur*32f,Longueur*32f,32f), 0f).Transform(layerPos);
				mineWorld.SubtractAsync(cube, mineVolume);
			}
		}
	}

	/*public void RemoveLayer(Vector3 pos)
	{
		if (!IsProxy)
		{
			//var cube = new BoxSdf3D(Vector3.Zero, 32f, 0f).Transform(pos);
			Vector3 layerPos = new Vector3(entityEnd.Transform.Position.x, entityStart.Transform.Position.y, pos.z);
			var cube = new BoxSdf3D(Vector3.Zero, new Vector3(Largeur*32f,Longueur*32f,32f), 0f).Transform(layerPos);
			mineWorld.SubtractAsync(cube, mineVolume);

			blocsRestants = blocsRestants - (Largeur * Longueur);
		}
	}*/



	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(entityStart.IsValid() && entityEnd.IsValid())
		{			
			//Ligne entre le start/end
			/*var posTrace = Scene.Trace.Ray( entityStartPosition, entityEndPosition ).Run();
			Gizmo.Draw.Color = Color.Red;
  			Gizmo.Draw.LineThickness = 3;
			Gizmo.Draw.Line(posTrace.StartPosition, posTrace.EndPosition);*/
			
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
	}

	void OnAroundMineTriggerEnter(Collider collider)
	{
		if (!playersAround.ContainsKey(collider.GameObject.Name) && collider.GameObject.Tags.Has("player"))
		{
			/*Log.Info("Enter");
			Log.Info(collider.GameObject.Name);*/
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
			/*Log.Info("Exit");
			Log.Info(collider.GameObject.Name);*/
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
			Log.Info("Enter");
			Log.Info(collider.GameObject.Name);
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
			var cube = new BoxSdf3D(Vector3.Zero, new Vector3(Longueur*blockSizeF, Largeur*blockSizeF, Hauteur*blockSizeF), 0f).Transform(Transform.Position);
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

		int[,,] shapeMatrix = new int[Longueur, Largeur, Hauteur]; // Initialiser shapeMatrix

		// Nouvelle méthode pour remplir FlattenedShapeMatrix
		for (int x = 0; x < Longueur; x++)
		{
			for (int y = 0; y < Largeur; y++)
			{
				for (int z = 0; z < Hauteur; z++)
				{
					// Calcul de l'index dans le tableau aplati
					int index = x * (Largeur * Hauteur) + y * Hauteur + z;

					// Remplir la valeur dans le tableau aplati
					FlattenedShapeMatrix[index] = 1; // Bloc présent
				}
			}
		}

		blocsRestants = Hauteur * Longueur * Largeur;
		ActualPercent = 100f;
	}

	[Broadcast]
	public void teleportPlayers()
	{ 
		foreach (KeyValuePair<string, GameObject> entry in playersInside)
			entry.Value.Components.Get<PlayerController>().tpAbove((int)(entityEnd.Transform.Position.z + 350));
	}

	public void updateMinePercentage() 
	{
		if (mineWorld.IsValid())
		{
			//Log.Info(blocsRestants);
			ActualPercent = (blocsRestants / (Hauteur * Longueur * Largeur) * 100f);
			Log.Info($"[Mine:{idMine}] % of blocks remaining: {ActualPercent}");

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