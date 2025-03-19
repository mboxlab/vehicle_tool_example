using System;
using Meteor.VehicleTool.Vehicle.Wheel;
using System.Collections.Generic;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleComponent
{


	[Property, FeatureEnabled( "Steering", Icon = "swap_horiz" )]
	public bool UseSteering { get; set; } = true;


	[Property, Feature( "Steering" )]
	public List<WheelCollider> SteeringWheels { get; set; }

	[Property, Feature( "Steering" )]
	public float MaxSteerAngle { get; private set; } = 45;

	[Property, Feature( "Steering" ), Group( "Steer Angle Multiplier" )]
	public bool UseSteerAngleMultiplier { get; set; } = true;

	[Property, Feature( "Steering" ), ShowIf( nameof( UseSteerAngleMultiplier ), true ), Group( "Steer Angle Multiplier" )]
	public float MaxSpeedForMinAngleMultiplier { get; set; } = 100;

	[Property, Feature( "Steering" ), ShowIf( nameof( UseSteerAngleMultiplier ), true ), Group( "Steer Angle Multiplier" )]
	public float MinSteerAngleMultiplier { get; set; } = 0.05f;

	[Property, Feature( "Steering" ), ShowIf( nameof( UseSteerAngleMultiplier ), true ), Group( "Steer Angle Multiplier" )]
	public float MaxSteerAngleMultiplier { get; set; } = 1f;

	public float CurrentSteerAngle { get; private set; }
	public float VelocityAngle { get; private set; }
	public int CarDirection { get { return CurrentSpeed < 1 ? 0 : (VelocityAngle < 90 && VelocityAngle > -90 ? 1 : -1); } }


	[Property, Feature( "Steering" ), Group( "Steer Angle Assist" )]
	public bool UseAssist { get; set; } = true;

	[Property, Feature( "Steering" ), ShowIf( nameof( UseAssist ), true ), Group( "Steer Angle Assist" )]
	public float AssistMultiplier { get; set; } = 0.8f;

	[Property, Feature( "Steering" ), ShowIf( nameof( UseAssist ), true ), Group( "Steer Angle Assist" )]
	public float AssistStartSpeed { get; set; } = 5f;


	protected virtual void UpdateSteerAngle()
	{
		float targetSteerAngle = SteeringAngle * MaxSteerAngle;

		if ( UseSteerAngleMultiplier )
			targetSteerAngle *= Math.Clamp( 1 - CurrentSpeed / MaxSpeedForMinAngleMultiplier, MinSteerAngleMultiplier, MaxSteerAngleMultiplier );

		CurrentSteerAngle = MathX.Lerp( CurrentSteerAngle, targetSteerAngle, Time.Delta * 5f );



		VelocityAngle = -SignedAngle( Body.Velocity, WorldRotation.Forward, WorldRotation.Up );

		float targetAngle = 0;

		if ( UseAssist && CurrentSpeed > AssistStartSpeed && CarDirection > 0 && IsOnGround )
			targetAngle = VelocityAngle * AssistMultiplier;

		targetAngle = MathX.Clamp( targetAngle + CurrentSteerAngle, -MaxSteerAngle, MaxSteerAngle );

		foreach ( var item in SteeringWheels )
			item.SteerAngle = targetAngle;

	}

	public static float SignedAngle( Vector3 from, Vector3 to, Vector3 axis )
	{
		float unsignedAngle = Vector3.GetAngle( from, to );

		float cross_x = from.y * to.z - from.z * to.y;
		float cross_y = from.z * to.x - from.x * to.z;
		float cross_z = from.x * to.y - from.y * to.x;
		float sign = MathF.Sign( axis.x * cross_x + axis.y * cross_y + axis.z * cross_z );
		return unsignedAngle * sign;
	}



}
