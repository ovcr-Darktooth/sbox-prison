using Sandbox;
using Sandbox.Sdf;
using System.Linq;
using System.Threading.Tasks;
public sealed class SDFGun : Component
{
	public Sdf3DWorld World { get; set; }
	[Property] public float SphereSize { get; set; } = 50;
	[Property] public Sdf3DVolume sdf3DVolume { get; set; }

	public bool buildMode = false;
	protected override void OnUpdate()
	{
		World = Scene.GetAllComponents<Sdf3DWorld>().FirstOrDefault();
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		if (tr.Hit)
		{
			Gizmo.Transform = new Transform(tr.HitPosition.SnapToGrid(32) + Vector3.Down * 32f, Rotation.Identity);
			Gizmo.Draw.Color = buildMode ? Color.Green : Color.Red;
			Gizmo.Draw.LineSphere(Vector3.Zero, 50f);
		}
		if (Input.Pressed("attack1") && !IsProxy)
		{
			/*if (buildMode)
				_ = Add();
			else*/
				//_ = Subtract()
				SubtractCube();
		}

		/*if (Input.Pressed("attack2"))
		{
			buildMode = !buildMode;
			Log.Info("Buildmode:"+ buildMode);
		}*/

	}

	public async Task Add()
	{
		if (World is null) return;
		World.Network.TakeOwnership();
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		var cube = new BoxSdf3D(Vector3.Zero, 50f).Transform(tr.HitPosition);
		await World.AddAsync(cube, sdf3DVolume);
	}

	public async Task Subtract()
	{
		if (World is null) return;
		World.Network.TakeOwnership();
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		//var sphere = new SphereSdf3D(Vector3.Zero, SphereSize).Transform(new Transform(tr.HitPosition, Rotation.Identity));
		var cube = new BoxSdf3D(Vector3.Zero, 33f).Transform((tr.HitPosition  + Vector3.Down * 32f).SnapToGrid(32f));
		await World.SubtractAsync(cube);
	}

	//[Broadcast]
	public void SubtractCube()
	{
		if (World is null) return;
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		//var cube = new BoxSdf3D(Vector3.Zero, 33f).Transform((tr.HitPosition  + Vector3.Down * 32f).SnapToGrid(32f));
		var position = (tr.HitPosition  + Vector3.Down * 50f).SnapToGrid(32f);
		//World.SubtractAsync(cube);
		Scene.GetAllComponents<MineComponent>().FirstOrDefault().RemoveCube(position);
	}
}
