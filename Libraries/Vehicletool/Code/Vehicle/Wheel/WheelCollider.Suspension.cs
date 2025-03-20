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

	[Group( "Suspension" ), Property] public float MinSuspensionLength { get => minSuspensionLength; set { minSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Group( "Suspension" ), Property] public float MaxSuspensionLength { get => maxSuspensionLength; set { maxSuspensionLength = value; UpdateTotalSuspensionLength(); } }
	[Group( "Suspension" ), Property] public float SuspensionStiffness { get; set; } = 33000;
	[Group( "Suspension" ), Property] public float SuspensionDamping { get; set; } = 6540;

	private void UpdateTotalSuspensionLength()
		=> suspensionTotalLength = maxSuspensionLength + minSuspensionLength;

	public Vector3 GetCenter()
		=> GroundHit.EndPosition;

	private void UpdateSuspension()
	{
		if ( !IsGrounded )
			return;

		var worldVelocity = CarBody.GetVelocityAtPoint( GroundHit.Point + CarBody.PhysicsBody.LocalMassCenter ) - GroundVelocity;

		var localVel = worldVelocity.Dot( GroundHit.Normal ).InchToMeter();

		var suspensionCompression = 1 - Easing.ExpoOut( GroundHit.Fraction );
		var dampingForce = -SuspensionDamping * localVel;
		var springForce = SuspensionStiffness * suspensionCompression;

		Load = Math.Max( 0, dampingForce + springForce );


		var suspensionForce = GroundHit.Normal * (Load.MeterToInch() * Time.Delta);

		CarBody.ApplyImpulseAt( WorldPosition, suspensionForce );
	}
}
