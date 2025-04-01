using Sandbox;

namespace Meteor.VehicleTool.Vehicle;


public partial class VehicleController
{
	/// <summary>
	/// Make sure the body and our components are created
	/// </summary>
	private void EnsureComponentsCreated()
	{
		Body.CollisionEventsEnabled = true;
		Body.CollisionUpdateEventsEnabled = true;
		Body.RigidbodyFlags = RigidbodyFlags.DisableCollisionSounds;
		Body.Flags = Body.Flags.WithFlag( ComponentFlags.Hidden, !_showRigidBodyComponent );
	}
}

