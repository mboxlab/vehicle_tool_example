using System;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle.Powertrain;

public abstract class PowertrainComponent : Component
{

	[Property] public VehicleController Controller;
	/// <summary>
	///     Name of the component. Only unique names should be used on the same vehicle.
	/// </summary>
	[Property] public string Name { get; set; }


	/// <summary>
	///     Angular inertia of the component. Higher inertia value will result in a powertrain that is slower to spin up, but
	///     also slower to spin down. Too high values will result in (apparent) sluggish response while too low values will
	///     result in vehicle being easy to stall and possible powertrain instability / glitches.
	/// </summary>
	[Property, Range( 0.0002f, 2f )] public float Inertia { get; set; } = 0.05f;

	public float InputTorque;
	public float OutputTorque;
	public float InputAngularVelocity;
	public float OutputAngularVelocity;
	public float InputInertia;
	public float OutputInertia;

	/// <summary>
	///     Input component. Set automatically.
	/// </summary>
	[Property]
	public PowertrainComponent Input
	{
		get { return _input; }
		set
		{
			if ( value == null || value == this )
			{
				_input = null;
				InputNameHash = 0;
				return;
			}

			_input = value;
			InputNameHash = _input.GetHashCode();
		}
	}

	protected PowertrainComponent _input;
	public int InputNameHash;



	/// <summary>
	///     The PowertrainComponent this component will output to.
	/// </summary>
	[Property]
	public PowertrainComponent Output
	{
		get { return _output; }
		set
		{
			if ( value == this )
			{
				Log.Warning( $"{Name}: PowertrainComponent Output can not be self." );
				OutputNameHash = 0;
				_output = null;
				return;
			}

			_output = value;
			if ( _output != null )
			{
				_output.Input = this;
				OutputNameHash = _output.GetHashCode();
			}
			else
			{
				OutputNameHash = 0;
			}
		}

	}
	protected PowertrainComponent _output;
	public int OutputNameHash { get; private set; }


	/// <summary>
	///    Input shaft RPM of component.
	/// </summary>
	[Property, ReadOnly]
	public float InputRPM => InputAngularVelocity.AngularVelocityToRPM();


	/// <summary>
	///    Output shaft RPM of component.
	/// </summary>
	[Property, ReadOnly]
	public float OutputRPM => OutputAngularVelocity.AngularVelocityToRPM();


	public virtual float QueryAngularVelocity( float angularVelocity, float dt )
	{
		InputAngularVelocity = angularVelocity;

		if ( OutputNameHash == 0 )
		{
			return angularVelocity;
		}

		OutputAngularVelocity = angularVelocity;
		return _output.QueryAngularVelocity( OutputAngularVelocity, dt );
	}

	public virtual float QueryInertia()
	{
		if ( OutputNameHash == 0 )
			return Inertia;

		float Ii = Inertia;
		float Ia = _output.QueryInertia();

		return Ii + Ia;
	}
	public virtual float ForwardStep( float torque, float inertiaSum, float dt )
	{
		InputTorque = torque;
		InputInertia = inertiaSum;

		if ( OutputNameHash == 0 )
			return torque;

		OutputTorque = InputTorque;
		OutputInertia = inertiaSum + Inertia;
		return _output.ForwardStep( OutputTorque, OutputInertia, dt );
	}

	public static float TorqueToPowerInKW( in float angularVelocity, in float torque )
	{
		// Power (W) = Torque (Nm) * Angular Velocity (rad/s)
		float powerInWatts = torque * angularVelocity;

		// Convert power from watts to kilowatts
		float powerInKW = powerInWatts / 1000f;

		return powerInKW;
	}

	public static float PowerInKWToTorque( in float angularVelocity, in float powerInKW )
	{
		// Convert power from kilowatts to watts
		float powerInWatts = powerInKW * 1000f;

		// Torque (Nm) = Power (W) / Angular Velocity (rad/s)
		float absAngVel = Math.Abs( angularVelocity );
		float clampedAngularVelocity = (absAngVel > -1f && absAngVel < 1f) ? 1f : angularVelocity;
		float torque = powerInWatts / clampedAngularVelocity;
		return torque;
	}

	public float CalculateOutputPowerInKW()
	{
		return GetPowerInKW( OutputTorque, OutputAngularVelocity );
	}
	public static float GetPowerInKW( in float torque, in float angularVelocity )
	{
		// Power (W) = Torque (Nm) * Angular Velocity (rad/s)
		float powerInWatts = torque * angularVelocity;

		// Convert power from watts to kilowatts
		float powerInKW = powerInWatts / 1000f;

		return powerInKW;
	}
}
