
using System;

namespace Sandbox;

public sealed class RespawnComponent : Component
{
	public PlayerController Controller;
	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( Input.Pressed( "Use" ) )
		{
			Controller.GameObject.Enabled = true;
			DestroyGameObject();
		}

		if ( Input.Pressed( "Reload" ) )
		{
			WorldRotation = WorldRotation.Angles().WithRoll( 0 );
		}
	}

}
