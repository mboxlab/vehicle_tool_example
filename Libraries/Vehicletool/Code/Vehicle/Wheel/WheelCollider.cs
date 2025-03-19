using System;
using Sandbox;

namespace Meteor.VehicleTool.Vehicle.Wheel;

[Category( "Physics" )]
[Title( "Wheel Collider" )]
[Icon( "sports_soccer" )]
public partial class WheelCollider : Component
{

	private float wheelRadius = 14;
	private float mass = 15;

	[Group( "Properties" ), Property]
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

	[Group( "Properties" ), Property] public float Width { get; set; } = 6;
	[Group( "Properties" ), Property]
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

	[Group( "Components" ), Property] VehicleController Controller { get; set; }

	private Rigidbody CarBody => Controller.Body;

	protected override void OnStart()
	{
		base.OnStart();
		velocityRotation = LocalRotation;
	}

	protected override void OnEnabled()
	{
		UpdateInertia();
		UpdateTotalSuspensionLength();
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


	protected override void OnFixedUpdate()
	{
		PhysUpdate();
	}

	public void PhysUpdate()
	{
		DoTrace();

		if ( UseVisual )
			UpdateVisual();

		UpdateSteer();
		UpdateHitVariables();
		UpdateSuspension();
		UpdateFriction();
	}
	protected override void OnAwake()
	{
		Controller ??= Components.Get<VehicleController>( FindMode.InAncestors );
	}
}
