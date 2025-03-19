using System.Linq;
using System;
using Sandbox;

namespace Meteor.VehicleTool.Vehicle;

[Category( "Physics" )]
[Title( "Vehicle Controller" )]
[EditorHandle( Icon = "directions_car" )]
[Icon( "directions_car" )]
public partial class VehicleComponent : Component
{
	[Property]
	[Hide]
	[RequireComponent]
	public Rigidbody Body { get; set; }

	public float CurrentSpeed { get; private set; }

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


	protected override void OnAwake()
	{
		EnsureComponentsCreated();
	}

	protected override void OnStart()
	{
		FindWheels();
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
		CurrentSpeed = Body.Velocity.Length.InchToMeter();
		UpdateWheelLoad();
		if ( UseSteering )
			UpdateSteerAngle();
		if ( UsePowertrain )
			UpdatePowertrain();
	}

}
