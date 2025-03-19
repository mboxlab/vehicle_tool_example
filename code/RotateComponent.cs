
public sealed class RotateComponent : Component
{

	[Property]
	private Angles AngleVelocity { get; set; }
	protected override void OnFixedUpdate()
	{
		WorldRotation = AngleVelocity * Time.Now;
	}

}
