using System.Collections.Generic;
using System.Linq;
using Meteor.VehicleTool.Vehicle.Wheel;
using Sandbox;

namespace Meteor.VehicleTool.Vehicle;
public class WheelManager : Component, IScenePhysicsEvents
{
	public bool IsOnGround { get; private set; }
	public float CombinedLoad => combinedLoad;
	private float combinedLoad;
	[Property] public List<WheelCollider> Wheels { get; private set; }
	public int WheelCount { get; private set; }

	protected override void OnAwake()
	{
		ConnectWheels();
	}

	internal void ConnectWheels()
	{
		Wheels ??= Components.GetAll<WheelCollider>( FindMode.InDescendants ).ToList();
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

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		UpdateWheelLoad();
	}

	internal void Register( WheelCollider wheel )
	{
		if ( Wheels.Contains( wheel ) )
			return;

		Wheels.Add( wheel );
		WheelCount++;

	}


	internal void UnRegister( WheelCollider wheel )
	{
		if ( !Wheels.Contains( wheel ) )
			return;

		Wheels.Remove( wheel );
		WheelCount--;
	}
}
