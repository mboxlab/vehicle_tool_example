using System;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleController
{
	[Property]
	[FeatureEnabled( "Input", Icon = "sports_esports", Description = "Default controls using AnalogMove and AnalogLook." )]
	public bool UseInputControls { get; set; } = true;

	private float throttle;
	private float brakes;
	private float steerAngle;
	private float handbrake;
	public bool IsInputSwapped => Transmission.Gear < 0;

	/// <summary>
	///     Convenience function for setting throttle/brakes as a single value.
	///     Use Throttle/Brake axes to apply throttle and braking separately.
	///     If the set value is larger than 0 throttle will be set, else if value is less than 0 brake axis will be set.
	/// </summary>
	[Sync]
	public float VerticalInput
	{
		get => throttle - Brakes;
		set
		{
			float clampedValue = Math.Clamp( value, -1, 1 );

			if ( value > 0 )
			{
				throttle = clampedValue;
				brakes = 0;
			}
			else
			{
				throttle = 0;
				brakes = -clampedValue;
			}
		}
	}

	/// <summary>
	///     Throttle axis.
	///     For combined throttle/brake input use 'VerticalInput' instead.
	/// </summary>
	[Sync]
	public float Throttle
	{
		get => throttle;
		set => throttle = Math.Clamp( value, 0, 1 );
	}

	/// <summary>
	///     Brake axis.
	///     For combined throttle/brake input use 'VerticalInput' instead.
	/// </summary>
	[Sync]
	public float Brakes
	{
		get => brakes;
		set => brakes = Math.Clamp( value, 0, 1 );
	}

	/// <summary>
	///     Steering axis.
	/// </summary>
	[Sync]
	public float SteeringAngle
	{
		get => steerAngle;
		set => steerAngle = Math.Clamp( value, -1, 1 );
	}
	[Sync]
	public float Handbrake
	{
		get => handbrake;
		set => handbrake = Math.Clamp( value, 0, 1 );
	}

	[Sync]
	public bool IsShiftingDown { get; private set; }


	[Sync]
	public bool IsShiftingUp { get; private set; }

	[Sync]
	public float IsClutching { get; private set; }

	public float SwappedThrottle => IsInputSwapped ? Brakes : Throttle;
	public float SwappedBrakes => IsInputSwapped ? Throttle : Brakes;

	private void UpdateInput()
	{
		VerticalInput = Input.AnalogMove.x;
		Handbrake = Input.Down( "Jump" ) ? 1 : 0;

		SteeringAngle = Input.AnalogMove.y;

		IsClutching = (Input.Down( "Run" ) || Input.Down( "Jump" )) ? 1 : 0;

		IsShiftingUp |= Input.Pressed( "Attack1" );
		IsShiftingDown |= Input.Pressed( "Attack2" );

	}

	/// <summary>
	/// When true we'll move the camera around using the mouse
	/// </summary>
	[Property]
	[Feature( "Input" )]
	[Category( "Eye Angles" )]
	public bool UseLookControls { get; set; } = true;

	/// <summary>
	/// Allows modifying the eye angle sensitivity. Note that player preference sensitivity is already automatically applied, this is just extra.
	/// </summary>
	[Property]
	[Feature( "Input" )]
	[Category( "Eye Angles" )]
	[Range( 0f, 2f, 0.01f, true, true )]
	public float LookSensitivity { get; set; } = 1f;

	[Property]
	[Feature( "Input" )]
	[Category( "Eye Angles" )]
	[Range( 0f, 90f, 0.01f, true, true )]
	public float MaxPitch { get; set; } = 90f;

	[Property]
	[Feature( "Input" )]
	[Category( "Eye Angles" )]
	[Range( 0f, 90f, 0.01f, true, true )]
	public float MinPitch { get; set; } = 90f;

	private TimeSince MouseMovedTime;
	private float centerStrength;

	private void UpdateEyeAngles()
	{
		Angles input = Input.AnalogLook;
		input *= LookSensitivity;

		if ( input.pitch != 0 || input.yaw != 0 )
		{
			centerStrength = 0;
			MouseMovedTime = -0.8f;
		}


		Angles eyeAngles = EyeAngles;
		eyeAngles += input;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -MinPitch, MaxPitch );

		var vehAngles = WorldRotation.Angles();
		float decay = Math.Clamp( (CurrentSpeed - 2) * 0.03f, 0f, 1f ) * 7f * centerStrength;
		eyeAngles.pitch = MathM.ExpDecayAngle( eyeAngles.pitch, vehAngles.pitch + (ThirdPerson ? 15f : 0f), decay, Time.Delta );
		eyeAngles.yaw = MathM.ExpDecayAngle( eyeAngles.yaw, vehAngles.yaw + CurrentSteerAngle / 10f + (CarDirection == -1 ? 180f : 0f), decay, Time.Delta );

		if ( MouseMovedTime > 1f )
			centerStrength = MathM.ExpDecay( centerStrength, 1, 2, Time.Delta );


		EyeAngles = eyeAngles;
	}
}
