using Sandbox;
using System;

namespace Meteor.VehicleTool.Vehicle.Powertrain;

public partial class Differential : PowertrainComponent
{


	/// <param name="T">Input torque</param>
	/// <param name="Wa">Angular velocity of the outputA</param>
	/// <param name="Wb">Angular velocity of the outputB</param>
	/// <param name="Ia">Inertia of the outputA</param>
	/// <param name="Ib">Inertia of the outputB</param>
	/// <param name="dt">Time step</param>
	/// <param name="biasAB">Torque bias between outputA and outputB. 0 = all torque goes to A, 1 = all torque goes to B</param>
	/// <param name="stiffness">Stiffness of the limited slip or locked differential</param>
	/// <param name="powerRamp">Stiffness under power</param>
	/// <param name="coastRamp">Stiffness under braking</param>
	/// <param name="slipTorque">Slip torque of the limited slip differential</param>
	/// <param name="Ta">Torque output towards outputA</param>
	/// <param name="Tb">Torque output towards outputB</param>
	public delegate void SplitTorque( float T, float Wa, float Wb, float Ia, float Ib, float dt, float biasAB,
		float stiffness, float powerRamp, float coastRamp, float slipTorque, out float Ta, out float Tb );

	protected override void OnAwake()
	{
		base.OnAwake();
		Name ??= "Differential";
		AssignDifferentialDelegate();
	}
	public enum DifferentialType
	{
		Open,
		Locked,
		LimitedSlip,
	}

	/// <summary>
	///     Differential type.
	/// </summary>
	[Property]
	public DifferentialType Type
	{
		get { return _differentialType; }
		set
		{
			_differentialType = value;
			AssignDifferentialDelegate();
		}
	}
	private DifferentialType _differentialType;

	/// <summary>
	///     Torque bias between left (A) and right (B) output in [0,1] range.
	/// </summary>
	[Property, Range( 0, 1 )] public float BiasAB { get; set; } = 0.5f;

	/// <summary>
	///     Stiffness of locking differential [0,1]. Higher value
	///     will result in lower difference in rotational velocity between left and right wheel.
	///     Too high value might introduce slight oscillation due to drivetrain windup and a vehicle that is hard to steer.
	/// </summary>
	[Property, Range( 0, 1 ), HideIf( nameof( _differentialType ), DifferentialType.Open )] public float Stiffness { get; set; } = 0.5f;

	/// <summary>
	/// Stiffness of the LSD differential under acceleration.
	/// </summary>
	[Property, Range( 0, 1 ), ShowIf( nameof( _differentialType ), DifferentialType.LimitedSlip )] public float PowerRamp { get; set; } = 1f;

	/// <summary>
	/// Stiffness of the LSD differential under braking.
	/// </summary>
	[Property, Range( 0, 1 ), ShowIf( nameof( _differentialType ), DifferentialType.LimitedSlip )] public float CoastRamp { get; set; } = 0.5f;


	/// <summary>
	///     Second output of differential.
	/// </summary>
	[Property]
	public PowertrainComponent OutputB
	{
		get { return _outputB; }
		set
		{
			if ( value == this )
			{
				Log.Warning( $"{Name}: PowertrainComponent Output can not be self." );
				OutputBNameHash = 0;
				_output = null;
				return;
			}
			if ( _outputB != null )
			{
				_outputB.InputNameHash = 0;
				_outputB.Input = null;
			}

			_outputB = value;

			if ( _outputB != null )
			{
				_outputB.Input = this;
				OutputBNameHash = _outputB.ToString().GetHashCode();
			}
			else
			{
				OutputBNameHash = 0;
			}
		}
	}

	protected PowertrainComponent _outputB;
	public int OutputBNameHash;


	/// <summary>
	///     Slip torque of limited slip differentials.
	/// </summary>
	[Property, ShowIf( nameof( _differentialType ), DifferentialType.LimitedSlip )] public float SlipTorque { get; set; } = 400f;

	/// <summary>
	/// Function delegate that will be used to split the torque between output(A) and outputB.
	/// </summary>
	public SplitTorque SplitTorqueDelegate;

	private void AssignDifferentialDelegate()
	{
		SplitTorqueDelegate = _differentialType switch
		{
			DifferentialType.Open => OpenDiffTorqueSplit,
			DifferentialType.Locked => LockingDiffTorqueSplit,
			DifferentialType.LimitedSlip => LimitedDiffTorqueSplit,
			_ => OpenDiffTorqueSplit,
		};
	}

