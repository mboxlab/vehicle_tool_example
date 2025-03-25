using System;
using Sandbox;

namespace Meteor.VehicleTool.Vehicle.Wheel;

[Category( "Physics" )]
[Title( "Wheel Collider" )]
[Icon( "sports_soccer" )]
public partial class WheelCollider : Component, IScenePhysicsEvents
{

	private float wheelRadius = 14;
	private float mass = 20;

	[Property] private ModelCollider BottomMeshCollider { get; set; }
	[Property] private ModelCollider TopMeshCollider { get; set; }
	[Property] private GameObject ColliderGO { get; set; }

	[Group( "Properties" ), Property, Sync]
	public float Radius
	{
		get => wheelRadius;
		set
		{
			wheelRadius = value;
			UpdateTotalSuspensionLength();
			UpdatePhysicalProperties();
		}
	}


	[Button]
	internal void CreateColliders()
	{
		using var undo = Scene.Editor.UndoScope( "Create Colliders" ).WithComponentCreations().WithComponentChanges( this ).Push();
		ColliderGO = new GameObject( GameObject, true, "Colliders" );
		BottomMeshCollider = ColliderGO.AddComponent<ModelCollider>();

		TopMeshCollider = ColliderGO.AddComponent<ModelCollider>();

		UpdatePhysicalProperties();
	}

	private void UpdatePhysicalProperties()
	{
		Inertia = 0.5f * Mass * (wheelRadius.InchToMeter() * wheelRadius.InchToMeter());
		if ( BottomMeshCollider != null )
		{
			float radiusUndersizing = Math.Clamp( Radius * 0.05f, 0, 0.025f );
			float widthUndersizing = Math.Clamp( Width * 0.05f, 0, 0.025f );
			BottomMeshCollider.Model = CreateWheelMesh(
				Radius - radiusUndersizing,
				Width - widthUndersizing, false );
			BottomMeshCollider.Friction = 0;
		}

		if ( TopMeshCollider != null )
		{
			float oversizing = Math.Clamp( Radius * 0.1f, 0, 0.1f );
			TopMeshCollider.Model = CreateWheelMesh(
				Radius + oversizing,
				Width + oversizing, true );
			TopMeshCollider.Friction = 0;
		}
	}

	[Group( "Properties" ), Property, Sync] public float Width { get; set; } = 6;
	[Group( "Properties" ), Property, Sync]
	public float Mass
	{
		get => mass;
		set
		{
			mass = value;
			UpdatePhysicalProperties();
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
		UpdatePhysicalProperties();
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

	protected override void OnUpdate()
	{

		if ( UseVisual )
			UpdateVisual();
	}

	public void PhysUpdate( float dt )
	{
		DoTrace();

		ColliderGO.WorldPosition = GetCenter();
		ColliderGO.WorldRotation = TransformRotationSteer;
		axleAngle = AngularVelocity.RadianToDegree() * Time.Delta;


		var bottomMeshColliderEnabled = false;

		// Check for high vertical velocity and enable the collider if above one frame travel distance
		float thresholdVelocity = suspensionTotalLength < 1e-5f ? float.MinValue : -suspensionTotalLength / dt;
		float relativeYVelocity = Controller.LocalVelocity.z;
		if ( relativeYVelocity < thresholdVelocity )
			bottomMeshColliderEnabled = true;

		if ( !bottomMeshColliderEnabled )
			UpdateSuspension( dt );

		if ( BottomMeshCollider.IsValid() )
			BottomMeshCollider.Enabled = bottomMeshColliderEnabled|| SuspensionLength == 0;

		UpdateSteer();
		UpdateHitVariables();
		UpdateFriction( dt );
	}
}
