using System.Net.Http;
using Sandbox;
using Sandbox.Sdf;
using Sandbox.Utility;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Sandbox.Network;
using System.Linq;
using System.Numerics;

[Title("Box sdf")]
[Icon("dashboard")]
public sealed class BoxSdf : Component
{


	[Property] public Sdf3DVolume Volume { get; set; }
	protected override void OnStart()
	{
		var sdfWorld = Components.GetInChildren<Sdf3DWorld>();
		//var sdfWorld = Components.Get<Sdf3DWorld>();
		sdfWorld.GameObject.NetworkSpawn();
		//var boxtest = new BoxSdf3D(Vector3.Zero, Vector3.One * 50);

		_ = AddCube(sdfWorld, WorldPosition, 32, Volume);
		//await sdfWorld.AddAsync(boxtest, Volume);
		//boxtest = new BoxSdf3D(BBox.FromPositionAndSize(Vector3.Zero, 50f));
		//await sdfWorld.AddAsync(boxtest, Volume);
	}

	public async Task AddCube(Sdf3DWorld world, Vector3 pos, Vector3 size, Sdf3DVolume volume)
	{
		var cube = new BoxSdf3D(Vector3.Zero, size);
		await world.AddAsync(cube, volume);
	}
}