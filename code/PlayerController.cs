using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Citizen;

namespace Facepunch.Arena;

[Group( "Arena" )]
[Title( "Player Controller" )]
public class PlayerController : Component, Component.ITriggerListener, IHealthComponent
{
	[Property] public Vector3 Gravity { get; set; } = new ( 0f, 0f, 800f );
	
	public CharacterController CharacterController { get; private set; }
	public SkinnedModelRenderer ModelRenderer { get; private set; }
	public RagdollController Ragdoll { get; private set; }
	public List<CitizenAnimationHelper> Animators { get; private set; } = new();
	public RealTimeSince LastHitmarkerTime { get; private set; }
	public Vector3 WishVelocity { get; private set; }
	
	[Property] private CitizenAnimationHelper ShadowAnimator { get; set; }
	[Property] public WeaponContainer Weapons { get; set; }
	[Property] public CameraComponent ViewModelCamera { get; set; }
	[Property] public GameObject ViewModelRoot { get; set; }
	[Property] public AmmoContainer Ammo { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] public SoundEvent HurtSound { get; set; }
	[Property] public bool SicknessMode { get; set; }
	[Property] public bool SeeOwnModel { get; set; } = true;
	[Property] public bool EnableCrouching { get; set; }
	[Property] public float StandHeight { get; set; } = 64f;
	[Property] public float DuckHeight { get; set; } = 28f;
	[Property] public float HealthRegenPerSecond { get; set; } = 10f;
	[Property] public Action OnJump { get; set; }
	[Property] public float WalkSpeed { get; set; } = 150f;
	[Property] public float RunSpeed { get; set; } = 550f;
	[Property] public float CrouchSpeed { get; set; } = 90f;
	[Property] public float JumpForce { get; set; } = 600f;
	[Sync, Property] public float MaxHealth { get; private set; } = 100f;
	[Sync] public LifeState LifeState { get; private set; } = LifeState.Alive;
	[Sync] public float Health { get; private set; } = 100f;
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public bool IsAiming { get; set; }
	[Sync] public bool IsRunning { get; set; }
	[Sync] public bool IsCrouching { get; set; }
	[Sync] public int Deaths { get; private set; }
	[Sync] public int Kills { get; private set; }

	private RealTimeSince LastGroundedTime { get; set; }
	private RealTimeSince LastUngroundedTime { get; set; }
	private RealTimeSince TimeSinceDamaged { get; set; }
	private bool WantsToCrouch { get; set; }
	private Angles Recoil { get; set; }

	public void ApplyRecoil( Angles recoil )
	{
		if ( IsProxy ) return;
		
		Recoil += recoil;
	}

	public void DoHitMarker( bool isHeadshot )
	{
		Sound.Play( isHeadshot ? "hitmarker.headshot" : "hitmarker.hit" );
		LastHitmarkerTime = 0f;
	}

	public void ResetViewAngles()
	{
		var rotation = Rotation.Identity;
		EyeAngles = rotation.Angles().WithRoll( 0f );
	}

	public async void RespawnAsync( float seconds )
	{
		if ( IsProxy ) return;

		await Task.DelaySeconds( seconds );
		Respawn();
	}

	public void Respawn()
	{
		if ( IsProxy )
			return;

		Weapons.Clear();
		Weapons.GiveDefault();
		
		Ragdoll.Unragdoll();
		MoveToSpawnPoint();
		LifeState = LifeState.Alive;
		Health = MaxHealth;
	}
	
	[Broadcast]
	public void TakeDamage( DamageType type, float damage, Vector3 position, Vector3 force, Guid attackerId )
	{
		if ( LifeState == LifeState.Dead )
			return;
		
		if ( type == DamageType.Bullet )
		{

			if ( HurtSound is not null )
			{
				Sound.Play( HurtSound, Transform.Position );
			}
		}
		
		if ( IsProxy )
			return;

		TimeSinceDamaged = 0f;
		Health = MathF.Max( Health - damage, 0f );
		
		if ( Health <= 0f )
		{
			LifeState = LifeState.Dead;
			Ragdoll.Ragdoll( position, force );
			SendKilledMessage( attackerId );
		}
	}

	protected virtual bool CanUncrouch()
	{
		if ( !IsCrouching ) return true;
		if ( LastUngroundedTime < 0.2f ) return false;
		
		var tr = CharacterController.TraceDirection( Vector3.Up * DuckHeight );
		return !tr.Hit;
	}

