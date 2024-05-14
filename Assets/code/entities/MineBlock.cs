using Editor;
using Sandbox;

[Spawnable]
[Library( "mine_block" , Title = "Bloc de mine" )]
public partial class MineBlock : Component
{

	/*public override void OnStart()
	{
		base.OnStart();
		SetModel( "models/dev/box.vmdl_c" );
		SetupPhysicsFromOBB( PhysicsMotionType.Static, -25f, 25 );
		Scale = 0.64f;
	}*/

	/*public bool IsUsable( Entity user )
	{
		return true;
	}

	public bool OnUse( Entity user )
	{
		Delete();

		return true;
	}

	public void Remove()
	{
		Delete();
	}*/

}
