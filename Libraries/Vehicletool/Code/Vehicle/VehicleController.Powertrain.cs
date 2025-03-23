using System;
using System.Collections.Generic;
using System.Linq;
using Meteor.VehicleTool.Vehicle.Powertrain;
using Meteor.VehicleTool.Vehicle.Wheel;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleController
{
	[Property, FeatureEnabled( "Powertrain", Icon = "power" )]
	public bool UsePowertrain { get; set; } = true;

	[Property, Feature( "Powertrain" ), Group( "Properties" )] public float MaxBrakeTorque { get; set; } = 15000;
	[Property, Feature( "Powertrain" ), Group( "Properties" )] public float HandBrakeTorque { get; set; } = 50000;


	[Property, Feature( "Powertrain" ), Group( "Components" )] public Engine Engine { get; set; }
	[Property, Feature( "Powertrain" ), Group( "Components" )] public Clutch Clutch { get; set; }
	[Property, Feature( "Powertrain" ), Group( "Components" )] public Transmission Transmission { get; set; }
	[Property, Feature( "Powertrain" ), Group( "Components" )] public Differential Differential { get; set; }


	private GameObject powertrainGameObject;

	[Property, Feature( "Powertrain" )] public List<WheelCollider> MotorWheels { get; set; }
	[Property, Feature( "Powertrain" )] public List<WheelCollider> HandBrakeWheels { get; set; }

	[Button, Feature( "Powertrain" )]
	internal void CreatePowertrain()
	{
		using var undoScope = Scene.Editor?.UndoScope( "Create Powertrain" ).WithComponentCreations().WithGameObjectCreations().Push();
		if ( !powertrainGameObject.IsValid() )
		{
			powertrainGameObject = null;
		}
		powertrainGameObject ??= new GameObject( true, "Powertrain" );

		if ( !Engine.IsValid() )
			Engine = new GameObject( powertrainGameObject, true, "Engine" ).GetOrAddComponent<Engine>();

		Engine.Controller = this;
		Engine.Inertia = 0.25f;
		if ( !Clutch.IsValid() )
			Clutch = new GameObject( Engine.GameObject, true, "Clutch" ).GetOrAddComponent<Clutch>();

		Clutch.Controller = this;
		Clutch.Inertia = 0.02f;

		Engine.Output = Clutch;

		if ( !Transmission.IsValid() )
			Transmission = new GameObject( Clutch.GameObject, true, "Transmission" ).GetOrAddComponent<Transmission>();

		Transmission.Controller = this;
		Transmission.Inertia = 0.01f;

		Clutch.Output = Transmission;

		Differential = new TreeBuilder( Transmission, MotorWheels ).Root.Diff;

		Transmission.Output = Differential;

	}


	private void UpdateBrakes()
	{
		foreach ( var wheel in Wheels )
			wheel.BrakeTorque = SwappedBrakes * MaxBrakeTorque;

		foreach ( var wheel in HandBrakeWheels )
			wheel.BrakeTorque += Handbrake * MaxBrakeTorque;
	}

}



internal class TreeNode
{
	internal TreeNode Left { get; set; }
	internal TreeNode Right { get; set; }
	public WheelPowertrain Item { get; set; }
	public Differential Diff { get; set; }
	public bool IsLeaf => Left == null && Right == null;
}

internal class TreeBuilder
{
	internal TreeNode Root { get; private set; }

	internal TreeBuilder( PowertrainComponent parent, List<WheelCollider> items )
	{
		if ( items == null || items.Count == 0 )
			throw new ArgumentException( "Items list cannot be null or empty." );

		Root = BuildTree( parent, items, 0, items.Count - 1 );
	}

	private static TreeNode BuildTree( PowertrainComponent parent, List<WheelCollider> items, int start, int end )
	{
		if ( start > end )
			return null;

		if ( start == end )
		{

			var leaf = new TreeNode() { Item = new GameObject( parent.GameObject, true, $"Wheel {items[start].GameObject.Name}" ).GetOrAddComponent<WheelPowertrain>() };
			leaf.Item.Controller = parent.Controller;
			leaf.Item.Wheel = items[start];
			leaf.Item.Inertia = 0.01f;
			var parentd = parent as Differential;
			if ( (start + 1) % 2 == 0 )
			{
				GameTask.RunInThreadAsync( () =>
				{
					parentd.OutputB = leaf.Item;
				} );
			}
			else
			{
				GameTask.RunInThreadAsync( () =>
				{
					parent.Output = leaf.Item;
				} );
			}

			return leaf;
		}

		int mid = (start + end) / 2;
		var diff = new GameObject( parent.GameObject, true, "Differential" ).GetOrAddComponent<Differential>();
		diff.Controller = parent.Controller;
		diff.Inertia = 0.1f;
		var node = new TreeNode
		{
			Left = BuildTree( diff, items, start, mid ),
			Right = BuildTree( diff, items, mid + 1, end ),
			Diff = diff
		};
		diff.Output = node.Left.Diff;
		diff.OutputB = node.Right.Diff;
		return node;
	}
}
