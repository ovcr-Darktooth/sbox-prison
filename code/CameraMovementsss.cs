using Sandbox;

public sealed class CameraMovementsss : Component
{
	[Property]
	public PlayerComponent Player {get;set;}

	[Property]
	public GameObject Body {get; set;}
	[Property]
	public GameObject Head {get; set;}
	[Property]
	public float Distance {get;set;} = 100f;

	public bool IsFirstPerson => Distance == 0f;

	public Vector3 CurrentOffset = Vector3.Zero;

	public CameraComponent Camera;
	public ModelRenderer BodyRenderer;

	protected override void OnAwake()
	{

		if ( IsProxy )
			return;

		//base.OnAwake();
		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;
		//base.OnUpdate();
		// Rotate the head based on mouse movement 
		var eyeAngles = Head.Transform.Rotation.Angles(); 
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp(-89.9f, 89.9f );
		Head.Transform.Rotation = eyeAngles.ToRotation();

		var targetOffset =  Vector3.Zero;
		//if (Player.IsCrouching) targetOffset += Vector3.Down * 32f;
		CurrentOffset = Vector3.Lerp(CurrentOffset, targetOffset, Time.Delta * 10f);

		// Set the position of the camera
		if ( Camera is not null)
		{
			var campos = Head.Transform.Position + CurrentOffset;
			if (!IsFirstPerson)
			{

				var camForward = eyeAngles.ToRotation().Forward; 
				var camTrace = Scene.Trace.Ray( campos, campos - (camForward * Distance))
					.WithoutTags("player", "trigger" )
					.Run();
				
				// Perform a trace backwards to see where we can safely place the camera 
				if (camTrace.Hit)
				{
					campos = camTrace.HitPosition + camTrace.Normal;
				}
				else
				{
					campos = camTrace.EndPosition;
				}
				// Show the body if we're not in the first person 
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				// Hide the body if we're in the first person 
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}

			Camera.Transform.Position = campos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();
		}
	}
}