	protected virtual void OnKilled( GameObject attacker )
	{
		if ( attacker.IsValid() )
		{
			
			var player = attacker.Components.GetInAncestorsOrSelf<PlayerController>();
			if ( player.IsValid() )
			{
				var chat = Scene.GetAllComponents<Chat>().FirstOrDefault();

				if ( chat.IsValid() )
					chat.AddTextLocal( "💀️", $"{player.Network.OwnerConnection.DisplayName} has killed {Network.OwnerConnection.DisplayName}" );
				
				if ( !player.IsProxy )
				{
					// We killed this player.
					player.Kills++;
				}
			}
		}
		
		if ( IsProxy )
			return;

		RespawnAsync( 3f );
		
		Deaths++;
	}
	
	protected override void OnAwake()
	{
		base.OnAwake();
		
		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>( true );
		
		CharacterController = Components.GetInDescendantsOrSelf<CharacterController>( true );
		CharacterController.IgnoreLayers.Add( "player" );
		
		Ragdoll = Components.GetInDescendantsOrSelf<RagdollController>( true );

		if ( CharacterController.IsValid() )
		{
			CharacterController.Height = StandHeight;
		}
		
		if ( IsProxy )
			return;

		ResetViewAngles();
	}

	protected override void OnStart()
	{
		Animators.Add( ShadowAnimator );
		Animators.Add( AnimationHelper );

		if ( !IsProxy )
		{
			//Respawn();
			RespawnAsync(0.5f);
		}

		if ( IsProxy && ViewModelCamera.IsValid() )
		{
			ViewModelCamera.Enabled = false;
		}
			
		base.OnStart();
	}

