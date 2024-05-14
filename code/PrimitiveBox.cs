using System;
using System.Collections.Generic;
using Sandbox;

namespace WorldCraft
{
	public partial class PrimitiveBox : Component
	{	
		private int[,] shapeMatrix = new int[,]
		{
			{ 0, 1, 1, 1, 1 },
			{ 0, 1, 0, 0, 1 },
			{ 1, 1, 0, 0, 1 }
		};

		private float blockSize = 30f;
  
        protected override void OnStart()
        {		
            base.OnStart();	
            
			//GenerateGroups();
			//GenerateGroups3D();

			//var model = BuildModel();
            //Components.GetOrCreate<ModelRenderer>().Model = model;
        }

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
			var mesh = new Mesh(Material.Load("materials/dev/reflectivity_30.vmat"));
			List<SimpleVertex> vertices = new List<SimpleVertex>(); // Liste pour stocker les sommets
			List<int> indices = new List<int>(); // Liste pour stocker les indices des sommets

			int indexOffset = 0;
			foreach (var block in group)
			{
				Vector3 basePosition = new Vector3(-(block.y * blockSize), (block.x * blockSize), 0);
				Vector3 center = basePosition + new Vector3(0, 0, 0.5f) * blockSize; // Centre de la face avant du cube
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

				/*Vector3[] cubeVertices = new Vector3[]
				{
					basePosition + new Vector3(-0.5f, -0.5f, 0.5f) * blockSize, // 0
					basePosition + new Vector3(0.5f, -0.5f, 0.5f) * blockSize,  // 1
					basePosition + new Vector3(0.5f, 0.5f, 0.5f) * blockSize,   // 2
					basePosition + new Vector3(-0.5f, 0.5f, 0.5f) * blockSize,  // 3
					basePosition + new Vector3(-0.5f, -0.5f, -0.5f) * blockSize,// 4
					basePosition + new Vector3(0.5f, -0.5f, -0.5f) * blockSize, // 5
					basePosition + new Vector3(0.5f, 0.5f, -0.5f) * blockSize,  // 6
					basePosition + new Vector3(-0.5f, 0.5f, -0.5f) * blockSize, // 7
				};*/

				// Ajouter les sommets
				for (int i = 0; i < cubeVertices.Length; i++)
				{
					vertices.Add(new SimpleVertex()
					{
						position = cubeVertices[i],
						normal = CalculateNormal(cubeVertices[i],center), // Calculer la normale pour chaque sommet

					});
				}

				// Définir les indices pour former les triangles
				int[] faceIndices = new int[]
				{
					0, 2, 1, 0, 3, 2, // Face vers le haut, vers l'axe bleu
					4, 5, 6, 4, 6, 7, // Face vers le bas, inverse de l'axe bleu
					1, 6, 5, 1, 2, 6, // Côté gauche, vers l'axe vert
					0, 4, 7, 0, 7, 3, // Côté droit, inverse de l'axe vert
					3, 6, 2, 3, 7, 6, // Côté devant, vers l'axe rouge
					0, 1, 5, 0, 5, 4  // Côté derriere, inverse de l'axe rouge
				};

				// Ajouter les indices décalés par rapport aux sommets précédents
				foreach (int index in faceIndices)
				{
					indices.Add(index + indexOffset);
				}

				indexOffset += 8; // Chaque cube ajoute 8 nouveaux sommets
			}

			// Convertir les sommets et les indices en vertex buffer et index buffer
			mesh.CreateVertexBuffer<SimpleVertex>(vertices.Count, SimpleVertex.Layout, vertices.ToArray());
			mesh.CreateIndexBuffer(indices.Count,indices.ToArray());

			// Créer le modèle
			var model = Model.Builder
				.AddMesh(mesh)
				.AddCollisionBox(blockSize) // Vous devrez probablement ajuster cela
				.Create();

			// Définir le modèle sur le composant ModelRenderer
			Components.GetOrCreate<ModelRenderer>().Model = model;
		}

		private Vector3 CalculateNormal(Vector3 vertexPosition, Vector3 center)
		{
			Vector3 direction;
			direction = vertexPosition - center;

			return direction.Normal; // Normaliser le vecteur pour obtenir la direction
		}



		/*private int[,,] shapeMatrix3D = new int[,,]
		{
			{
				{ 0, 0, 0, 0, 0 },
				{ 0, 1, 1, 1, 0 },
				{ 0, 1, 0, 1, 0 },
				{ 0, 1, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 }
			},
			{
				{ 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 },
				{ 0, 1, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 }
			},
			{
				{ 0, 0, 0, 0, 0 },
				{ 0, 1, 0, 0, 0 },
				{ 0, 1, 0, 1, 0 },
				{ 0, 1, 1, 1, 0 },
				{ 0, 0, 0, 0, 0 }
			},
		};*/

		private int[,,] shapeMatrix3D = new int[,,]
		{
			{
				{ 1, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0 }
			},
		};

