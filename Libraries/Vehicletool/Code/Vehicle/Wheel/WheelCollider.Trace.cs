using Sandbox;
namespace Meteor.VehicleTool.Vehicle.Wheel;

public partial class WheelCollider
{
	private static Rotation CylinderOffset => Rotation.FromRoll( 90 );
	[Property] public TagSet IgnoredTags { get; set; }
	public bool IsGrounded { get; private set; }

	private GroundHit GroundHit;
	private void DoTrace()
	{
		var rot = CarBody.WorldRotation;
		var startPos = WorldPosition + rot.Up * MinSuspensionLength;
		var endPos = WorldPosition + rot.Down * MaxSuspensionLength;

		GroundHit = new( Scene.Trace
				.IgnoreGameObjectHierarchy( Controller.GameObject )
				.FromTo( startPos, endPos )
				.Cylinder( Width, Radius )
				.Rotated( WorldRotation * CylinderOffset )
				.UseRenderMeshes( false )
				.UseHitPosition( false )
				.WithoutTags( IgnoredTags )
				.Run() );

		IsGrounded = GroundHit.Hit;
	}

}
