﻿using System;
using Meteor.VehicleTool.Vehicle.Wheel;
using System.Collections.Generic;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleController
{


	[Property, FeatureEnabled( "Steering", Icon = "swap_horiz" )]
	public bool UseSteering { get; set; } = true;


	[Property, Feature( "Steering" )]
	public List<WheelCollider> SteeringWheels { get; set; }

	[Property, Feature( "Steering" )]
	public float MaxSteerAngle { get; set; } = 45;

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



		VelocityAngle = -Body.Velocity.SignedAngle( WorldRotation.Forward, WorldRotation.Up );

		float targetAngle = 0;

		if ( UseAssist && CurrentSpeed > AssistStartSpeed && CarDirection > 0 && IsOnGround )
			targetAngle = VelocityAngle * AssistMultiplier;

		SetSteerAngle( MathX.Clamp( targetAngle + CurrentSteerAngle, -MaxSteerAngle, MaxSteerAngle ) );

	}

	protected virtual void SetSteerAngle( float angle )
	{

		foreach ( var item in SteeringWheels )
			item.SteerAngle = angle;
	}


}
