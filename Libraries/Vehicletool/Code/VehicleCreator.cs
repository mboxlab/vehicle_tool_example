using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Text.RegularExpressions;
using Meteor.VehicleTool.Vehicle;
using Meteor.VehicleTool.Vehicle.Wheel;
using Sandbox;
namespace Meteor.VehicleTool;

public sealed class VehicleCreator : Component
{
	[Property] public GameObject Model { get; set; }
	[Property, Group( "Wheels" )] public List<GameObject> Wheels { get; set; }
	[Property, Group( "Wheels" )] public List<GameObject> MotorWheels { get; set; }
	[Property, Group( "Wheels" )] public List<GameObject> SteeringWheels { get; set; }
	[Property, Group( "Wheels" )] public List<GameObject> HandBrakeWheels { get; set; }

	[Property, Group( "Engine" )] public List<SoundFile> AcsendingSounds { get; set; }
	[Property, Group( "Engine" )] public List<SoundFile> DecsendingSounds { get; set; }

	[Button]
	internal void CreateCar()
	{

		using var undo = Scene.Editor.UndoScope( "Create Car" ).WithComponentCreations().WithComponentDestructions( this ).Push();

		var controller = AddComponent<VehicleController>();

		List<WheelCollider> motors = [];
		List<WheelCollider> steering = [];
		List<WheelCollider> handBrake = [];

		var maxRPM = 0;
		foreach ( var item in Wheels )
		{
			GameObject p = new( item.Parent, true, $"WheelCollider ({item.Name})" )
			{
				WorldPosition = item.WorldPosition,
				WorldRotation = WorldRotation,
			};
			var collider = p.AddComponent<WheelCollider>();
			collider.UseVisual = true;
			collider.Controller = controller;

			item.SetParent( new( item.Parent, true, $"Wrap ({item.Name})" )
			{
				WorldPosition = item.WorldPosition,
				WorldRotation = WorldRotation,
			} );
			collider.RendererObject = item.Parent;

			if ( MotorWheels.Contains( item ) )
				motors.Add( collider );

			if ( SteeringWheels.Contains( item ) )
				steering.Add( collider );

			if ( HandBrakeWheels.Contains( item ) )
				handBrake.Add( collider );

			collider.SetBoundsToVisual();
		}

		Dictionary<int, SoundFile> acsendingSounds = [];
		Dictionary<int, SoundFile> decsendingSounds = [];

		foreach ( var item in AcsendingSounds )
		{
			var rpm = ExtractInteger( item.ResourceName );
			acsendingSounds.Add( rpm, item );
			maxRPM = Math.Max( maxRPM, rpm );
		}

		foreach ( var item in DecsendingSounds )
			decsendingSounds.Add( ExtractInteger( item.ResourceName ), item );


		controller.AcsendingSounds = acsendingSounds;
		controller.DecsendingSounds = decsendingSounds;

		controller.HandBrakeWheels = handBrake;
		controller.SteeringWheels = steering;
		controller.MotorWheels = motors;

		controller.Body.MassOverride = 1500;

		var size = Model.GetLocalBounds().Size;
		controller.CameraOffset = controller.CameraOffset.WithX( size.x * 1.5f ).WithZ( size.z / 2f );
		controller.ConnectWheels();
		controller.CreatePowertrain();
		controller.Engine.RevLimiterRPM = maxRPM;


		Destroy();
	}

	public static int ExtractInteger( string input )
	{
		Match match = Regex.Match( input, @"\d+" );
		if ( match.Success )
			return int.Parse( match.Value );
		throw new FormatException( "No integer found in the input." );
	}
}
