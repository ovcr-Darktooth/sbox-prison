using Sandbox;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Mine : GameObject
{

	public int IdMine { get; set; }
	public MinePhysics EntDebut { get; set; }
	public MinePhysics EntFin { get; set; }
	public int LevelMine { get; set; }
	public float ActualPercent {get;set;}
	public float PercentReset { get; set; }
	public List<MineBlockLines> MineBlockLines { get; set; }
	public string BlockMaterial { get; set; }
	//AxeX(Rouge)
	public int Longueur { get; set; }
	//AxeY(Vert)
	public int Largeur { get; set; }
	//AxeZ(Bleu)
	public int Hauteur { get; set; }
	//private Timer mineCheckTimer;

	//Constructeur pour une mine par défaut
	public Mine(int id, MinePhysics entD, MinePhysics entF)
	{
		IdMine = id;
		EntDebut = entD;
		EntFin = entF;
		LevelMine = 1;
		ActualPercent = 100f;
		PercentReset = 60f;
		MineBlockLines = new List<MineBlockLines>();	
		BlockMaterial = "materials/cables/cable_metal_wire_a.vmat_c";

		InitMine();
	}

	public async void InitMine()
	{
		MineBlockLines = new List<MineBlockLines>();
		await GameTask.Delay( 200 );
		//On récupére la différence de position entre les 2 entités sur l'axe Z (haut bas)
		/*float differenceZ = EntFin.Position.z - EntDebut.Position.z;
		float differenceX = EntDebut.Position.x - EntFin.Position.x;
		float differenceY = EntFin.Position.y - EntDebut.Position.y;
		Log.Info( $"Dif Z :{differenceZ}" );
		Log.Info( $"Dif Z/32 (nb blocs technique) :{differenceZ / 32}" ); // compté 24
		Log.Info( $"Dif X (rouge) :{differenceX}" );
		Log.Info( $"Dif X/32 (nb blocs technique) :{differenceX / 32}" ); // compté 24
		Log.Info( $"Dif Y (vert) :{differenceY}" );
		Log.Info( $"Dif Y/32 (nb blocs technique) :{differenceY / 32}" ); // compté 24
		Hauteur = (int)differenceZ / 32 + 1;
		Longueur = (int)differenceX / 32 + 1; // on compte mal a cause de la position prise en compte
		Largeur = (int)differenceY / 32 + 1; // pareil donc + 1
		Log.Info( $"Hauteur finale : {Hauteur}" );
		Log.Info( $"Longueur finale : {Longueur}" );
		Log.Info( $"Largeur finale : {Largeur}" );
		// for ( int i = 1; i < differenceZ / 32; i++ ) {
		// 	if ( i == 1 )
		// 	{
		// 		MineBlockLines aAjouter = new MineBlockLines();
		// 		aAjouter.GenerateMineProgressive( i, EntFin, EntDebut, Longueur, Largeur );
		// 		MineBlockLines.Add( aAjouter );
		// 	}
		// }

		for ( int i = 0; i < Hauteur; i++ )
		{
			await GameTask.Delay( 200 );
			MineBlockLines aAjouter = new MineBlockLines();
			aAjouter.InitBlocs( i, EntFin, EntDebut, Longueur, Largeur );
			MineBlockLines.Add( aAjouter );
		}*/
	}





	/*public void PreMineReset()
	{

		float posZMine = EntFin.Position.z + 32;

		List<Player> playersInMineZone = GetPlayersInMineZone();

		foreach ( Player player in playersInMineZone )
		{
			player.Position = new Vector3( player.Position.x, player.Position.y, posZMine ); // Téléporter le joueur
		}

		ResetMine();
	}

	public async void ResetMine()
	{
		foreach ( MineBlockLines mineBlockLine in MineBlockLines )
		{
			await GameTask.Delay( 200 );
			mineBlockLine.RegenLine();
			//mineBlockLine.RegenRawLine();
		}
	}

	private List<Player> GetPlayersInMineZone()
	{
		List<Player> playersInZone = new List<Player>();

		// foreach (Player player in Player.All)
		// {
		// 	if ( player.Position.x < this.EntDebut.Position.x && player.Position.x > this.EntFin.Position.x ) // Axe X
		// 	{
		// 		if ( player.Position.y < this.EntFin.Position.y && player.Position.y > this.EntDebut.Position.y ) // Axe Y
		// 		{
		// 			if ( player.Position.z < this.EntFin.Position.z && player.Position.z > this.EntDebut.Position.z ) // Axe Z
		// 			{
		// 				playersInZone.Add( player );
		// 			}
		// 		}
		// 	} 
		// }

		return playersInZone;
	}

	public void CoupageLigne()
	{
		foreach ( MineBlockLines mineBlockLine in MineBlockLines )
		{
			mineBlockLine.Coupage();
		}
	}



	public void ResetTimer()
	{
		// Réinitialisez le timer si nécessaire
		//mineCheckTimer.Change( 0, 60000 ); // Changez l'intervalle si besoin
	}

	public void StopTimer()
	{
		// Arrêtez le timer lorsque vous n'en avez plus besoin
		//mineCheckTimer.Dispose();
	}*/
}
