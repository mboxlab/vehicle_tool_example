using Sandbox;

namespace Meteor.VehicleTool.Vehicle.Wheel;

public partial class WheelCollider
{

	[Property]
	[FeatureEnabled( "Visual", Icon = "videocam" )]
	public bool UseVisual { get; set; } = true;

	[Feature( "Visual" )]
	[Property] public GameObject RendererObject { get; set; }

	void UpdateVisual()
	{
		if ( !RendererObject.IsValid() )
			return;

		axleAngle = AngularVelocity.RadianToDegree() * Time.Delta;

		RendererObject.WorldPosition = GetCenter();
	}

	[Feature( "Visual" )]
	[Button]
	public void SetBoundsToVisual()
	{
		var bounds = RendererObject.GetLocalBounds().Size / 2;
		using ( Scene.Editor.UndoScope( "Set Bounds To Visual" ).WithComponentChanges( this ).Push() )
		{
			Width = bounds.y * 2 - 1;
			Radius = bounds.x - 1;
		}
	}
}