	private void UpdateModelVisibility()
	{
		if ( !ModelRenderer.IsValid() )
			return;
		
		var deployedWeapon = Weapons.Deployed;
		var shadowRenderer = ShadowAnimator.Components.Get<SkinnedModelRenderer>( true );
		var hasViewModel = deployedWeapon.IsValid() && deployedWeapon.HasViewModel;
		var clothingComponents = ModelRenderer.Components.GetAll<ClothingComponent>( FindMode.EverythingInSelfAndDescendants );
        var clothing = ModelRenderer.GameObject.GetAllObjects(true);
        var clothingShadow = shadowRenderer.GameObject.GetAllObjects(true);
		
		if ( hasViewModel )
		{
			shadowRenderer.Enabled = true;
            shadowRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
            //Log.Info(shadowRenderer.GameObject.Name);

            foreach ( var c in clothingShadow )
            {
                c.Components.Get<SkinnedModelRenderer>(true).RenderType = Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
            }

			ModelRenderer.Enabled = Ragdoll.IsRagdolled;
            ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
            
            foreach ( var c in clothing )
            {
				if(c.Name.StartsWith("Clothing"))
                {
                    c.Enabled = false;//Ragdoll.IsRagdolled;
                    c.Components.Get<SkinnedModelRenderer>(true).RenderType = Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
                }                
            }
			return;
		}
			
		ModelRenderer.SetBodyGroup( "head", IsProxy ? 0 : 1 );
		ModelRenderer.SetBodyGroup( "chest", IsProxy ? 0 : 1 );
		ModelRenderer.SetBodyGroup( "legs", IsProxy ? 0 : 1 );
		ModelRenderer.SetBodyGroup( "hands", IsProxy ? 0 : 1 );
		ModelRenderer.SetBodyGroup( "feet", IsProxy ? 0 : 1 );
		ModelRenderer.Enabled = !IsProxy ? true : SeeOwnModel;



		if ( Ragdoll.IsRagdolled )
		{
			ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
			shadowRenderer.Enabled = false;
		}
		else
		{
			ModelRenderer.RenderType = IsProxy
				? Sandbox.ModelRenderer.ShadowRenderType.On
				: Sandbox.ModelRenderer.ShadowRenderType.Off;

			shadowRenderer.Enabled = true;
		}

		foreach ( var c in clothing )
		{
			//c.ModelRenderer.Enabled = true;
			//c.Enabled = !IsProxy ? true : SeeOwnModel;
			//TODO: c'est presque sa
			if(c.Name.StartsWith("Clothing"))
			{
				c.Enabled = !SeeOwnModel;
				c.Components.Get<SkinnedModelRenderer>(true).RenderType = IsProxy
					? Sandbox.ModelRenderer.ShadowRenderType.On
					: Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
            //ClothingComponent
            //Log.Info("cat" + c.Components.Get<ClothingComponent>(true).Category);

            //if ( c.Category is Clothing.ClothingCategory.Hair or Clothing.ClothingCategory.Facial or Clothing.ClothingCategory.Hat )
            //{
                //TODO: juste cette ligne fait enlever la caméra, le if du dessus y est pour quelque chose, mais impossible d'accéder a un clothingcomponent (composant créé manuellement)
                //c.Components.Get<SkinnedModelRenderer>(true).RenderType = IsProxy ? Sandbox.ModelRenderer.ShadowRenderType.On : Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
				//c.ModelRenderer.RenderType = IsProxy ? Sandbox.ModelRenderer.ShadowRenderType.On : Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
            //}
		}

		foreach ( var c in clothingShadow )
		{
			c.Components.Get<SkinnedModelRenderer>(true).RenderType = Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
		}
	}

	protected override void OnPreRender()
	{
		base.OnPreRender();

		if ( !Scene.IsValid() || !Scene.Camera.IsValid() )
			return;

		UpdateModelVisibility();
		
		if ( IsProxy )
			return;

		if ( !Eye.IsValid() )
			return;

		if ( Ragdoll.IsRagdolled )
		{
			Scene.Camera.Transform.Position = Scene.Camera.Transform.Position.LerpTo( Eye.Transform.Position, Time.Delta * 32f );
			Scene.Camera.Transform.Rotation = Rotation.Lerp( Scene.Camera.Transform.Rotation, Eye.Transform.Rotation, Time.Delta * 16f );
			return;
		}

		var idealEyePos = Eye.Transform.Position;
		var headPosition = Transform.Position + Vector3.Up * CharacterController.Height;
		var headTrace = Scene.Trace.Ray( Transform.Position, headPosition )
			.UsePhysicsWorld()
			.IgnoreGameObjectHierarchy( GameObject )
			.WithAnyTags( "solid" )
			.Run();

		headPosition = headTrace.EndPosition - headTrace.Direction * 2f;
	
		var trace = Scene.Trace.Ray( headPosition, idealEyePos )
			.UsePhysicsWorld()
			.IgnoreGameObjectHierarchy( GameObject )
			.WithAnyTags( "solid" )
			.Radius( 2f )
			.Run();

		var deployedWeapon = Weapons.Deployed;
		var hasViewModel = deployedWeapon.IsValid() && deployedWeapon.HasViewModel;

		if ( hasViewModel )
			Scene.Camera.Transform.Position = Head.Transform.Position;
		else
			Scene.Camera.Transform.Position = trace.Hit ? trace.EndPosition : idealEyePos;
		
		if ( SicknessMode )
			Scene.Camera.Transform.Rotation = Rotation.LookAt( Eye.Transform.Rotation.Left ) * Rotation.FromPitch( -10f );
		else
			Scene.Camera.Transform.Rotation = EyeAngles.ToRotation() * Rotation.FromPitch( -10f );
	}

	protected override void OnUpdate()
	{
		if ( Ragdoll.IsRagdolled || LifeState == LifeState.Dead )
			return;
		
		if ( !IsProxy )
		{
			var angles = EyeAngles.Normal;
			angles += Input.AnalogLook * 0.5f;
			angles += Recoil * Time.Delta;
			//angles.pitch = angles.pitch.Clamp( -89f, 89f );
            // Normalisation de l'angle de pitch
            if (angles.pitch > 89.9f) angles.pitch = 89.9f;
            if (angles.pitch < -89.9f) angles.pitch = -89.9f;
			EyeAngles = angles.WithRoll( 0f );
			IsRunning = Input.Down( "Run" );
			Recoil = Recoil.LerpTo( Angles.Zero, Time.Delta * 8f );
		}
		
		var weapon = Weapons.Deployed;

		foreach ( var animator in Animators )
		{
			animator.HoldType = weapon.IsValid() ? weapon.HoldType : CitizenAnimationHelper.HoldTypes.None;
			animator.WithVelocity( CharacterController.Velocity );
			animator.WithWishVelocity( WishVelocity );
			animator.IsGrounded = CharacterController.IsOnGround;
			animator.MoveRotationSpeed = 0f;
			animator.DuckLevel = IsCrouching ? 1f : 0f;
			animator.WithLook( EyeAngles.Forward );
			animator.MoveStyle = ( IsRunning && !IsCrouching ) ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		}
	}

	protected virtual void DoCrouchingInput()
	{
		WantsToCrouch = CharacterController.IsOnGround && Input.Down( "Duck" );  //EnableCrouching &&

		if ( WantsToCrouch == IsCrouching )
			return;
		
		if ( WantsToCrouch )
		{
			CharacterController.Height = DuckHeight;
			IsCrouching = true;
		}
		else
		{
			if ( !CanUncrouch() )
				return;

			CharacterController.Height = StandHeight;
			IsCrouching = false;
		}
	}

	protected virtual void DoMovementInput()
	{
		BuildWishVelocity();

		if ( CharacterController.IsOnGround && Input.Down( "Jump" ) )
		{
			CharacterController.Punch( Vector3.Up * JumpForce );
			SendJumpMessage();
		}

		if ( CharacterController.IsOnGround )
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0f );
			CharacterController.Accelerate( WishVelocity );
			CharacterController.ApplyFriction( 4.0f );
		}
		else
		{
			CharacterController.Velocity -= Gravity * Time.Delta * 0.5f;
			CharacterController.Accelerate( WishVelocity.ClampLength( 50f ) );
			CharacterController.ApplyFriction( 0.1f );
		}
		
