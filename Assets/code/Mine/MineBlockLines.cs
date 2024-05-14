using Sandbox;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

public class MineBlockLines : GameObject
{
	//1ere ligne = en haut
	public int Layer { get; set; }

	public MinePhysics EntFin { get; set; }
	public MinePhysics EntDeb { get; set; }
	//Dim 1 = long
	//Dim 2 = lar
	public MineBlock[,] MineBlocks { get; set; }

	/*public Entity MonCube { get; set; }

	public int GetLongueur()
	{
		return MineBlocks.GetLength( 0 ); // Récupérer la longueur de la première dimension de la matrice
	}

	public int GetLargeur()
	{
		return MineBlocks.GetLength( 1 ); // Récupérer la largeur de la deuxième dimension de la matrice
	}

	public void InitBlocs(int layer, MinePhysics entFin, MinePhysics entDebut, int Longueur, int Largeur)
	{
		Layer = layer;
		MineBlocks = new MineBlock[Longueur, Largeur];
		EntDeb = entDebut;
		EntFin = entFin;
		float hauteurLayer = entFin.Position.z - Layer*32;

		for (int i = 0; i < Longueur; i++)
		{
			for(int j = 0; j < Largeur - 6; j++)
			{
				if (true)
				{
					if (i == 0 && j == 0)
					{
						Mesh mesh = new Mesh( Material.Load( "materials/dev/reflectivity_30.vmat" ) );
						Vector3[] verticesData = new Vector3[]
			{
                    // TOP
                    new Vector3( 10, 10, 10 ),
					new Vector3( -10, 10, 10),
					new Vector3( -10, -10, 10 ),
					new Vector3( 10, -10, 10 ),

                    // BOTTOM
                    new Vector3( -10, 10, -10 ),
					new Vector3( 10, 10, -10 ),
					new Vector3( 10, -10, -10 ),
					new Vector3( -10, -10, -10 ),
			};

						int[] trianglesData = new int[] {
					0,1,2,
					0,2,3,
					4,5,6,
					4,6,7,
			};

						List<SimpleVertex> vertexList = new();

						for ( var k = 0; k < 2; ++k )
						{
							for ( var l = 0; l < 3; ++l )
							{
								var vertexIndex = trianglesData[(k * 3) + l];
								var pos = verticesData[vertexIndex];

								vertexList.Add( new SimpleVertex()
								{
									position = pos,
									normal = Vector3.Up,
									tangent = Vector3.Forward,
									texcoord = Vector2.Zero,
								} );
							}
						}
						mesh.CreateVertexBuffer( vertexList.Count, SimpleVertex.Layout, vertexList );
					}
				}
				if ( false )
				{
					MineBlock mineBlock = new MineBlock();
					float posX = entFin.Position.x + i * 32;
					float posY = entFin.Position.y - j * 32;
					mineBlock.Position = new Vector3( posX, posY, hauteurLayer );
					MineBlocks[i, j] = mineBlock;
				}
			}
		}
	}


	//Régénération de la ligne des blocs manquants
	public void RegenLine()
	{
		float hauteurLayer = EntFin.Position.z - Layer * 32;
		for ( int i = 0; i < GetLongueur(); i++ )
		{
			for ( int j = 0; j < GetLargeur(); j++ )
			{
				if ( MineBlocks[i, j] == null || !MineBlocks[i, j].IsValid )
				{
					MineBlock mineBlock = new MineBlock();
					float posX = EntFin.Position.x + i * 32;
					float posY = EntFin.Position.y - j * 32;
					mineBlock.Position = new Vector3( posX, posY, hauteurLayer );
					MineBlocks[i, j] = mineBlock;
				} 
			}
		}
	}

	//Régénération complette de la ligne
	public void RegenRawLine()
	{
		float hauteurLayer = EntFin.Position.z - Layer * 32;
		for ( int i = 0; i < GetLongueur(); i++ )
		{
			for ( int j = 0; j < GetLargeur(); j++ )
			{
				if ( MineBlocks[i, j].IsValid )
				{
					MineBlocks[i, j].Delete();
				}
				MineBlock mineBlock = new MineBlock();
				float posX = EntFin.Position.x + i * 32;
				float posY = EntFin.Position.y - j * 32;
				mineBlock.Position = new Vector3( posX, posY, hauteurLayer );
				MineBlocks[i, j] = mineBlock;
			}
		}
	}

	public void Coupage()
	{
		bool aDelete = false;
		for ( int i = 0; i < GetLongueur(); i++ )
		{
			for ( int j = 0; j < GetLargeur(); j++ )
			{
				if ( !MineBlocks[i, j].IsValid )
				{
					aDelete = true;
				}
			}
		}

		if ( aDelete )
		{
			for ( int i = 0; i < GetLongueur(); i++ )
			{
				for ( int j = 0; j < GetLargeur(); j++ )
				{
					MineBlocks[i, j].Delete();
				}
			}
		}
	}


	public void GenerateMissingBlocks()
	{
		// La largeur et la longueur de votre ligne
		int longueur = GetLongueur();
		int largeur = GetLargeur();

		// Parcourez la ligne en longueur
		for ( int i = 0; i < longueur; i++ )
		{
			// Parcourez la ligne en largeur
			for ( int j = 0; j < largeur; j++ )
			{
				// Vérifiez si le bloc à la position (i, j) est manquant
				if ( MineBlocks[i, j] == null )
				{
					// Générez un nouveau bloc pour cet emplacement
					MineBlock mineBlock = new MineBlock();

					// Calculez la position en fonction de l'entité de début et des dimensions du bloc
					float posX = EntDeb.Position.x + i * 32;
					float posY = EntDeb.Position.y - j * 32;
					float posZ = Layer * 32; // Supposons que chaque couche a une épaisseur de 32

					// Définissez la position du bloc généré
					mineBlock.Position = new Vector3( posX, posY, posZ );

					// Stockez le bloc généré dans la matrice MineBlocks
					MineBlocks[i, j] = mineBlock; 
				}
			}
		}
	}*/
}
