using System;
using System.Collections.Generic;
using Sandbox;

namespace Meteor.VehicleTool.Vehicle.Powertrain;

public partial class Clutch : PowertrainComponent
{
	protected override void OnAwake()
	{
		base.OnAwake();
		Name ??= "Clutch";
	}

	/// <summary>
	///     RPM at which automatic clutch will try to engage.
	/// </summary>

	[Property] public float EngagementRPM { get; set; } = 1200f;

	public float ThrottleEngagementOffsetRPM = 400f;

	/// <summary>
	///     Clutch engagement in range [0,1] where 1 is fully engaged clutch.
	///     Affected by Slip Torque field as the clutch can transfer [clutchEngagement * SlipTorque] Nm
	///     meaning that higher value of SlipTorque will result in more sensitive clutch.
	/// </summary>
	[Range( 0, 1 ), Property] public float ClutchInput { get; set; }

	/// <summary>
	/// Curve representing pedal travel vs. clutch engagement. Should start at 0,0 and end at 1,1.
	/// </summary>
	[Property] public Curve EngagementCurve { get; set; } = new( new List<Curve.Frame>() { new( 0, 0 ), new( 1, 1 ) } );

	public enum ClutchControlType
	{
		Automatic,
		Manual
	}

	[Property] public ClutchControlType СontrolType { get; set; } = ClutchControlType.Automatic;

	/// <summary>
	/// The RPM range in which the clutch will go from disengaged to engaged and vice versa. 
	/// E.g. if set to 400 and engagementRPM is 1000, 1000 will mean clutch is fully disengaged and
	/// 1400 fully engaged. Setting it too low might cause clutch to hunt/oscillate.
	/// </summary>
	[Property] public float EngagementRange { get; set; } = 400f;

	/// <summary>
	///     Torque at which the clutch will slip / maximum torque that the clutch can transfer.
	///     This value also affects clutch engagement as higher slip value will result in clutch
	///     that grabs higher up / sooner. Too high slip torque value combined with low inertia of
	///     powertrain components might cause instability in powertrain solver.
	/// </summary>
	[Property] public float SlipTorque { get; set; } = 500f;


	/// <summary>
	/// Amount of torque that will be passed through clutch even when completely disengaged
	/// to emulate torque converter creep on automatic transmissions.
	/// Should be higher than rolling resistance of the wheels to get the vehicle rolling.
	/// </summary>
	[Range( 0, 100f ), Property] public float CreepTorque { get; set; } = 0;

	[Property] public float CreepSpeedLimit { get; set; } = 1f;


	/// <summary>
	/// Clutch engagement based on ClutchInput and the clutchEngagementCurve
	/// </summary>
	[Property, ReadOnly]
	public float Engagement => _clutchEngagement;
	private float _clutchEngagement;

	protected override void OnStart()
	{
		base.OnStart();
		SlipTorque = Controller.Engine.EstimatedPeakTorque * 1.5f;
	}

	public override float QueryAngularVelocity( float angularVelocity, float dt )
	{
		InputAngularVelocity = angularVelocity;

		// Return input angular velocity if InputNameHash or OutputNameHash is 0
		if ( InputNameHash == 0 || OutputNameHash == 0 )
		{
			return InputAngularVelocity;
		}

		// Adjust clutch engagement based on conditions
		if ( СontrolType == ClutchControlType.Automatic )
		{
			Engine engine = Controller.Engine;

			// Engine is at risk of stalling, disconnect the clutch
			if ( engine.OutputRPM < engine.IdleRPM )
			{
				ClutchInput = 0f;
			}
			// Override engagement when shifting to smoothly engage and disengage gears
			else if ( Controller.Transmission.IsShifting )
			{
				float shiftProgress = Controller.Transmission.ShiftProgress;
				ClutchInput = MathF.Abs( MathF.Cos( MathF.PI * shiftProgress ) );
			}
			// Clutch engagement calculation for automatic clutch
			else
			{
				// Calculate engagement
				// Engage the clutch if the input spinning faster than the output, but also if vice versa.
				float throttleInput = Controller.SwappedThrottle;
				float finalEngagementRPM = EngagementRPM + ThrottleEngagementOffsetRPM * (throttleInput * throttleInput);
				float referenceRPM = MathF.Max( InputRPM, OutputRPM );
				ClutchInput = (referenceRPM - finalEngagementRPM) / EngagementRange;
				ClutchInput = Math.Clamp( ClutchInput, 0f, 1f );

				// Avoid disconnecting clutch at high speed
				if ( engine.OutputRPM > engine.IdleRPM * 1.1f && Controller.CurrentSpeed > 3f )
				{
					ClutchInput = 1f;
				}
			}
			if ( Controller.IsClutching > 0 )
			{
				ClutchInput = 1 - Controller.IsClutching;
			}

		}
		else if ( СontrolType == ClutchControlType.Manual )
		{
			// Manual clutch engagement through user input
			ClutchInput = Controller.IsClutching;
		}

		OutputAngularVelocity = InputAngularVelocity * _clutchEngagement;
		float Wout = Output.QueryAngularVelocity( OutputAngularVelocity, dt ) * _clutchEngagement;
		float Win = angularVelocity * (1f - _clutchEngagement);
		return Wout + Win;
	}

	public override float QueryInertia()
	{
		if ( OutputNameHash == 0 )
		{
			return Inertia;
		}

		float I = Inertia + Output.QueryInertia() * _clutchEngagement;
		return I;
	}


	public override float ForwardStep( float torque, float inertiaSum, float dt )
	{

		InputTorque = torque;
		InputInertia = inertiaSum;

		if ( OutputNameHash == 0 )
			return torque;


		// Get the clutch engagement point from the input value
		// Do not use the clutchEnagement directly for any calculations!
		_clutchEngagement = EngagementCurve.Evaluate( ClutchInput );
		_clutchEngagement = Math.Clamp( _clutchEngagement, 0, 1 );

		// Calculate output inertia and torque based on the clutch engagement
		// Assume half of the inertia is on the input plate and the other half is on the output clutch plate.
		float halfClutchInertia = Inertia * 0.5f;
		OutputInertia = (inertiaSum + halfClutchInertia) * _clutchEngagement + halfClutchInertia;

		// Allow the torque output to be only up to the slip torque valu
		float outputTorqueClamp = SlipTorque * _clutchEngagement;
		OutputTorque = InputTorque;
		OutputTorque = Math.Clamp( OutputTorque, 0, outputTorqueClamp );
		float slipOverflowTorque = -Math.Min( outputTorqueClamp - OutputTorque, 0 );

		// Apply the creep torque commonly caused by torque converter drag in automatic transmissions
		ApplyCreepTorque( ref OutputTorque, CreepTorque );

		// Send the torque downstream
		float returnTorque = _output.ForwardStep( OutputTorque, OutputInertia, dt ) * _clutchEngagement;

		// Clamp the return torque to the slip torque of the clutch once again
		returnTorque = Math.Clamp( returnTorque, -SlipTorque, SlipTorque );

		// Torque returned to the input is a combination of torque returned by the powertrain and the torque that 
		// was possibly never sent downstream
		return returnTorque + slipOverflowTorque;
	}


	private void ApplyCreepTorque( ref float torque, float creepTorque )
	{
		// Apply creep torque to forward torque
		if ( creepTorque != 0 && Controller.Engine.IsRunning && Controller.CurrentSpeed < CreepSpeedLimit )
		{
			bool torqueWithinCreepRange = torque < creepTorque && torque > -creepTorque;

			if ( torqueWithinCreepRange )
			{
				torque = creepTorque;
			}
		}

	}

}
