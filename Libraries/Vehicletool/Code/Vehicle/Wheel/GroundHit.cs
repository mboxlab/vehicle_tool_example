using Sandbox;
namespace Meteor.VehicleTool.Vehicle.Wheel;

/// <summary>
///     Represents single ground ray hit.
/// </summary>
public struct GroundHit
{

	public GroundHit( SceneTraceResult ray ) : this()
	{
		Normal = ray.Normal;
		if ( ray.Hit )
			Point = ray.HitPosition;
		else
			Point = ray.EndPosition;

		Surface = ray.Surface;
		StartPosition = ray.StartPosition;
		EndPosition = ray.EndPosition;
		HitPosition = ray.HitPosition;
		Hit = ray.Hit;
		Distance = EndPosition.Distance( ray.StartPosition );
		Body = ray.Body;
		Fraction = ray.Fraction;
		Collider = ray.Collider;
	}

	/// <summary>
	/// Collider that was hit.
	/// </summary>
	public Collider Collider { get; }

	public PhysicsBody Body { get; }

	/// <summary>
	///     The normal at the point of contact.
	/// </summary>
	public Vector3 Normal { get; }

	/// <summary>
	///     The point of contact between the wheel and the ground.
	/// </summary>
	public Vector3 Point { get; }
	public Surface Surface { get; }
	public Vector3 StartPosition { get; }
	public Vector3 EndPosition { get; }
	public Vector3 HitPosition { get; }
	public bool Hit { get; set; } = false;
	public float Distance { get; set; }
	public float Fraction { get; }
}
