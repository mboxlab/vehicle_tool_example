using System;
using Sandbox;
using Sandbox.Utility;
namespace Meteor.VehicleTool.Vehicle.Wheel;

public partial class WheelCollider
{
	private float minSuspensionLength = 3;
	private float maxSuspensionLength = 3;
	private float suspensionTotalLength;

	public float Load { get; private set; }

	[Group( "Spring" ), Property] public float MinSuspensionLength { get => minSuspensionLength; set { minSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Group( "Spring" ), Property] public float MaxSuspensionLength { get => maxSuspensionLength; set { maxSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Group( "Spring" ), Property] public float SuspensionStiffness { get; set; } = 16000.0f;


	public Vector3 SuspensionForce { get; private set; }

	/// <summary>
	/// The speed coefficient of the spring / suspension extension when not on the ground. how fast the wheels extend when in the air.
	/// The setting of 1 will result in suspension fully extending in 1 second, 2 in 0.5s, 3 in 0.333s, etc.
	/// Recommended value is 6-10.
	/// </summary>

	[Group( "Spring" ), Property, Range( 0.01f, 30f )]
	public float SuspensionExtensionSpeed { get; set; } = 6f;


	public float SuspensionLength { get; private set; }

	private void UpdateTotalSuspensionLength()
		=> suspensionTotalLength = maxSuspensionLength + minSuspensionLength;

	public Vector3 GetCenter()
		=> WorldTransform.PointToWorld( Vector3.Down * (SuspensionLength - minSuspensionLength) );

	private float prevlength = 0;
	private void UpdateSuspension()
	{
		prevlength = SuspensionLength;
		if ( !IsGrounded )
		{
			SuspensionLength += Time.Delta * suspensionTotalLength * SuspensionExtensionSpeed;
		}
		else
		{
			SuspensionLength = GroundHit.Fraction * suspensionTotalLength;
		}


		if ( SuspensionLength <= 0f )
		{
			SuspensionLength = 0;
		}
		else if ( SuspensionLength >= suspensionTotalLength )
		{
			SuspensionLength = suspensionTotalLength;
			IsGrounded = false;
		}
		if ( IsGrounded )
		{
			//var worldVelocity = (SuspensionLength - prevlength) / Time.Delta; // CarBody.GetVelocityAtPoint( GroundHit.StartPosition + CarBody.MassCenter ) - GroundVelocity;

			//var localVel = worldVelocity.Dot( GroundHit.Normal ).InchToMeter();

			var suspensionCompression = (suspensionTotalLength - SuspensionLength) / suspensionTotalLength;

			var dampingForce = CalculateDamperForce( (SuspensionLength - prevlength).InchToMeter() / Time.Delta ) + GroundVelocity.z.InchToMeter();

			var springForce = SuspensionStiffness * suspensionCompression;

			Load = Math.Max( 0, springForce - dampingForce );


			SuspensionForce = GroundHit.Normal * (Load.MeterToInch() * Time.Delta);

			CarBody.ApplyImpulseAt( WorldPosition, SuspensionForce );

		}
		else
		{
			Load = 0;
		}
	}


	public float CalculateDamperForce( in float velocity )
	{
		if ( velocity > 0f )
			return CalculateBumpForce( velocity );

		return CalculateReboundForce( velocity );
	}


	/// <summary>
	///     Bump rate of the damper in Ns/m.
	/// </summary>
	[Property, Group( "Damper" )] public float BumpRate { get; set; } = 3000f;

	/// <summary>
	///     Rebound rate of the damper in Ns/m.
	/// </summary>
	[Property, Group( "Damper" )] public float ReboundRate { get; set; } = 3000f;

	/// <summary>
	/// Slow bump slope for the damper, used for damper velocity below bumpDivisionVelocity.
	/// Value of 1 means that the bump force increases proportionally to the compression velocity.
	/// </summary>
	[Property, Group( "Damper" )]
	[Range( 0f, 3f )]
	public float SlowBump { get; set; } = 1.4f;

	/// <summary>
	/// Fast bump slope for the damper, used for damper velocity above bumpDivisionVelocity.
	/// Value of 1 means that the bump force increases proportionally to the compression velocity.
	/// </summary>
	[Property, Group( "Damper" )]
	[Range( 0f, 3f )]
	public float FastBump { get; set; } = 0.6f;

	/// <summary>
	/// Damper velocity at which the bump slope switches from the slowBump to fastBump.
	/// </summary>
	[Property, Group( "Damper" )]
	[Range( 0f, 0.2f )]
	public float BumpDivisionVelocity { get; set; } = 0.06f;

	/// <summary>
	/// Slow rebound slope for the damper, used for damper velocity below reboundDivisionVelocity.
	/// Value of 1 means that the rebound force increases proportionally to the extension velocity.
	/// </summary>
	[Property, Group( "Damper" )]
	[Range( 0f, 3f )]
	public float SlowRebound { get; set; } = 1.6f;

	/// <summary>
	/// Fast rebound slope for the damper, used for damper velocity above reboundDivisionVelocity.
	/// Value of 1 means that the rebound force increases proportionally to the extension velocity.
	/// </summary>
	[Property, Group( "Damper" )]
	[Range( 0f, 3f )]
	public float FastRebound { get; set; } = 0.6f;

	/// <summary>
	/// Damper velocity at which the rebound slope switches from the slowRebound to fastRebound.
	/// </summary>
	[Property, Group( "Damper" )]
	[Range( 0f, 0.2f )]
	public float ReboundDivisionVelocity { get; set; } = 0.05f;


	private float CalculateBumpForce( in float velocity )
	{
		if ( velocity < 0f ) return 0f; // We are in rebound, return.

		float x = velocity;
		float y;
		if ( x < BumpDivisionVelocity )
		{
			y = x * SlowBump;
		}
		else
		{
			y = BumpDivisionVelocity * SlowBump + (x - BumpDivisionVelocity) * FastBump;
		}
		return y * BumpRate;
	}

	private float CalculateReboundForce( in float velocity )
	{
		if ( velocity > 0f ) return 0f; // We are in bump, return.

		float x = -velocity;
		float y;
		if ( x < ReboundDivisionVelocity )
		{
			y = x * SlowRebound;
		}
		else
		{
			y = ReboundDivisionVelocity * SlowRebound + (x - ReboundDivisionVelocity) * FastRebound;
		}
		return -y * ReboundRate;
	}
}
