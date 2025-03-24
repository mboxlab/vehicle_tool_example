using Sandbox;

namespace Meteor.VehicleTool.Vehicle.Wheel;

[Category( "Physics" )]
[Title( "Wheel Collider" )]
[Icon( "sports_soccer" )]
public partial class WheelCollider : Component, IScenePhysicsEvents
{

	private float wheelRadius = 14;
	private float mass = 20;

	[Group( "Properties" ), Property, Sync]
	public float Radius
	{
		get => wheelRadius;
		set
		{
			wheelRadius = value;
			UpdateTotalSuspensionLength();
			UpdateInertia();
		}
	}


	private void UpdateInertia()
		=> Inertia = 0.5f * Mass * (wheelRadius.InchToMeter() * wheelRadius.InchToMeter());

	[Group( "Properties" ), Property, Sync] public float Width { get; set; } = 6;
	[Group( "Properties" ), Property, Sync]
	public float Mass
	{
		get => mass;
		set
		{
			mass = value;
			UpdateInertia();
		}
	}
	[Group( "Properties" ), Property, ReadOnly] public float Inertia;

	[Group( "Components" ), Property] public VehicleController Controller { get; set; }

	public bool AutoSimulate = true;
	private Rigidbody CarBody => Controller.Body;
	protected override void OnAwake()
	{
		Controller ??= Components.Get<VehicleController>( FindMode.InAncestors );
	}
	protected override void OnStart()
	{
		base.OnStart();
		velocityRotation = LocalRotation;
	}

	protected override void OnEnabled()
	{
		UpdateInertia();
		UpdateTotalSuspensionLength();
		SuspensionLength = suspensionTotalLength / 2;
		Controller?.Register( this );
	}

	protected override void OnDisabled()
	{
		Controller?.UnRegister( this );
	}

	protected override void OnDestroy()
	{
		Controller?.UnRegister( this );
	}

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		if ( AutoSimulate )
			PhysUpdate( Time.Delta * Scene.PhysicsSubSteps );
	}

	public void PhysUpdate( float dt )
	{
		DoTrace();

		if ( UseVisual )
			UpdateVisual();

		UpdateSteer();
		UpdateHitVariables();
		UpdateSuspension( dt );
		UpdateFriction( dt );
	}
}
