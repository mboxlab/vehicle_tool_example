using System;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle;

public static class MathM
{
	public static float ExpDecay( float a, float b, float decay, float dt ) => b + (a - b) * MathF.Exp( -decay * dt );
	public static float AngleDifference( float a, float b ) => ((((b - a) % 360) + 540) % 360) - 180;
	public static float ExpDecayAngle( float a, float b, float decay, float dt ) => ExpDecay( a, a + AngleDifference( a, b ), decay, dt );

	/// <summary>
	///     Converts angular velocity (rad/s) to rotations per minute.
	/// </summary>
	public static float AngularVelocityToRPM( this float angularVelocity ) => angularVelocity * 9.5492965855137f;

	/// <summary>
	///     Converts rotations per minute to angular velocity (rad/s).
	/// </summary>
	public static float RPMToAngularVelocity( this float RPM ) => RPM * 0.10471975511966f;


	public static float Map( float x, float a, float b, float c, float d ) => (x - a) / (b - a) * (d - c) + c;

	public static float Fade( float n, float min, float mid, float max )
	{
		if ( n < min || n > max )
			return 0;

		if ( n > mid )
			min = mid - (max - mid);

		return MathF.Cos( (1 - ((n - min) / (mid - min))) * (MathF.PI / 2) );
	}

	public static Vector3 MeterToInch( this Vector3 v ) => new( v.x.MeterToInch(), v.y.MeterToInch(), v.z.MeterToInch() );
	public static Vector3 InchToMeter( this Vector3 v ) => new( v.x.InchToMeter(), v.y.InchToMeter(), v.z.InchToMeter() );
	public static Vector3 InchToMillimeter( this Vector3 v ) => new( v.x.InchToMillimeter(), v.y.InchToMillimeter(), v.z.InchToMillimeter() );
	public static Vector3 MillimeterToInch( this Vector3 v ) => new( v.x.MillimeterToInch(), v.y.MillimeterToInch(), v.z.MillimeterToInch() );
	
	public static float SignedAngle( this Vector3 from, Vector3 to, Vector3 axis )
	{
		float unsignedAngle = Vector3.GetAngle( from, to );

		float cross_x = from.y * to.z - from.z * to.y;
		float cross_y = from.z * to.x - from.x * to.z;
		float cross_z = from.x * to.y - from.y * to.x;
		float sign = MathF.Sign( axis.x * cross_x + axis.y * cross_y + axis.z * cross_z );
		return unsignedAngle * sign;
	}


}
