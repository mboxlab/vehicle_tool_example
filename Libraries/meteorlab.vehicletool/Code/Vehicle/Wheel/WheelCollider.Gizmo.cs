using Sandbox;
namespace Meteor.VehicleTool.Vehicle.Wheel;

public partial class WheelCollider : Component
{
	protected override void DrawGizmos()
	{

		if ( !Gizmo.IsSelected )
			return;

		Gizmo.Draw.IgnoreDepth = true;

		//
		// Suspension length
		//
		{
			var suspensionStart = Vector3.Zero - Vector3.Down * MinSuspensionLength;
			var suspensionEnd = Vector3.Zero + Vector3.Down * MaxSuspensionLength;

			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.LineThickness = 0.25f;

			Gizmo.Draw.Line( suspensionStart, suspensionEnd );

			Gizmo.Draw.Line( suspensionStart + Vector3.Forward, suspensionStart + Vector3.Backward );
			Gizmo.Draw.Line( suspensionEnd + Vector3.Forward, suspensionEnd + Vector3.Backward );
		}
		var widthOffset = Vector3.Right * Width * 0.5f;
		//
		// Wheel radius
		//
		{
			Gizmo.Draw.LineThickness = 0.5f;
			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.LineCylinder( widthOffset, -widthOffset, Radius, Radius, 16 );
		}

		//
		// Wheel width
		//
		{
			var circlePosition = Vector3.Zero;

			Gizmo.Draw.LineThickness = 0.25f;
			Gizmo.Draw.Color = Color.White;

			for ( float i = 0; i < 16; i++ )
			{

				var pos = circlePosition + Vector3.Up.RotateAround( Vector3.Zero, new Angles( i / 16 * 360, 0, 0 ) ) * Radius;

				Gizmo.Draw.Line( new Line( pos - widthOffset, pos + widthOffset ) );

				var pos2 = circlePosition + Vector3.Up.RotateAround( Vector3.Zero, new Angles( (i + 1) / 16 * 360, 0, 0 ) ) * Radius;
				Gizmo.Draw.Line( pos - widthOffset, pos2 + widthOffset );
			}
		}

		//
		// Forward direction
		//
		{
			var arrowStart = Vector3.Forward * Radius;
			var arrowEnd = arrowStart + Vector3.Forward * 8f;

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.Arrow( arrowStart, arrowEnd, 4, 1 );
		}
	}
}
