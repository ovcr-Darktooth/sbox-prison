using Sandbox;
using Sandbox.Sdf;
using System;
using System.Linq;
using System.Threading.Tasks;
public sealed class SDFGun : Component
{
	public MineComponent Mine { get; set; }
	[Property] public float SphereSize { get; set; } = 50;
	[Property] public Sdf3DVolume sdf3DVolume { get; set; }
	

	public bool buildMode = false;
	protected override void OnUpdate()
	{
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		if (tr.Hit)
		{
			/*Vector3 hitPosition = tr.HitPosition;
			Vector3 normal = tr.HitPosition.Normal;
			Vector3 adjustPosition = hitPosition - normal * 16f; // Ajuste la position dans la direction du normal (demi-taille du cube)

		// Arrondir la position ajustée à la grille
			Vector3 snappedPosition = Vector3.Down * 32f + adjustPosition.SnapToGrid(32f);*/

			Vector3 hitPosition = tr.HitPosition;
			Vector3 normal = tr.Normal; // Utiliser la normale fournie par le tracé
			Vector3 adjustPosition = hitPosition - normal * (32f/2) + Vector3.Down * 16f; // Ajuster la position dans la direction de la normale (demi-taille du cube)
			
			// Arrondir la position ajustée à la grille
			Vector3 snappedPosition = adjustPosition.SnapToGrid(32f);
			//Gizmo.Transform = new Transform(tr.HitPosition.SnapToGrid(32) + Vector3.Down * 32f, Rotation.Identity);
			Gizmo.Transform = new Transform(snappedPosition, Rotation.Identity);
			Gizmo.Draw.Color = buildMode ? Color.Green : Color.Red;
			//Gizmo.Draw.LineSphere(Vector3.Zero, 50f);
			Gizmo.Draw.LineBBox(BBox.FromHeightAndRadius(32f,16f));
		}

		if (Input.Down("attack1") && !IsProxy && tr.Hit)
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

	/*public async Task Add()
	{
		if (World is null) return;
		World.Network.TakeOwnership();
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		var cube = new BoxSdf3D(Vector3.Zero, 50f).Transform(tr.HitPosition);
		await World.AddAsync(cube, sdf3DVolume);
	}*/

	/*public async Task Subtract()
	{
		if (World is null) return;
		World.Network.TakeOwnership();
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		//var sphere = new SphereSdf3D(Vector3.Zero, SphereSize).Transform(new Transform(tr.HitPosition, Rotation.Identity));
		var cube = new BoxSdf3D(Vector3.Zero, 33f).Transform((tr.HitPosition  + Vector3.Down * 32f).SnapToGrid(32f));
		await World.SubtractAsync(cube);
	}*/

	//[Broadcast]
	public void SubtractCube()
	{	
		var tr = Scene.Trace.Ray(Scene.Camera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		//var cube = new BoxSdf3D(Vector3.Zero, 33f).Transform((tr.HitPosition  + Vector3.Down * 32f).SnapToGrid(32f));
		int lastUnderscoreIndex = tr.GameObject.Name.LastIndexOf('_');
		ReadOnlySpan<char> span = tr.GameObject.Name.AsSpan(lastUnderscoreIndex + 1);
    	string result = span.ToString();
		Mine = Scene.GetAllObjects(true).Where(go => go.Name == "Mine_"+result).FirstOrDefault().Components.Get<MineComponent>();
		if (Mine is null)
			return;
		//var position = (tr.HitPosition + Vector3.Down * 32f).SnapToGrid(32f);

		Vector3 hitPosition = tr.HitPosition;
		Vector3 normal = tr.Normal; // Utiliser la normale fournie par le tracé
		Vector3 adjustPosition = hitPosition - normal * (32f/2) + Vector3.Down * 16f;  // Ajuster la position dans la direction de la normale (demi-taille du cube)
		
		// Arrondir la position ajustée à la grille
		Vector3 snappedPosition = adjustPosition.SnapToGrid(32f);
		Mine.RemoveCube(snappedPosition);
		//World.SubtractAsync(cube);
			//Scene.GetAllComponents<MineComponent>().Where()
		//Scene.GetAllComponents<MineComponent>().FirstOrDefault().RemoveCube(position);
	}
}
