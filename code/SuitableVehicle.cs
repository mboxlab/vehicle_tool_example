using Meteor.VehicleTool.Vehicle;

public sealed class SuitableVehicle : Component, Component.IPressable, Component.INetworkListener, IGameObjectNetworkEvents
{

	[Sync] public VehicleController Vehicle { get; private set; }
	[Sync] public PlayerController User { get; private set; }
	public Connection Owner { get; set; }


	protected override void OnStart()
	{

		Vehicle ??= Components.Get<VehicleController>( FindMode.EverythingInSelf );
		if ( IsProxy )
			return;
		Vehicle.UseInputControls = false;
		Vehicle.UseCameraControls = false;
		Vehicle.UseLookControls = false;
	}

	void INetworkListener.OnDisconnected( Connection channel )
	{
		if ( Owner is null || Owner == channel )
		{
			StandUp();
			DestroyGameObject();
		}
	}

	public bool Press( IPressable.Event e )
	{
		if ( e.Source is PlayerController ply && !User.IsValid() )
		{
			Network.TakeOwnership();
			User = ply;
			ply.GameObject.Enabled = false;
			Vehicle.UseInputControls = true;
			Vehicle.UseCameraControls = true;
			Vehicle.UseLookControls = true;
			Input.Clear( "use" );
			return true;
		}
		return false;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( Input.Pressed( "use" ) )
			StandUp();

	}

	private void StandUp()
	{
		if ( !User.IsValid() )
			return;

		User.GameObject.Enabled = true;
		Vehicle.UseInputControls = false;
		Vehicle.UseCameraControls = false;
		Vehicle.UseLookControls = false;
		Vehicle.ResetInput();


		foreach ( var item in Vehicle.Wheels )
		{
			item.BrakeTorque = 200f;
		}

		User.WorldPosition = Vehicle.WorldTransform.PointToWorld( Vector3.Left * 64f );
		User.EyeAngles = Vehicle.WorldRotation;
		User = null;

		Network.DropOwnership();
	}
}
