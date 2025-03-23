using Sandbox;

namespace Meteor.VehicleTool.Vehicle;

[Category( "Physics" )]
[Title( "Vehicle Controller" )]
[EditorHandle( Icon = "directions_car" )]
[Icon( "directions_car" )]
public partial class VehicleController : Component
{
	[Property]
	[Hide]
	[RequireComponent]
	public Rigidbody Body { get; set; }

	public float CurrentSpeed { get; private set; }
	public Vector3 LocalVelocity { get; private set; }

	private bool _showRigidBodyComponent;

	[Property]
	[Group( "Components" )]
	[Title( "Show Rigidbody" )]
	public bool ShowRigidbodyComponent
	{
		get => _showRigidBodyComponent;
		set
		{
			_showRigidBodyComponent = value;
			if ( Body.IsValid() )
				Body.Flags = Body.Flags.WithFlag( ComponentFlags.Hidden, !value );
		}
	}

	protected override void OnDisabled()
	{
		VerticalInput = 0;
		Handbrake = 0;
		SteeringAngle = 0;
		CurrentSteerAngle = 0;
		if ( UseSteering )
			SetSteerAngle( 0 );

		foreach ( var item in Wheels )
		{
			item.BrakeTorque = 200f;
			item.MotorTorque = 0;
		}
	}

	protected override void OnAwake()
	{
		EnsureComponentsCreated();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		UpdateInput();

		if ( UseCameraControls )
			UpdateCameraPosition();

		if ( UseLookControls )
			UpdateEyeAngles();

	}

	protected override void OnFixedUpdate()
	{
		LocalVelocity = WorldTransform.PointToLocal( Body.GetVelocityAtPoint( WorldPosition ) + WorldPosition );
		CurrentSpeed = Body.Velocity.Length.InchToMeter();
		if ( UseSteering )
			UpdateSteerAngle();
		if ( UsePowertrain )
			UpdateBrakes();
	}

}
