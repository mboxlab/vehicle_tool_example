using System.Collections.Generic;
using System.Linq;
using Meteor.VehicleTool.Vehicle.Wheel;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleComponent
{
	public bool IsOnGround { get; private set; }
	[Property] public List<WheelCollider> Wheels { get; set; } = [];
	[Property] public float CombinedLoad => combinedLoad;
	private float combinedLoad;
	private int WheelCount;


	public void FindWheels()
	{
		Wheels = Components.GetAll<WheelCollider>( FindMode.InDescendants ).ToList();
		WheelCount = Wheels.Count;
	}

	private void UpdateWheelLoad()
	{
		combinedLoad = 0f;
		IsOnGround = false;
		for ( int i = 0; i < WheelCount; i++ )
		{
			WheelCollider wheel = Wheels[i];
			combinedLoad += wheel.Load;
			IsOnGround |= wheel.IsGrounded;
		}
	}

	public void Register( WheelCollider wheel )
	{
		if ( Wheels.Contains( wheel ) )
			return;

		Wheels.Add( wheel );
		WheelCount++;

	}


	public void UnRegister( WheelCollider wheel )
	{
		if ( !Wheels.Contains( wheel ) )
			return;

		Wheels.Remove( wheel );
		WheelCount--;
	}
}
