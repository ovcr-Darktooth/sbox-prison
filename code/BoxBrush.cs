using Sandbox;
using Sandbox.Sdf;
using System.Threading.Tasks;
using System.Linq;

public sealed class BoxBrush : Component
{
    [Property] public int Size { get; set; } = 100;
    [Property] public Sdf3DVolume sdf3DVolume { get; set; }

    protected override void OnStart()
    {
        _ = BuildTask();
    }
    protected override void OnUpdate()
    {
    }

    public async Task BuildTask()
    {
        var world = Scene.GetAllComponents<Sdf3DWorld>().FirstOrDefault();
        world.GameObject.NetworkSpawn();
        await DrawBrush(world);
    }

    public async Task DrawBrush(Sdf3DWorld world)
    {
        if (world is null) return;
        var box = new BoxSdf3D(Vector3.Zero, Size).Transform(new Transform(WorldPosition, Rotation.Identity));
        await world.AddAsync(box, sdf3DVolume);
    }

    protected override void DrawGizmos()
    {
        Gizmo.Draw.Color = Color.Blue;
        Gizmo.Transform = Transform.World;
        Gizmo.Draw.LineBBox(new BBox(Vector3.Zero, new Vector3(Size)));
    }
}