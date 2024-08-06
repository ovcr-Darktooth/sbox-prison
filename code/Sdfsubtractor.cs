using Sandbox;
using Sandbox.Sdf;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
public sealed class SDFGun : Component
{
	public MineComponent Mine { get; set; }
	[Property] public float SphereSize { get; set; } = 50;
	[Property] public Sdf3DVolume sdf3DVolume { get; set; }
	[Property] public CameraComponent ViewModelCamera { get; set; } 

	private TimeUntil nextDestroy = 0.1f;	
	public bool buildMode = false; 

	protected override void OnStart()
	{
		base.OnStart();
		nextDestroy = 0.1f;
	}
	protected override void OnUpdate()
	{
		if (ViewModelCamera.IsValid())
		{
			var tr = Scene.Trace.Ray(ViewModelCamera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
			/*
			if (tr.Hit)
			{
				/ *Vector3 hitPosition = tr.HitPosition;
				Vector3 normal = tr.HitPosition.Normal;
				Vector3 adjustPosition = hitPosition - normal * 16f; // Ajuste la position dans la direction du normal (demi-taille du cube)

			// Arrondir la position ajustée à la grille
				Vector3 snappedPosition = Vector3.Down * 32f + adjustPosition.SnapToGrid(32f);* /

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

				var trace = Scene.Trace.Ray(ViewModelCamera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
				int lastUnderscoreIndex = trace.GameObject.Name.LastIndexOf('_');
				ReadOnlySpan<char> span = trace.GameObject.Name.AsSpan(lastUnderscoreIndex + 1);
				string result = span.ToString();
				if (trace.Hit && Scene.GetAllObjects(true).Where(go => go.Name == "Mine_"+result).FirstOrDefault().IsValid())
				{			
					Mine = Scene.GetAllObjects(true).Where(go => go.Name == "Mine_"+result).FirstOrDefault().Components.Get<MineComponent>();

					Vector3 layerPos = new Vector3(Mine.entityEnd.Transform.Position.x, Mine.entityStart.Transform.Position.y, snappedPosition.z);
					//var cube = new BoxSdf3D(Vector3.Zero, new Vector3(Mine.Largeur,Mine.Longueur,32f), 0f).Transform(layerPos);

					var bboxtest = new BBox(Vector3.Zero, new Vector3(Mine.Largeur*32f,Mine.Longueur*32f,32f));

					Gizmo.Transform = new Transform(layerPos, Rotation.Identity);
					Gizmo.Draw.Color =  Color.Blue;
					Gizmo.Draw.LineBBox(bboxtest);
				}
			}*/

			if (Input.Down("attack1") && !IsProxy && tr.Hit)
			{
				/*if (buildMode)
					_ = Add();
				else*/
					//_ = Subtract()
				Log.Info(nextDestroy);
				if (nextDestroy <= 0f)
				{
					enchantsCalc();
					SubtractCube();
					nextDestroy = 0.075f;
				}
			}

			/*if (Input.Pressed("attack2"))
			{
				buildMode = !buildMode;
				Log.Info("Buildmode:"+ buildMode);
			}*/
		}
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

	public void enchantsCalc()
	{
		Random random = new Random();

        // Générer un nombre aléatoire entre 0 et 1
        double randomValue = random.NextDouble() * 100;

        // Formater le nombre avec 6 chiffres après la virgule
        string formattedValue = randomValue.ToString("F6");

        // Convertir le format string en double pour obtenir la précision requise
        double preciseValue = double.Parse(formattedValue);

		//Log.Info(preciseValue);
		

		double jackHammerChance = GameObject.Components.Get<Enchantments>().getChanceOfEnchant(Enchants.Jackhammer);
		//Log.Info(jackHammerChance);

		if (preciseValue < jackHammerChance)
		{
			SubtractLayer();
			Log.Info("Jackhammer proc !");
		}
		
		//GameObject.Components.Get<Enchantments>().getChanceOfEnum(Enchants.Jackhammer);
	}

	//[Broadcast]
	public void SubtractCube()
	{	
		var tr = Scene.Trace.Ray(ViewModelCamera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		//var cube = new BoxSdf3D(Vector3.Zero, 33f).Transform((tr.HitPosition  + Vector3.Down * 32f).SnapToGrid(32f));
		int lastUnderscoreIndex = tr.GameObject.Name.LastIndexOf('_');
		ReadOnlySpan<char> span = tr.GameObject.Name.AsSpan(lastUnderscoreIndex + 1);
		string result = span.ToString();
		if (tr.Hit && Scene.GetAllObjects(true).Where(go => go.Name == "Mine_"+result).FirstOrDefault().IsValid())
		{			
			Mine = Scene.GetAllObjects(true).Where(go => go.Name == "Mine_"+result).FirstOrDefault().Components.Get<MineComponent>();
			//var position = (tr.HitPosition + Vector3.Down * 32f).SnapToGrid(32f);

			Vector3 hitPosition = tr.HitPosition;
			Vector3 normal = tr.Normal; // Utiliser la normale fournie par le tracé
			Vector3 adjustPosition = hitPosition - normal * (32f/2) + Vector3.Down * 16f;  // Ajuster la position dans la direction de la normale (demi-taille du cube)
			
			// Arrondir la position ajustée à la grille
			Vector3 snappedPosition = adjustPosition.SnapToGrid(32f);
			Mine.RemoveCube(snappedPosition);
		}
		//World.SubtractAsync(cube);
			//Scene.GetAllComponents<MineComponent>().Where()
		//Scene.GetAllComponents<MineComponent>().FirstOrDefault().RemoveCube(position);
	}

	public void SubtractLayer()
	{
		var tr = Scene.Trace.Ray(ViewModelCamera.ScreenNormalToRay(0.5f), 200).WithoutTags("player").Run();
		int lastUnderscoreIndex = tr.GameObject.Name.LastIndexOf('_');
		ReadOnlySpan<char> span = tr.GameObject.Name.AsSpan(lastUnderscoreIndex + 1);
		string result = span.ToString();
		if (tr.Hit && Scene.GetAllObjects(true).Where(go => go.Name == "Mine_"+result).FirstOrDefault().IsValid())
		{			
			Mine = Scene.GetAllObjects(true).Where(go => go.Name == "Mine_"+result).FirstOrDefault().Components.Get<MineComponent>();

			Vector3 hitPosition = tr.HitPosition;
			Vector3 normal = tr.Normal; // Utiliser la normale fournie par le tracé
			Vector3 adjustPosition = hitPosition - normal * (32f/2) + Vector3.Down * 16f;  // Ajuster la position dans la direction de la normale (demi-taille du cube)
			
			// Arrondir la position ajustée à la grille
			Vector3 snappedPosition = adjustPosition.SnapToGrid(32f);
			Mine.RemoveLayer(snappedPosition);


			
		}
	}
}
