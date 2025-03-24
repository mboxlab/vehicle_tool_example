
namespace Sandbox;

public sealed class RespawnComponent : Component
{
	protected override void OnUpdate()
	{
		if ( IsProxy || Network.Owner is null )
			return;

		if ( Input.Pressed( "Reload" ) )
		{
			WorldRotation = WorldRotation.Angles().WithRoll( 0 );
		}
	}

}
