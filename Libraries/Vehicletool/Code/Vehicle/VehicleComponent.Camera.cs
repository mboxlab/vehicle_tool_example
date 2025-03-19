using System;
using Sandbox;

namespace Meteor.VehicleTool.Vehicle;

public partial class VehicleComponent
{
	private float _cameraDistance = 100f;
	private float _eyez;

	[Property]
	[FeatureEnabled( "Camera", Icon = "videocam" )]
	public bool UseCameraControls { get; set; } = true;


	[Property]
	[Feature( "Camera" )]
	public bool ThirdPerson { get; set; } = true;

	[Property]
	[Feature( "Camera" )]
	public Vector3 CameraOffset { get; set; } = new Vector3( 256f, 0f, 12f );

	/// <summary>
	/// The player's eye position, in first person mode
	/// </summary>
	[Property]
	[Feature( "Camera" )]
	public Vector3 EyePosition { get; set; }

	[Property]
	[Feature( "Camera" )]
	[InputAction]
	public string ToggleCameraModeButton { get; set; } = "view";

	/// <summary>
	/// The direction we're looking.
	/// </summary>
	[Sync( SyncFlags.Interpolate )]
	public Angles EyeAngles { get; set; }


	[Property]
	[Feature( "Camera" )]
	public float BodyHeight { get; set; } = 72f;

	[Property]
	[Feature( "Camera" )]
	public float EyeDistanceFromTop { get; set; } = 8f;

	private void UpdateCameraPosition()
	{
		CameraComponent cam = Scene.Camera;
		if ( cam == null )
			return;

		if ( !string.IsNullOrWhiteSpace( ToggleCameraModeButton ) && Input.Pressed( ToggleCameraModeButton ) )
		{
			ThirdPerson = !ThirdPerson;
			_cameraDistance = 20f;
		}

		Rotation worldRotation = EyeAngles.ToRotation();
		if ( !ThirdPerson )
		{
			worldRotation = WorldRotation * worldRotation;
		}
		cam.WorldRotation = worldRotation;
		Vector3 from = WorldPosition + Vector3.Up * (BodyHeight - EyeDistanceFromTop);
		if ( IsOnGround && _eyez != 0f )
		{
			from.z = _eyez.LerpTo( from.z, Time.Delta * 50f );
		}

		_eyez = from.z;

		if ( ThirdPerson )
		{
			Vector3 vector = worldRotation.Forward * (0f - CameraOffset.x) + worldRotation.Up * CameraOffset.z + worldRotation.Right * CameraOffset.y;
			SceneTrace trace = Scene.Trace;
			Vector3 to = from + vector;
			SceneTraceResult sceneTraceResult = trace.FromTo( in from, in to ).IgnoreGameObjectHierarchy( GameObject ).Radius( 8f ).Run();

			if ( sceneTraceResult.StartedSolid )
			{
				_cameraDistance = _cameraDistance.LerpTo( vector.Length, Time.Delta * 100f );
			}
			else if ( sceneTraceResult.Distance < _cameraDistance )
			{
				_cameraDistance = _cameraDistance.LerpTo( sceneTraceResult.Distance, Time.Delta * 200f );
			}
			else
			{
				_cameraDistance = _cameraDistance.LerpTo( sceneTraceResult.Distance, Time.Delta * 2f );
			}

			from += vector.Normal * _cameraDistance;
		}
		else
		{
			from = WorldTransform.PointToWorld( EyePosition );
		}

		cam.WorldPosition = from;

	}
}
