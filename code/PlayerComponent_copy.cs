using System.Numerics;
using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerComponent_copy : Component
{
	[Property]
	public GameObject Camera { get; set; }

	[Property]
	public CharacterController characterController { get; set; }

	public CitizenAnimationHelper animationHelper { get; set; }

	[Property]
	public GameObject Head { get; set; }
	[Property]
	public GameObject Body { get; set; }

	[Property]
	public float WalkSpeed { get; set; } = 120f;

	[Property]
	public float RunSpeed { get; set; } = 250f;

	[Property]
	public float JumpStrength { get; set; } = 400f;

	[Property] 
	public float GroundControl { get; set; } = 4.0f;

	[Property] 
	public float AirControl { get; set; } = 0.1f; 

	[Property] 
	public float MaxForce { get; set; } = 50f;

	[Property] 
	public float CrouchSpeed { get; set; } = 90f;

	[Property]
	public Vector3 EyePosition { get; set; }

	/*[Property]
	public MapInstance MapInstance {get;set;}*/

	public Vector3 WishVelocity = Vector3.Zero;

	public bool IsCrouching = false;
	public bool IsSprinting = false;
	public Angles EyeAngles { get; set; }

	//public Mines lesMines;
	//public IEnumerable<MinePhysics> minesPhysics;


	protected override void OnEnabled()
	{

		if ( IsProxy )
			return;
		Log.Info( $"Owner is {Network.OwnerId}" );
		//base.OnAwake();
		
		characterController = Components.Get<CharacterController>();
		animationHelper = Body.Components.Get<CitizenAnimationHelper>();

		//preInitMines();
	}

	/*void preInitMines()
	{
		var lesMinesBlock = Scene.GetAllObjects( true ).Where( go => go.Name.StartsWith("mine_physics"));

		foreach ( GameObject preMinePhysics in lesMinesBlock )
		{
			var idMine = 0;
			var sensMine = "aucun";

			Log.Info(preMinePhysics.Name);

			var splitted = preMinePhysics.Name.Split("_");


			if (splitted.Length == 4) 
			{
				Log.Info(splitted[0]);
				Log.Info(splitted[1]);
				Log.Info(splitted[2]);
				Log.Info(splitted[3]);
				sensMine = splitted[2];
				idMine = int.Parse(splitted[3]);
			}

			if (idMine != 0 && sensMine != "aucun")
			{
				//minesPhysics.Append(new MinePhysics(sensMine, idMine, preMinePhysics));
			}

		}
	}*/

	protected override void OnUpdate()
	{

		//base.OnUpdate();
		if ( IsProxy )
			return;

		UpdateCrouch();
		IsSprinting = Input.Down( "Run" );
		if (Input.Pressed("Jump")) Jump();

		RotateBody();
		UpdateAnimations();

		CheckMinesAround();
	}


	protected override void OnFixedUpdate()
	{
		//base.OnFixedUpdate();
		BuildWishVelocity();
		Move();
	}


	void BuildWishVelocity()
	{
		WishVelocity = 0;
	
		var rot = Head.WorldRotation;
		if (Input.Down( "Forward")) WishVelocity += rot.Forward; 
		if (Input.Down( "Backward")) WishVelocity += rot.Backward; 
		if (Input.Down( "Left" ) ) WishVelocity += rot.Left;
		if (Input.Down( "Right" ) ) WishVelocity += rot.Right;

		WishVelocity = WishVelocity.WithZ( 0 );
		if (!WishVelocity.IsNearZeroLength) WishVelocity = WishVelocity.Normal;

		if (IsCrouching) WishVelocity *= CrouchSpeed;
		else if (IsSprinting) WishVelocity *= RunSpeed; 
		else WishVelocity *= WalkSpeed;
	}


	void Move()
	{
		// Get gravity from our scene
		var gravity = Scene.PhysicsWorld.Gravity;
		if ( characterController.IsOnGround )
		{
			// Apply Friction/Acceleration
			characterController.Velocity = characterController.Velocity.WithZ( 0 ); 
			characterController.Accelerate( WishVelocity ); 
			characterController.ApplyFriction( GroundControl );
		}
		else
		{
			// Apply Air Control / Gravity
			characterController.Velocity += gravity * Time.Delta * 0.5f; 
			characterController.Accelerate( WishVelocity.ClampLength(MaxForce) );
			characterController.ApplyFriction( AirControl );
		}
		
		// Move the character controller
		characterController.Move();
		// Apply the second half of gravity after movement
		if ( !characterController.IsOnGround )
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}
	

	void RotateBody()
	{
		if ( Body is null ) return;

		var targetAngle = new Angles( 0, Head.WorldRotation.Yaw(), 0 ).ToRotation();
		float rotateDifference = Body.WorldRotation.Distance( targetAngle );
		if ( rotateDifference > 50f || characterController.Velocity.Length > 10f )
		{
			Body.WorldRotation = Rotation.Lerp( Body.WorldRotation, targetAngle, Time.Delta * 2f );
		}
	}

	void Jump()
	{
		if (!characterController.IsOnGround) return;

		characterController.Punch(Vector3.Up * JumpStrength);
		animationHelper?.TriggerJump();
	}

	void UpdateAnimations()
	{
		if (animationHelper is null) return;

		animationHelper.WithWishVelocity(WishVelocity);
		animationHelper.WithVelocity(characterController.Velocity);
		animationHelper.AimAngle = Head.WorldRotation;
		animationHelper.IsGrounded = characterController.IsOnGround;
		animationHelper.WithLook(Head.WorldRotation.Forward, 1f, 0.75f, 0.5f);
		animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		animationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}


	void UpdateCrouch()
	{
		if(characterController is null) return;


		if (Input.Down("Crouch") && !IsCrouching)
		{
			IsCrouching = true;
			characterController.Height /= 2f;
		}

		var fromPos = Body.WorldPosition + Vector3.Up * 15f;
		var targetHit = Body.WorldPosition;
		targetHit += Vector3.Up * 64f;
		var upTrace = Scene.Trace.Sphere(10f, fromPos, targetHit)
					.WithoutTags("player", "trigger" )
					.Run();
		bool canUncrouch = !upTrace.Hit;
		if ((Input.Released("Crouch") && IsCrouching && canUncrouch) || (!Input.Down("Crouch") && IsCrouching && canUncrouch))
		{
			IsCrouching = false;
			characterController.Height *= 2f;
		}
	}
	
	/*protected override void OnStart()
	{
		base.OnStart();

		if ( Components.TryGet<SkinnedModelRenderer>( out var model ) )
		{
			var clothing = ClothingContainer.CreateFromLocalUser();
			clothing.Apply( model );
		}
	}*/

	/*protected override void OnEnabled()
	{
		base.OnEnabled();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}*/


	void CheckMinesAround() {

		//Vector3 playerPosition = Body.Transform.Position;

		//var lesMinesAutour = GameObject.GetAllObjects(false).OfType<MapObjectComponent>();
		//var testGameobject = Scene.GetAllObjects( true ).Where( go => go.Name.StartsWith("GameObject"));
		//Log.Info(testGameobject.Count());
		//var minesAutour = lesMinesAutour();

		//if (minesPhysics.Count() != 0)
		//{
			//foreach ( MinePhysics mineEntity in minesPhysics )
			//{
				//var minePhysics = new MinePhysics("test",1);
				//Log.Info(mineEntity.sensMine);

				//TODO: initialiser des "entités" facile d'utilisation sur le onawake pour itérer ici

				//if ( mineEntity.sensMine != null )			
				//{
					/*Vector3 minePosition = mineEntity.Transform.Position;

					// Déterminez la distance entre le joueur et l'entité "MinePhysics"
					float distance = Vector3.DistanceBetween( playerPosition, minePosition );

					// Définissez une distance seuil pour considérer une mine comme "à proximité"
					float proximitéSeuil = 400.0f; // Vous pouvez ajuster cette valeur en fonction de vos besoins

					// Si la distance est inférieure ou égale au seuil, la mine est considérée comme à proximité
					if ( distance <= proximitéSeuil )
					{
						//Log.Info( $"Le joueur est à proximité de la mine avec ID : {mineEntity.idMine}, {mineEntity.sensMine}" );
						Log.Info( $"Le joueur est à proximité de la mine" );

						//CheckMineInit( mineEntity.idMine );
					}*/
				//}*/
			//}
		//}
	}




	void CheckMineInit(int idMine)
	{
		//Log.Info( $" Contient ? : {this.lesMines.MinesInit.Contains( idMine )}" );
		//Log.Info( $" Contient (reversed) ? : {!this.lesMines.MinesInit.Contains( idMine )}" );
		//Log.Info( $"Compteur Mines initialisés : {this.lesMines.MinesInit.Count}" );

		/*foreach ( Mine testMine in this.lesMines.LesMines)
		{
			Log.Info( $"Idmine :{testMine.IdMine}" );
		}*/

		/*if (!lesMines.MinesInit.Contains(idMine))
		//if (!lesMinesSdf.MinesInit.Contains(idMine))
		{ // la mine n'est pas init, donc on l'init
			Log.Info( $"Initialisation de la mine {idMine}" );
			var lesEntityMines = Entity.All.OfType<MinePhysics>().ToArray();
			MinePhysics entD = null;
			MinePhysics entF = null;
			//on récupère les entités mine début et mine fin
			foreach ( MinePhysics mineEntity in lesEntityMines )
			{
				//Log.Info( $"Check : {mineEntity.idMine}, {mineEntity.sensMine}" );
				if ( mineEntity.idMine == idMine )
				{
					//Log.Info( $"Bon id" );
					if ( mineEntity.sensMine == "debut" )
					{
						entD = mineEntity;
						//Log.Info( $"C'est le debut" );
					}
					if ( mineEntity.sensMine == "fin" )
					{
						entF = mineEntity;
						//Log.Info( $"C'est la fin" );
					}
				}	
			}
			if ( (entD != null) && (entF != null) )
			{
				string minesInitString = string.Join( ", ", lesMines.MinesInit );
				Log.Info( $"Mines initialisées en string : {minesInitString}" );
				lesMines.InitMine( idMine, entD, entF );
				//lesMinesSdf.InitMine(idMine,entD,entF);
			}
		} 
		else
		{
			//idk, générer les entités ?
		}*/
	}
}