		CharacterController.Move();

		if ( !CharacterController.IsOnGround )
		{
			CharacterController.Velocity -= Gravity * Time.Delta * 0.5f;
			LastUngroundedTime = 0f;
		}
		else
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ( 0 );
			LastGroundedTime = 0f;
		}

		Transform.Rotation = Rotation.FromYaw( EyeAngles.ToRotation().Yaw() );
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		if ( Ragdoll.IsRagdolled || LifeState == LifeState.Dead )
			return;

		if ( TimeSinceDamaged > 3f )
		{
			Health += HealthRegenPerSecond * Time.Delta;
			Health = MathF.Min( Health, MaxHealth );
		}

		DoCrouchingInput();
		DoMovementInput();
		
		if (Input.Pressed("See_own_model"))
		{
			Log.Info("see own");
			SeeOwnModel = !SeeOwnModel;
		} 


		if ( Input.MouseWheel.y > 0 )
			Weapons.Next();
		else if ( Input.MouseWheel.y < 0 )
			Weapons.Previous();

		var weapon = Weapons.Deployed;
		if ( !weapon.IsValid() ) return;

		if ( Input.Down( "Attack1" ) )
		{
			if ( weapon.DoPrimaryAttack() )
			{
				SendAttackMessage();
			}
		}

		if ( Input.Released( "Reload" ) )
		{
			if ( weapon.DoReload() )
			{
				SendReloadMessage();
			}
		}
	}
	
	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		var pickup = other.Components.GetInAncestorsOrSelf<PickupComponent>();

		if ( pickup.IsValid() )
		{
			pickup.Pickup( GameObject );
		}
	}
	
	private void MoveToSpawnPoint()
	{
		if ( IsProxy )
			return;
		
		var spawnpoints = Scene.GetAllComponents<SpawnPoint>();
		var randomSpawnpoint = Game.Random.FromList( spawnpoints.ToList() );

		Transform.Position = randomSpawnpoint.Transform.Position;
		Transform.Rotation = Rotation.FromYaw( randomSpawnpoint.Transform.Rotation.Yaw() );
		EyeAngles = Transform.Rotation;
	}

	private void BuildWishVelocity()
	{
		var rotation = EyeAngles.ToRotation();

		WishVelocity = rotation * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0f );

		if ( !WishVelocity.IsNearZeroLength )
			WishVelocity = WishVelocity.Normal;

		if ( IsCrouching )
			WishVelocity *= CrouchSpeed;
		else if ( IsRunning )
			WishVelocity *= RunSpeed;
		else
			WishVelocity *= WalkSpeed;
	}

	[Broadcast]
	private void SendKilledMessage( Guid attackerId )
	{
		var attacker = Scene.Directory.FindByGuid( attackerId );
		OnKilled( attacker );
	}
	
	[Broadcast]
	private void SendReloadMessage()
	{
		foreach ( var animator in Animators )
		{
			var renderer = animator.Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
			renderer?.Set( "b_reload", true );
		}
	}

	[Broadcast]
	private void SendAttackMessage()
	{
		foreach ( var animator in Animators )
		{
			var renderer = animator.Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
			renderer?.Set( "b_attack", true );
		}
	}
	
	[Broadcast]
	private void SendJumpMessage()
	{
		foreach ( var animator in Animators )
		{
			animator.TriggerJump();
		}

		OnJump?.Invoke();
	}
}
