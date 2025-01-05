using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Sandbox;
using System.Collections.Generic;
using Facepunch.Arena;

public sealed class MineComponent_copy : Component, Component.ITriggerListener
{

	[Property]
	public GameObject entityStart {get; set;}
	[Property]
	public GameObject entityEnd {get; set;}
	[Property]
	public int idMine;

	public int LevelMine { get; set; }
	public float ActualPercent {get;set;}
	public float PercentReset { get; set; }
	//public List<MineBlockLines> MineBlockLines { get; set; }
	public string BlockMaterial { get; set; }
	//AxeX(Rouge)
	public int Longueur { get; set; }
	//AxeY(Vert)
	public int Largeur { get; set; }
	//AxeZ(Bleu)
	public int Hauteur { get; set; }

	private Dictionary<string, GameObject> playersInside = new Dictionary<string, GameObject>();

	// Matrice de données représentant la forme
    private int[,] shapeMatrix = new int[,]
    {
        { 0, 1, 1, 1, 0 },
        { 0, 0, 0, 0, 0 },
        { 1, 1, 1, 1, 1 }
    };

    // Taille d'un bloc
    private float blockSize = 33f;

	protected override void OnAwake()
	{
		base.OnAwake();

	}

	protected override void OnStart()
	{		
		base.OnStart();	

		//if(entityStart.IsValid() && entityEnd.IsValid())
		//	GenerateGroups();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(entityStart.IsValid() && entityEnd.IsValid())
		{			
			var posTrace = Scene.Trace.Ray( entityStart.WorldPosition, entityEnd.WorldPosition ).Run();
			Gizmo.Draw.Color = Color.Red;
  			Gizmo.Draw.LineThickness = 3;
			Gizmo.Draw.Line(posTrace.StartPosition, posTrace.EndPosition);
		}
		else 
		{
			Log.Info("pas valide");
		}

		//GenerateGroups();
		
	}

	void ITriggerListener.OnTriggerEnter(Collider collider)
	{
		Log.Info("Enter");
		Log.Info(collider.GameObject.Name);

		if (!playersInside.ContainsKey(collider.GameObject.Name))
			playersInside.Add(collider.GameObject.Name,collider.GameObject);
	}

	void ITriggerListener.OnTriggerExit(Collider collider)
	{
		Log.Info("Exit");
		Log.Info(collider.GameObject.Name);

		if (playersInside.ContainsKey(collider.GameObject.Name))
			playersInside.Remove(collider.GameObject.Name);
	}

	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------Création----------------------------

	public void GenerateGroups()
	{
		bool[,] visited = new bool[shapeMatrix.GetLength(0), shapeMatrix.GetLength(1)];
		List<List<Vector2Int>> groups = new List<List<Vector2Int>>();

		for (int x = 0; x < shapeMatrix.GetLength(0); x++)
		{
			for (int y = 0; y < shapeMatrix.GetLength(1); y++)
			{
				if (shapeMatrix[x, y] == 1 && !visited[x, y])
				{
					List<Vector2Int> newGroup = new List<Vector2Int>();
					ExploreGroup(x, y, visited, newGroup);
					groups.Add(newGroup);
				}
			}
		}

		Log.Info("nb groupes:" + groups.Count);
		foreach (var group in groups)
		{
			GenerateMeshForGroup(group);
		}
	}

