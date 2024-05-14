using Editor;
using Sandbox;
//using Sandbox.Tools;

//[Library( "mine_physics" )]
//[MapObjectComponent]
public partial class MinePhysics : GameObject
{
	[Property]
	public int idMine { get; set; }

	[Property]
	public string sensMine { get; set; }

	[Property]
	public GameObject entity {get; set;}

	public MinePhysics(string _sensMine, int _idMine, GameObject _entity) {
		sensMine = _sensMine;
		idMine = _idMine;
		entity = _entity;
	}
}
