namespace Sandbox;

public sealed class VehicleSpawner : Component, Component.IPressable
{
	[Property] public GameObject VehiclePrefab { get; set; }

	[Property] public Transform SpawnOffset { get; set; } = new();

	public bool Press( IPressable.Event e )
	{
		if ( VehiclePrefab.IsValid() && e.Source is PlayerController controller )
		{
			var vehicle = VehiclePrefab.Clone( WorldTransform.ToWorld( SpawnOffset ).WithScale( 1 ), name: $"Vehicle - {controller.Network.Owner.DisplayName}" );

			vehicle.NetworkSpawn();
			var c = vehicle.Components.Get<RespawnComponent>();

			c.Controller = controller;
			controller.GameObject.Enabled = false;
			return true;
		}

		return false;
	}
}