	public static void OpenDiffTorqueSplit( float T, float Wa, float Wb, float Ia, float Ib, float dt, float biasAB,
	float stiffness, float powerRamp, float coastRamp, float slipTorque, out float Ta, out float Tb )
	{
		Ta = T * (1f - biasAB);
		Tb = T * biasAB;
	}


	public static void LockingDiffTorqueSplit( float T, float Wa, float Wb, float Ia, float Ib, float dt, float biasAB,
		float stiffness, float powerRamp, float coastRamp, float slipTorque, out float Ta, out float Tb )
	{
		float Isum = Ia + Ib;

		float Wtarget = Ia / Isum * Wa + Ib / Isum * Wb;
		float TaCorrective = (Wtarget - Wa) * Ia / dt;
		TaCorrective *= stiffness;
		float TbCorrective = (Wtarget - Wb) * Ib / dt;
		TbCorrective *= stiffness;

		float Tabs = T < 0 ? -T : T;
		TbCorrective = TbCorrective > 0 ?
			(TbCorrective > Tabs ? Tabs : TbCorrective) :
			(TbCorrective < -Tabs ? -Tabs : TbCorrective);

		float biasA = 0.5f + (Wb - Wa) * 10f * stiffness;
		biasA = biasA < 0 ? 0 : biasA > 1f ? 1f : biasA;

		Ta = T * biasA + TaCorrective;
		Tb = T * (1f - biasA) + TbCorrective;
	}


	public static void LimitedDiffTorqueSplit( float T, float Wa, float Wb, float Ia, float Ib, float dt, float biasAB,
		float stiffness, float powerRamp, float coastRamp, float slipTorque, out float Ta, out float Tb )
	{
		if ( Wa < 0 || Wb < 0 )
		{
			Ta = T * (1f - biasAB);
			Tb = T * biasAB;
			return;
		}

		//Wa and Wb are positive at this point
		float c = T > 0 ? powerRamp : coastRamp;
		float Wtotal = (Wa < 0 ? -Wa : Wa) + (Wb < 0 ? -Wb : Wb);
		float slip = Wtotal == 0 ? 0 : (Wa - Wb) / Wtotal;
		float Td = slip * stiffness * c * slipTorque;

		float Tabs = Math.Abs( T );
		Td = Math.Clamp( Td, -Tabs * 0.5f, Tabs * 0.5f );

		Ta = T * 0.5f - Td;
		Tb = T * 0.5f + Td;
	}
	public override float QueryAngularVelocity( float angularVelocity, float dt )
	{
		InputAngularVelocity = angularVelocity;

		if ( OutputNameHash == 0 || OutputBNameHash == 0 )
			return angularVelocity;

		OutputAngularVelocity = InputAngularVelocity;
		float Wa = _output.QueryAngularVelocity( OutputAngularVelocity, dt );
		float Wb = _outputB.QueryAngularVelocity( OutputAngularVelocity, dt );
		return (Wa + Wb) * 0.5f;
	}

	public override float QueryInertia()
	{
		if ( OutputNameHash == 0 || OutputBNameHash == 0 )
			return Inertia;

		float Ia = _output.QueryInertia();
		float Ib = _outputB.QueryInertia();
		float I = Inertia + (Ia + Ib);

		return I;
	}


	public override float ForwardStep( float torque, float inertiaSum, float dt )
	{
		InputTorque = torque;
		InputInertia = inertiaSum;

		if ( OutputNameHash == 0 || OutputBNameHash == 0 )
			return torque;

		float Wa = _output.QueryAngularVelocity( OutputAngularVelocity, dt );
		float Wb = _outputB.QueryAngularVelocity( OutputAngularVelocity, dt );

		float Ia = _output.QueryInertia();
		float Ib = _outputB.QueryInertia();
		SplitTorqueDelegate.Invoke( torque, Wa, Wb, Ia, Ib, dt, BiasAB, Stiffness, PowerRamp,
								   CoastRamp, SlipTorque, out float Ta, out float Tb );

		float outAInertia = inertiaSum * 0.5f + Ia;
		float outBInertia = inertiaSum * 0.5f + Ib;

		OutputTorque = Ta + Tb;
		OutputInertia = outAInertia + outBInertia;

		return _output.ForwardStep( Ta, outAInertia, dt ) + _outputB.ForwardStep( Tb, outBInertia, dt );
	}


}
