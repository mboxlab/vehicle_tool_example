using System.Collections.Generic;
using Meteor.VehicleTool.Vehicle.Wheel;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleController
{
	[Property, RequireComponent, Group( "Components" )] public WheelManager Manager { get; set; }
	public bool IsOnGround => Manager.IsOnGround;
	public float CombinedLoad => Manager.CombinedLoad;
	public IReadOnlyList<WheelCollider> Wheels => Manager.Wheels;
	public int WheelCount => Manager.WheelCount;

	[Button]
	internal void ConnectWheels() => Manager.ConnectWheels();

	public void Register( WheelCollider wheel )
	{
		Manager.Register( wheel );
	}
	public void UnRegister( WheelCollider wheel )
	{
		Manager.UnRegister( wheel );
	}
}
