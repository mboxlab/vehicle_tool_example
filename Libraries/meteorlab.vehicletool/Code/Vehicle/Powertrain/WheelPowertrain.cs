using Sandbox;
using Meteor.VehicleTool.Vehicle.Wheel;
using System;

namespace Meteor.VehicleTool.Vehicle.Powertrain;

public partial class WheelPowertrain : PowertrainComponent
{
	protected override void OnAwake()
	{
		base.OnAwake();
		Name ??= Wheel.ToString();
	}
	[Property] public WheelCollider Wheel { get; set; }

	protected override void OnStart()
	{
		_initialRollingResistance = Wheel.RollingResistanceTorque;
		_initialWheelInertia = Wheel.Inertia;
	}
	private float _initialRollingResistance;
	private float _initialWheelInertia;


	/// <summary>
	///     Adds brake torque to the wheel on top of the existing torque. Value is clamped to max brake torque.
	/// </summary>
	public void AddBrakeTorque( float torque )
	{
		Wheel.BrakeTorque = Math.Clamp( Wheel.BrakeTorque, 0, Controller.MaxBrakeTorque ) + Math.Max( torque, 0 );
	}



	public override float QueryAngularVelocity( float angularVelocity, float dt )
	{
		InputAngularVelocity = OutputAngularVelocity = Wheel.AngularVelocity;

		return OutputAngularVelocity;
	}

	public override float QueryInertia()
	{
		// Calculate the base inertia of the wheel and scale it by the inverse of the dt.
		float dtScale = Math.Clamp( Time.Delta, 0.01f, 0.05f ) / 0.005f;
		float radius = Wheel.Radius.InchToMeter();
		return 0.5f * Wheel.Mass * radius * radius * dtScale;
	}

	public void ApplyRollingResistanceMultiplier( float multiplier )
	{

		Wheel.RollingResistanceTorque = _initialRollingResistance * multiplier;
	}
	public override float ForwardStep( float torque, float inertiaSum, float dt )
	{
		InputTorque = torque;
		InputInertia = inertiaSum;

		OutputTorque = InputTorque;
		OutputInertia = _initialWheelInertia + inertiaSum;
		Wheel.MotorTorque = OutputTorque;
		Wheel.Inertia = OutputInertia;

		Wheel.AutoSimulate = false;
		Wheel.PhysUpdate( dt );

		return Math.Abs( Wheel.CounterTorque );
	}
}
