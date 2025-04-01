using Sandbox;
namespace Meteor.VehicleTool.Vehicle.Wheel;

public partial class WheelCollider
{
	private Rotation TransformRotationSteer;

	[Property, Range( -90, 90 ), Sync] public float SteerAngle { get; set; }

	private void UpdateSteer()
	{
		var steerRotation = Rotation.FromAxis( Vector3.Up, SteerAngle );
		TransformRotationSteer = WorldRotation * steerRotation;

		velocityRotation *= Rotation.From( axleAngle, 0, 0 );
		RendererObject.LocalRotation = Rotation.FromYaw( SteerAngle ) * velocityRotation;

	}
}
