namespace Meteor.VehicleTool.Vehicle.Wheel;
public struct Friction
{
	public Friction()
	{
	}

	/// <summary>
	///     Current force in friction direction.
	/// </summary>
	public float Force { get; set; }

	/// <summary>
	/// Speed at the point of contact with the surface.
	/// </summary>
	public float Speed { get; set; }

	/// <summary>
	///     Multiplies the Y value (grip) of the friction graph.
	/// </summary>
	public float Grip { get; set; } = 1f;

	/// <summary>
	///     Current slip in friction direction.
	/// </summary>
	public float Slip { get; set; }
}
