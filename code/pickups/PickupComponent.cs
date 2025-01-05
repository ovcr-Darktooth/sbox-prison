using System;
using Sandbox;

namespace Facepunch.Arena;

[Hide]
public abstract class PickupComponent : Component
{
	[Property] public Collider Collider { get; set; }
	[Property] public SoundEvent PickupSound { get; set; }
	
	private Vector3 StartPosition { get; set; }
	
	public void Pickup( GameObject picker )
	{
		if ( !Network.IsOwner )
			return;
		
		if ( !GameObject.IsValid() )
			return;

		PlayPickupSound();
		OnPickup( picker.Id );
		
		GameObject.Destroy();
	}
	
	protected virtual void OnPickup( Guid pickerId )
	{
		
	}

	protected override void OnStart()
	{
		StartPosition = WorldPosition;
		Collider.IsTrigger = true;
		
		base.OnStart();
	}

	protected override void OnFixedUpdate()
	{
		WorldPosition = StartPosition + Vector3.Up * MathF.Sin( Time.Now ) * 8f;
		WorldRotation = WorldRotation.RotateAroundAxis( Vector3.Up, 90f * Time.Delta );
		
		base.OnFixedUpdate();
	}

	[Rpc.Broadcast]
	private void PlayPickupSound()
	{
		if ( PickupSound is null )
			return;
		
		Sound.Play( PickupSound, WorldPosition );
	}
}
