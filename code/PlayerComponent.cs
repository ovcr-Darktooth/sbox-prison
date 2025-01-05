using Sandbox;
using Sandbox.Citizen;
using System.Collections;
using System.Drawing;
using System.Runtime;
using System.Linq;

public class PlayerComponent : Component
{
	[Property] public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 800 );

	public Vector3 WishVelocity { get; private set; }

	[Property]
	public GameObject Camera { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Eye { get; set; }

	[Property]
	public CharacterController characterController { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] public bool FirstPerson { get; set; }

	[Sync]
	public Angles EyeAngles { get; set; }

	[Sync]
	public bool IsRunning { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		if ( IsProxy )
			return;
		characterController = Components.Get<CharacterController>();
		//var cam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		var cam = Components.Get<CameraComponent>();
		if ( cam is not null )
		{
			var ee = cam.WorldRotation.Angles();
			ee.roll = 0;
			EyeAngles = ee;
		}
	}

	protected override void OnUpdate()
	{
		// Eye input
		if ( !IsProxy )
		{
			var ee = EyeAngles;
			ee += Input.AnalogLook * 0.5f;
			ee.roll = 0;
			EyeAngles = ee;

			var cam = Components.Get<CameraComponent>();

			var lookDir = EyeAngles.ToRotation();

			if ( FirstPerson )
			{
				cam.WorldPosition = Eye.WorldPosition;
				cam.WorldRotation = lookDir;
			}
			else
			{
				cam.WorldPosition = WorldPosition + lookDir.Backward * 300 + Vector3.Up * 75.0f;
				cam.WorldRotation = lookDir;
			}



			IsRunning = Input.Down( "Run" );
		}

		var characterController = GameObject.Components.Get<CharacterController>();
		if ( characterController is null ) return;

		float rotateDifference = 0;

		// rotate body to look angles
		if ( Body is not null )
		{
			/*var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

			var v = cc.Velocity.WithZ( 0 );

			if ( v.Length > 10.0f )
			{
				targetAngle = Rotation.LookAt( v, Vector3.Up );
			}

			rotateDifference = Body.WorldRotation.Distance( targetAngle );

			if ( rotateDifference > 50.0f || cc.Velocity.Length > 10.0f )
			{
				Body.WorldRotation = Rotation.Lerp( Body.WorldRotation, targetAngle, Time.Delta * 2.0f );
			}*/
			var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();
			rotateDifference = Body.WorldRotation.Distance( targetAngle );
			if ( rotateDifference > 50f || characterController.Velocity.Length > 10f )
			{
				Body.WorldRotation = Rotation.Lerp( Body.WorldRotation, targetAngle, Time.Delta * 2f );
			}
		}


		if ( AnimationHelper is not null )
		{
			AnimationHelper.WithVelocity( characterController.Velocity );
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = characterController.IsOnGround;
			AnimationHelper.MoveRotationSpeed = rotateDifference;
			AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
			AnimationHelper.MoveStyle = IsRunning ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		}
	}

	[Rpc.Broadcast]
	public void OnJump( float floatValue, string dataString, object[] objects, Vector3 position )
	{
		AnimationHelper?.TriggerJump();
	}

	float fJumps;

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		BuildWishVelocity();

		var characterController = GameObject.Components.Get<CharacterController>();

		if ( characterController.IsOnGround && Input.Down( "Jump" ) )
		{
			float flGroundFactor = 1.0f;
			float flMul = 268.3281572999747f * 1.2f;
			//if ( Duck.IsActive )
			//	flMul *= 0.8f;

			characterController.Punch( Vector3.Up * flMul * flGroundFactor );
			//	characterController.IsOnGround = false;

			OnJump( fJumps, "Hello", new object[] { Time.Now.ToString(), 43.0f }, Vector3.Random );

			fJumps += 1.0f;

		}

		if ( characterController.IsOnGround )
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( WishVelocity );
			characterController.ApplyFriction( 4.0f );
		}
		else
		{
			characterController.Velocity -= Gravity * Time.Delta * 0.5f;
			characterController.Accelerate( WishVelocity.ClampLength( 50 ) );
			characterController.ApplyFriction( 0.1f );
		}

		characterController.Move();

		if ( !characterController.IsOnGround )
		{
			characterController.Velocity -= Gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}

	public void BuildWishVelocity()
	{
		var rot = EyeAngles.ToRotation();

		WishVelocity = rot * Input.AnalogMove;
		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( Input.Down( "Run" ) ) WishVelocity *= 320.0f;
		else WishVelocity *= 110.0f;
	}
}