	void ExploreGroup(int x, int y, bool[,] visited, List<Vector2Int> group)
	{
		// Implémentation simplifiée d'un DFS
		Stack<Vector2Int> stack = new Stack<Vector2Int>();
		stack.Push(new Vector2Int(x, y));

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (visited[current.x, current.y])
				continue;

			visited[current.x, current.y] = true;
			group.Add(current);

			// Vérifie les voisins (à gauche, à droite, en haut, en bas)
			foreach (var dir in new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) })
			{
				int nx = current.x + dir.x, ny = current.y + dir.y;
				if (nx >= 0 && ny >= 0 && nx < shapeMatrix.GetLength(0) && ny < shapeMatrix.GetLength(1))
					if (shapeMatrix[nx, ny] == 1 && !visited[nx, ny])
						stack.Push(new Vector2Int(nx, ny));
			}
		}
	}

	void GenerateMeshForGroup(List<Vector2Int> group)
	{
		// Crée un mesh basé sur les coordonnées du groupe
		// Cela pourrait être un grand mesh englobant ou des meshes individuels
		var mesh = new Mesh(Material.Load("materials/dev/reflectivity_30.vmat"));
		List<Vector3> positions = new List<Vector3>();
		List<int> indices = new List<int>();

		int indexOffset = 0;
		foreach (var block in group)
		{
			Vector3 basePosition = new Vector3(block.x * blockSize, 0, block.y * blockSize);
			Vector3[] cubeVertices = new Vector3[]
			{
				basePosition + new Vector3(-0.5f, -0.5f, 0.5f) * blockSize,
				basePosition + new Vector3(-0.5f, 0.5f, 0.5f) * blockSize,
				basePosition + new Vector3(0.5f, 0.5f, 0.5f) * blockSize,
				basePosition + new Vector3(0.5f, -0.5f, 0.5f) * blockSize,
				basePosition + new Vector3(-0.5f, -0.5f, -0.5f) * blockSize,
				basePosition + new Vector3(-0.5f, 0.5f, -0.5f) * blockSize,
				basePosition + new Vector3(0.5f, 0.5f, -0.5f) * blockSize,
				basePosition + new Vector3(0.5f, -0.5f, -0.5f) * blockSize,
			};

			positions.AddRange(cubeVertices);
			// Ajoutez les indices pour chaque face du cube, en assumant que chaque face utilise 4 vertices avec deux triangles
			indices.AddRange(new int[] {
				0 + indexOffset, 1 + indexOffset, 2 + indexOffset, 0 + indexOffset, 2 + indexOffset, 3 + indexOffset, // Face avant
				4 + indexOffset, 5 + indexOffset, 6 + indexOffset, 4 + indexOffset, 6 + indexOffset, 7 + indexOffset, // Face arrière
				// Ajoutez des indices pour les autres faces...
			});

			indexOffset += 8; // Chaque cube ajoute 8 nouveaux vertices
		}

		BuildMesh(mesh, positions, indices);
	}

	public void BuildMesh(Mesh mesh, List<Vector3> positions, List<int> indices)
	{
		// Convert positions and indices to vertex buffer and index buffer respectively
		mesh.CreateVertexBuffer<Vector3>(positions.Count, Vertex.Layout, positions.ToArray());
		mesh.CreateIndexBuffer(indices.Count, indices.ToArray());

		var model = Model.Builder
			.AddMesh(mesh)
			.AddCollisionBox(blockSize) // Vous devrez probablement ajuster cela
			.Create();


		Components.GetOrCreate<ModelRenderer>().Model = model;
	}

	/*private readonly Vector2Int[] directions = {
		new Vector2Int(0, 1),  // Haut
		new Vector2Int(0, -1), // Bas
		new Vector2Int(1, 0),  // Droite
		new Vector2Int(-1, 0)  // Gauche
	};

	public void GenerateGroups()
	{
		int rows = shapeMatrix.GetLength(0);
		int cols = shapeMatrix.GetLength(1);
		bool[,] visited = new bool[rows, cols];
		List<List<Vector2Int>> groups = new List<List<Vector2Int>>();

		for (int x = 0; x < rows; x++)
		{
			for (int y = 0; y < cols; y++)
			{
				if (shapeMatrix[x, y] == 1 && !visited[x, y])
				{
					List<Vector2Int> newGroup = new List<Vector2Int>();
					ExploreGroup(x, y, visited, newGroup);
					groups.Add(newGroup);
				}
			}
		}

		Log.Info("nb groupes:" + groups.Count);
		foreach (var group in groups)
		{
			GenerateMeshForGroup(group);
		}
	}

	private void ExploreGroup(int x, int y, bool[,] visited, List<Vector2Int> group)
	{
		Stack<Vector2Int> stack = new Stack<Vector2Int>();
		stack.Push(new Vector2Int(x, y));

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (visited[current.x, current.y])
				continue;

			visited[current.x, current.y] = true;
			group.Add(current);

			foreach (var dir in directions)
			{
				int nx = current.x + dir.x, ny = current.y + dir.y;
				if (nx >= 0 && ny >= 0 && nx < shapeMatrix.GetLength(0) && ny < shapeMatrix.GetLength(1))
					if (shapeMatrix[nx, ny] == 1 && !visited[nx, ny])
						stack.Push(new Vector2Int(nx, ny));
			}
		}
	}*/


	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-------------------------------------------------------------
	//-----------------------Suppression---------------------------

	public void RemoveBlock(Vector3 worldPosition)
	{
		// Convertir les coordonnées du monde en coordonnées de la matrice
		Vector2Int matrixCoords = ConvertWorldToMatrixCoords(worldPosition);

		// Vérifier si les coordonnées sont valides
		if (IsValidMatrixCoords(matrixCoords))
		{
			// Supprimer le bloc dans la matrice
			shapeMatrix[matrixCoords.x, matrixCoords.y] = 0;

			// Mettre à jour les regroupements si nécessaire
			RecalculateBlockGroups();
		}
	}

	public Vector2Int ConvertWorldToMatrixCoords(Vector3 worldPosition)
	{
		// Convertir les coordonnées du monde en coordonnées de la matrice
		// Implémentation dépendante de la taille de la grille et de la position du joueur
		// Pour un exemple simple avec une grille régulière, tu peux diviser les coordonnées du monde par la taille d'un bloc et les arrondir à l'entier le plus proche
		return new Vector2Int();
	}

	public bool IsValidMatrixCoords(Vector2Int matrixCoords)
	{
		// Vérifier si les coordonnées de la matrice sont valides
		// Assure-toi que les coordonnées ne sortent pas de la plage de la matrice

		return true;
	}

	public void RecalculateBlockGroups()
	{
		// Recalculer les regroupements de blocs dans la matrice
		// Implémentation dépendante de l'algorithme de regroupement que tu utilises
	}
}