		public void GenerateGroups3D()
		{
			bool[,,] visited = new bool[shapeMatrix3D.GetLength(0), shapeMatrix3D.GetLength(1), shapeMatrix3D.GetLength(2)];
			List<List<Vector3Int>> groups = new List<List<Vector3Int>>();

			for (int x = 0; x < shapeMatrix3D.GetLength(0); x++)
			{
				for (int y = 0; y < shapeMatrix3D.GetLength(1); y++)
				{
					for (int z = 0; z < shapeMatrix3D.GetLength(2); z++)
					{
						if (shapeMatrix3D[x, y, z] == 1 && !visited[x, y, z])
						{
							List<Vector3Int> newGroup = new List<Vector3Int>();
							ExploreGroup3D(x, y, z, visited, newGroup);
							groups.Add(newGroup);
						}
					}
				}
			}

			Log.Info("nb groupes:" + groups.Count);
			foreach (var group in groups)
			{
				GenerateMeshForGroup3D(group);
			}
		}

		void ExploreGroup3D(int x, int y, int z, bool[,,] visited, List<Vector3Int> group)
		{
			// Implémentation simplifiée d'un DFS
			Stack<Vector3Int> stack = new Stack<Vector3Int>();
			stack.Push(new Vector3Int(x, y, z));

			while (stack.Count > 0)
			{
				var current = stack.Pop();
				if (visited[current.x, current.y, current.z])
					continue;

				visited[current.x, current.y, current.z] = true;
				group.Add(current);

				// Vérifie les voisins (6 directions possibles)
				foreach (var dir in new Vector3Int[]
				{
					new Vector3Int(0, 0, 1),
					new Vector3Int(0, 0, -1),
					new Vector3Int(0, 1, 0),
					new Vector3Int(0, -1, 0),
					new Vector3Int(1, 0, 0),
					new Vector3Int(-1, 0, 0)
				})
				{
					int nx = current.x + dir.x, ny = current.y + dir.y, nz = current.z + dir.z;
					if (nx >= 0 && ny >= 0 && nz >= 0 &&
						nx < shapeMatrix3D.GetLength(0) && ny < shapeMatrix3D.GetLength(1) && nz < shapeMatrix3D.GetLength(2))
						if (shapeMatrix3D[nx, ny, nz] == 1 && !visited[nx, ny, nz])
							stack.Push(new Vector3Int(nx, ny, nz));
				}
			}
		}

		void GenerateMeshForGroup3D(List<Vector3Int> group)
		{
			var mesh = new Mesh(Material.Load("materials/dev/reflectivity_30.vmat"));
			List<SimpleVertex> vertices = new List<SimpleVertex>(); // Liste pour stocker les sommets
			List<int> indices = new List<int>(); // Liste pour stocker les indices des sommets

			int indexOffset = 0;
			foreach (var block in group)
			{
				Vector3 basePosition = new Vector3(-(block.y * blockSize), (block.x * blockSize), (block.z * blockSize)); // Ajustez la logique pour la position en 3D
				Vector3 center = basePosition + new Vector3(0, 0, 0.5f) * blockSize; // Centre de la face avant du cube
				// Définir les sommets du cube en 3D
				Vector3[] cubeVertices = new Vector3[]
				{
					basePosition + new Vector3(-0.5f, -0.5f, 0.5f) * blockSize, // 0
					basePosition + new Vector3(-0.5f, 0.5f, 0.5f) * blockSize,  // 1
					basePosition + new Vector3(0.5f, 0.5f, 0.5f) * blockSize,   // 2
					basePosition + new Vector3(0.5f, -0.5f, 0.5f) * blockSize,  // 3
					basePosition + new Vector3(-0.5f, -0.5f, -0.5f) * blockSize,// 4
					basePosition + new Vector3(-0.5f, 0.5f, -0.5f) * blockSize, // 5
					basePosition + new Vector3(0.5f, 0.5f, -0.5f) * blockSize,  // 6
					basePosition + new Vector3(0.5f, -0.5f, -0.5f) * blockSize, // 7
				};

				var uAxis = new Vector3[]
				{
					Vector3.Forward,
					Vector3.Left,
					Vector3.Left,
					Vector3.Forward,
					Vector3.Right,
					Vector3.Backward,  
				};

				var vAxis = new Vector3[]
				{
					Vector3.Left,
					Vector3.Forward,
					Vector3.Down,
					Vector3.Down,
					Vector3.Down,
					Vector3.Down,
				};

				// Définir les indices pour former les triangles du cube
				/*int[] faceIndices = new int[]
				{
					0, 2, 1, 0, 3, 2, // Face vers le haut, vers l'axe bleu
					4, 5, 6, 4, 6, 7, // Face vers le bas, inverse de l'axe bleu
					1, 6, 5, 1, 2, 6, // Côté gauche, vers l'axe vert
					0, 4, 7, 0, 7, 3, // Côté droit, inverse de l'axe vert
					3, 6, 2, 3, 7, 6, // Côté devant, vers l'axe rouge
					0, 1, 5, 0, 5, 4  // Côté derriere, inverse de l'axe rouge
				};*/

				int[] faceIndices = new int[]
				{
					0, 2, 1, 0, 3, 2, // Face vers le haut, vers l'axe bleu
					4, 6, 5, 4, 7, 6, // Face vers le bas, inverse de l'axe bleu
					0, 5, 1, 0, 4, 5, // Côté gauche, vers l'axe vert
					3, 6, 7, 3, 2, 6, // Côté droit, inverse de l'axe vert
					1, 6, 2, 1, 5, 6, // Côté devant, vers l'axe rouge
					0, 7, 4, 0, 3, 4  // Côté derriere, inverse de l'axe rouge
				};

				// Ajouter les sommets
				for (int i = 0; i < cubeVertices.Length; i++)
				{
					//var tangent = uAxis[i];
					//var binormal = vAxis[i];
					//var normal = Vector3.Cross( tangent, binormal );		
					vertices.Add(new SimpleVertex()
					{
						position = cubeVertices[i],
						normal = CalculateNormal(cubeVertices[i], center), // Calculer la normale pour chaque sommet
						//tangent = tangent,
						//texcoord = Planar( cubeVertices[i] / 30, uAxis[i], vAxis[i] )
					});
				}

				// Ajouter les indices décalés par rapport aux sommets précédents
				foreach (int index in faceIndices)
				{
					indices.Add(index + indexOffset);
				}

				indexOffset += 8; // Chaque cube ajoute 8 nouveaux sommets
			}

			// Convertir les sommets et les indices en vertex buffer et index buffer
			mesh.CreateVertexBuffer<SimpleVertex>(vertices.Count, SimpleVertex.Layout, vertices.ToArray());
			mesh.CreateIndexBuffer(indices.Count, indices.ToArray());

			// Créer le modèle
			var model = Model.Builder
				.AddMesh(mesh)
				.AddCollisionBox(blockSize) // Vous devrez probablement ajuster cela
				.Create();

			// Définir le modèle sur le composant ModelRenderer
			Components.GetOrCreate<ModelRenderer>().Model = model;

		}

