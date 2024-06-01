using Sandbox;
using Sandbox.Citizen;

namespace Facepunch.Arena;

public abstract class WeaponComponent : Component
{
	[Property] public string DisplayName { get; set; }
	[Property] public float ReloadTime { get; set; } = 2f;
	[Property] public float DeployTime { get; set; } = 0.5f;
	[Property] public float FireRate { get; set; } = 3f;
	[Property] public float Spread { get; set; } = 0.01f;
	[Property] public Angles Recoil { get; set; }
	[Property] public float DamageForce { get; set; } = 5f;
	[Property] public float Damage { get; set; } = 10f;
	[Property] public GameObject ViewModelPrefab { get; set; }
	[Property] public AmmoType AmmoType { get; set; } = AmmoType.Pistol;
	[Property] public int DefaultAmmo { get; set; } = 60;
	[Property] public int ClipSize { get; set; } = 30;
	[Property] public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.Pistol;
	[Property] public SoundEvent DeploySound { get; set; }
	[Property] public SoundEvent FireSound { get; set; }
	[Property] public SoundEvent EmptyClipSound { get; set; }
	[Property] public SoundSequenceData ReloadSoundSequence { get; set; }
	[Property] public ParticleSystem MuzzleFlash { get; set; }
	[Property] public ParticleSystem ImpactEffect { get; set; }
	[Property] public ParticleSystem MuzzleSmoke { get; set; }
	[Property] public bool IsDeployed { get; set; }
	
	[Sync] public bool IsReloading { get; set; }
	[Sync] public int AmmoInClip { get; set; }

	public bool HasViewModel => ViewModel.IsValid();
	
	private SkinnedModelRenderer ModelRenderer { get; set; }
	private SoundSequence ReloadSound { get; set; }
	private ViewModel ViewModel { get; set; }
	private TimeUntil ReloadFinishTime { get; set; }
	private TimeUntil NextAttackTime { get; set; }
	private SkinnedModelRenderer EffectRenderer => ViewModel.IsValid() ? ViewModel.ModelRenderer : ModelRenderer;

	[Broadcast]
	public void Deploy()
	{
		if ( !IsDeployed )
		{
			IsDeployed = true;
			OnDeployed();
		}
	}

	[Broadcast]
	public void Holster()
	{
		if ( IsDeployed )
		{
			OnHolstered();
			IsDeployed = false;
		}
	}
	
	public virtual bool DoPrimaryAttack()
	{
		if ( !NextAttackTime ) return false;
		if ( IsReloading ) return false;

		if ( AmmoInClip <= 0 )
		{
			SendEmptyClipMessage();
			NextAttackTime = 1f / FireRate;
			return false;
		}
		
		var player = Components.GetInAncestors<PlayerController>();
		player.ApplyRecoil( Recoil );
		
		var attachment = EffectRenderer.GetAttachment( "muzzle" );
		var startPos = Scene.Camera.Transform.Position;
		var direction = Scene.Camera.Transform.Rotation.Forward;
		direction += Vector3.Random * Spread;
		
		var endPos = startPos + direction * 10000f;
		var trace = Scene.Trace.Ray( startPos, endPos )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.UsePhysicsWorld()
			.UseHitboxes()
			.Run();

		var damage = Damage;
		var origin = attachment?.Position ?? startPos;

		SendAttackMessage( origin, trace.EndPosition, trace.Distance );

		IHealthComponent damageable = null;
		if ( trace.Component.IsValid() )
		{
			damageable = trace.Component.Components.GetInAncestorsOrSelf<IHealthComponent>();
		}

		if ( damageable is not null )
		{
			if ( trace.Hitbox is not null && trace.Hitbox.Tags.Has( "head" ) )
			{
				player.DoHitMarker( true );
				damage *= 3f;
			}
			else
			{
				player.DoHitMarker( false );
			}
			
			damageable.TakeDamage( DamageType.Bullet, damage, trace.EndPosition, trace.Direction * DamageForce, GameObject.Id );
		}
		else if ( trace.Hit )
		{
			SendImpactMessage( trace.EndPosition, trace.Normal );
		}
		
		NextAttackTime = 1f / FireRate;
		//AmmoInClip--;

		return true;
	}

