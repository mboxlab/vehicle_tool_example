using Sandbox;

public sealed class AntiPlayerPush : Component, Component.ICollisionListener
{
	public void OnCollisionStart( Collision collision )
	{
		collision.Other.Body.ApplyImpulse( -collision.Other.Body.Velocity );
	}
	public void OnCollisionUpdate( Collision collision )
	{
		if ( collision.Other.GameObject.Components.TryGet<PlayerController>( out var ply, FindMode.InAncestors ) )
		{
			ply.Body.ApplyImpulse( -ply.Velocity * 1500);

		}
	}
}
