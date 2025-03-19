using System;
using System.Collections.Generic;
using Meteor.VehicleTool.Vehicle.Wheel;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleComponent
{
	[Property, FeatureEnabled( "Powertrain", Icon = "power" )]
	public bool UsePowertrain { get; set; } = true;


	[Property, Feature( "Powertrain" ), Category( "Engine" )]
	public List<WheelCollider> MotorWheels { get; set; }

	[Property, Feature( "Powertrain" ), Category( "Engine" )]
	public float MaxPower { get; set; } = 300;


	[Property, Feature( "Powertrain" ), Category( "Engine" )]
	public float MaxSpeed { get; set; } = 150;



	[Property, Feature( "Powertrain" ), Category( "Brake" )]
	public List<WheelCollider> HandBrakeWheels { get; set; }

	[Property, Feature( "Powertrain" ), Category( "Brake" )]
	public float HandBrakePower { get; set; } = 10000f;

	public void UpdatePowertrain()
	{

		var perc = VerticalInput * MaxPower / Math.Max( 0.1f, CurrentSpeed / MaxSpeed );
		foreach ( var item in MotorWheels )
			item.MotorTorque = perc;

		var brake = Handbrake * HandBrakePower;
		foreach ( var item in HandBrakeWheels )
			item.BrakeTorque = brake;

	}
}