	public virtual bool DoReload()
	{
		var ammoToTake = ClipSize - AmmoInClip;
		if ( ammoToTake <= 0 )
			return false;
		
		var player = Components.GetInAncestors<PlayerController>();
		if ( !player.IsValid() )
			return false;

		if ( !player.Ammo.TryTake( AmmoType, ammoToTake, out var taken ) )
			return false;

		ReloadFinishTime = ReloadTime;
		IsReloading = true;
		AmmoInClip += taken;
			
		SendReloadMessage();
			
		return true;
	}

	protected override void OnStart()
	{
		if ( IsDeployed )
			OnDeployed();
		else
			OnHolstered();
		
		base.OnStart();
	}

	protected virtual void OnDeployed()
	{
		var player = Components.GetInAncestors<PlayerController>();

		if ( player.IsValid() )
		{
			foreach ( var animator in player.Animators )
			{
				animator.TriggerDeploy();
			}
		}
		
		ModelRenderer.Enabled = !HasViewModel;
		//ModelRenderer.Enabled = true;
		
		if ( DeploySound is not null )
		{
			Sound.Play( DeploySound, Transform.Position );
		}

		if ( !IsProxy )
		{
			CreateViewModel();
		}
		
		NextAttackTime = DeployTime;
	}

	protected virtual void OnHolstered()
	{
		ModelRenderer.Enabled = false;
		DestroyViewModel();

		ReloadSound?.Stop();
	}

	protected override void OnAwake()
	{
		ModelRenderer = Components.GetInDescendantsOrSelf<SkinnedModelRenderer>( true );
		base.OnAwake();
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy && ReloadFinishTime )
		{
			IsReloading = false;
		}

		ReloadSound?.Update( Transform.Position );

		base.OnUpdate();
	}

	protected override void OnDestroy()
	{
		if ( IsDeployed )
		{
			OnHolstered();
			IsDeployed = false;
		}
		
		base.OnDestroy();
	}
	
	private void DestroyViewModel()
	{
		ViewModel?.GameObject.Destroy();
		ViewModel = null;
	}

	private void CreateViewModel()
	{
		if ( !ViewModelPrefab.IsValid() )
			return;
		
		var player = Components.GetInAncestors<PlayerController>();

		var viewModelGameObject = ViewModelPrefab.Clone();
		viewModelGameObject.Flags |= GameObjectFlags.NotNetworked;
		viewModelGameObject.SetParent( player.ViewModelRoot, false );
		
		ViewModel = viewModelGameObject.Components.Get<ViewModel>();
		ViewModel.SetWeaponComponent( this );
		ViewModel.SetCamera( player.ViewModelCamera );
		
		ModelRenderer.Enabled = false;
	}

	[Broadcast]
	private void SendReloadMessage()
	{
		if ( ReloadSoundSequence is null )
			return;
		
		ReloadSound?.Stop();
		
		ReloadSound = new( ReloadSoundSequence );
		ReloadSound.Start( Transform.Position );
	}

	[Broadcast]
	private void SendEmptyClipMessage()
	{
		if ( EmptyClipSound is not null )
		{
			Sound.Play( EmptyClipSound, Transform.Position );
		}
	}

	[Broadcast]
	private void SendImpactMessage( Vector3 position, Vector3 normal )
	{
		if ( ImpactEffect is null ) return;

	}

	[Broadcast]
	private void SendAttackMessage( Vector3 startPos, Vector3 endPos, float distance )
	{
		

		if ( FireSound is not null )
		{
			Sound.Play( FireSound, startPos );
		}
	}
}
