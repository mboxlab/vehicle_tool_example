
using System;

public sealed class MovingComponent : Component
{
	[Property] Vector3 EndPos { get; set; }
	[Property] float Offset { get; set; }

	Vector3 StartPos { get; set; }
	protected override void OnAwake()
	{
		StartPos = GameObject.WorldPosition;
	}


	protected override void OnFixedUpdate()
	{
		GameObject.WorldPosition = StartPos.LerpTo( StartPos + EndPos, MathF.Sin( Offset + Time.Now ), false ).RotateAround( StartPos, WorldRotation );
	}
}