		protected static Vector2 Planar( Vector3 pos, Vector3 uAxis, Vector3 vAxis )
		{
			return new Vector2()
			{
				x = Vector3.Dot( uAxis, pos ),
				y = Vector3.Dot( vAxis, pos )
			};
		}
	}


	/*public Model BuildModel()
		{
			var mesh = new Mesh( Material.Load( "materials/dev/reflectivity_30.vmat" ) );
			//BuildMesh( mesh );

			var model = Model.Builder
				.AddMesh( mesh )
				.AddCollisionBox( blockSize )
				.Create();

			return model;
		}
	
	
	public void BuildMesh( Mesh mesh )
		{
			var positions = new Vector3[]
			{
				new Vector3(-0.5f, -0.5f, 0.5f) * blockSize,
				new Vector3(-0.5f, 0.5f, 0.5f) * blockSize,
				new Vector3(0.5f, 0.5f, 0.5f) * blockSize,
				new Vector3(0.5f, -0.5f, 0.5f) * blockSize,
				new Vector3(-0.5f, -0.5f, -0.5f) * blockSize,
				new Vector3(-0.5f, 0.5f, -0.5f) * blockSize,
				new Vector3(0.5f, 0.5f, -0.5f) * blockSize,
				new Vector3(0.5f, -0.5f, -0.5f) * blockSize,
			};

			var faceIndices = new int[]
			{
				0, 1, 2, 3,
				7, 6, 5, 4,
				0, 4, 5, 1,
				1, 5, 6, 2,
				2, 6, 7, 3,
				3, 7, 4, 0,
			};

			var uAxis = new Vector3[]
			{
				Vector3.Forward,
				Vector3.Left,
				Vector3.Left,
				Vector3.Forward,
				Vector3.Right,
				Vector3.Backward,
			};

			var vAxis = new Vector3[]
			{
				Vector3.Left,
				Vector3.Forward,
				Vector3.Down,
				Vector3.Down,
				Vector3.Down,
				Vector3.Down,
			};

            


			List<SimpleVertex> verts = new();
			List<int> indices = new();

			for ( var i = 0; i < 6; ++i )
			{
				var tangent = uAxis[i];
				var binormal = vAxis[i];
				var normal = Vector3.Cross( tangent, binormal );

				for ( var j = 0; j < 4; ++j )
				{
					var vertexIndex = faceIndices[(i * 4) + j];
					var pos = positions[vertexIndex];

					verts.Add( new SimpleVertex()
					{
						position = pos,
						normal = normal,
						tangent = tangent,
						texcoord = Planar( pos / 32, uAxis[i], vAxis[i] )
					} );
				}

				indices.Add( i * 4 + 0 );
				indices.Add( i * 4 + 2 );
				indices.Add( i * 4 + 1 );
				indices.Add( i * 4 + 2 );
				indices.Add( i * 4 + 0 );
				indices.Add( i * 4 + 3 );
			}

			mesh.CreateVertexBuffer<SimpleVertex>( verts.Count, SimpleVertex.Layout, verts.ToArray() );
			mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );
		}
		
		protected static Vector2 Planar( Vector3 pos, Vector3 uAxis, Vector3 vAxis )
		{
			return new Vector2()
			{
				x = Vector3.Dot( uAxis, pos ),
				y = Vector3.Dot( vAxis, pos )
			};
		}*/
}
