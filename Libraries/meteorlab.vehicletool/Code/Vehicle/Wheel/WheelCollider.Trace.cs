using System;
using System.Collections.Generic;
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
				.Rotated( TransformRotationSteer * CylinderOffset )
				.UseRenderMeshes( false )
				.UseHitPosition( false )
				.WithoutTags( IgnoredTags )
				.Run() );

		IsGrounded = GroundHit.Hit;
	}

	public static Model CreateWheelMesh( float radius, float length, bool topHalf, int segments = 16 )
	{
		if ( radius <= 0 || length <= 0 )
			return null;

		List<Vector3> vertices = [new( 0, -length / 2, 0 ), new( 0, length / 2, 0 )];


		float angleStep = 2 * MathF.PI / segments;
		int startAngleSegment = topHalf ? 0 : segments / 2;
		int endAngleSegment = topHalf ? segments / 2 : segments;

		for ( int i = startAngleSegment; i <= endAngleSegment; i++ )
		{
			float angle = i * angleStep;
			float x = radius * MathF.Cos( angle );
			float z = radius * MathF.Sin( angle );

			vertices.Add( new( x, -length / 2, z ) );
			vertices.Add( new( x, length / 2, z ) );
		}

		return Model.Builder.AddCollisionHull( [.. vertices] ).WithMass( 0 ).Create();
	}
}
