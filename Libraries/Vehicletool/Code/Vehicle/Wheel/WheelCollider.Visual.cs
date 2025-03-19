using Sandbox;

namespace Meteor.VehicleTool.Vehicle.Wheel;

public partial class WheelCollider
{

	[Property]
	[FeatureEnabled( "Visual", Icon = "videocam" )]
	public bool UseVisual { get; set; } = true;

	[Feature( "Visual" )]
	[Property] GameObject RendererObject { get; set; }

	void UpdateVisual()
	{
		if ( !RendererObject.IsValid() )
			return;

		axleAngle = AngularVelocity.RadianToDegree() * Time.Delta;

		RendererObject.WorldPosition = GetCenter();
	}
}
