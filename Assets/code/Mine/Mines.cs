using System.Collections.Generic;

public class Mines : GameObject
{

	public List<Mine> LesMines { get; set; }
	//Les mines qui sont déja initialisés.
	public List<int> MinesInit {  get; set; }
	public Mines()
	{
		LesMines = new List<Mine>();
		MinesInit = new List<int>();
	}

	public void InitMine(int idMine, MinePhysics entD, MinePhysics entF)
	{
		MinesInit.Add( idMine );
		if ( idMine != 2 ) // 2 = mine de test juste pour init
		{
			Mine newMine = new Mine( idMine, entD, entF );
			LesMines.Add( newMine );
		}
	}
}
