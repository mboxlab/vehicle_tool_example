using System;
using Sandbox;

namespace Meteor.VehicleTool.Vehicle.Wheel;

public struct PacejkaCurve
{
	// X // B
	[Range( 0, 30 )] public float Stiffnes;

	// Y // C
	[Range( 0, 5 )] public float ShapeFactor;

	// Z // D
	[Range( 0, 2 )] public float PeakValue;

	// W // E
	[Range( 0, 2 )] public float CurvatureFactor;

	public PacejkaCurve( float stiffnes, float shapeFactor, float peakValue, float curvatureFactor ) : this()
	{
		Stiffnes = stiffnes;
		ShapeFactor = shapeFactor;
		PeakValue = peakValue;
		CurvatureFactor = curvatureFactor;
		UpdateFrictionCurve();
	}

	/// <summary>
	/// Slip at which the friction preset has highest friction.
	/// </summary>
	public float PeakSlip;

	private Curve Curve;

	/// <summary>
	/// Gets the slip at which the friction is the highest for this friction curve.
	/// </summary>
	private readonly float GetPeakSlip()
	{
		float peakSlip = -1;
		float yMax = 0;

		for ( float i = 0; i < 1f; i += 0.01f )
		{
			float y = Curve.Evaluate( i );
			if ( y > yMax )
			{
				yMax = y;
				peakSlip = i;
			}
		}

		return peakSlip;
	}
	public readonly float Evaluate( float time ) => Curve.Evaluate( Math.Abs( time ) );


	/// <summary>
	///     Generate Curve from B,C,D and E parameters of Pacejka's simplified magic formula
	/// </summary>
	private void UpdateFrictionCurve()
	{
		Curve.Frame[] frames = new Curve.Frame[20];
		float t = 0;

		for ( int i = 0; i < frames.Length; i++ )
		{
			float v = GetFrictionValue( t );
			frames[i] = new Curve.Frame( t, v );

			if ( i <= 10 )
			{
				t += 0.02f;
			}
			else
			{
				t += 0.1f;
			}
		}
		Curve = new( frames );

		PeakSlip = GetPeakSlip();
	}

	private readonly float GetFrictionValue( float slip )
	{
		float B = Stiffnes;
		float C = ShapeFactor;
		float D = PeakValue;
		float E = CurvatureFactor;
		float t = MathF.Abs( slip );
		return D * MathF.Sin( C * MathF.Atan( B * t - E * (B * t - MathF.Atan( B * t )) ) );
	}

	public static readonly PacejkaCurve Asphalt = new( 9f, 2.15f, 0.933f, 0.971f );
	public static readonly PacejkaCurve AsphaltWet = new( 9f, 2.35f, 0.82f, 0.907f );
	public static readonly PacejkaCurve Generic = new( 8f, 1.9f, 0.8f, 0.99f );
	public static readonly PacejkaCurve Grass = new( 7.38f, 1.1f, 0.538f, 1f );
	public static readonly PacejkaCurve Dirt = new( 7.38f, 1.1f, 0.538f, 1f );
	public static readonly PacejkaCurve Gravel = new( 5.39f, 1.03f, 0.634f, 1f );
	public static readonly PacejkaCurve Ice = new( 1.2f, 2f, 0.16f, 1f );
	public static readonly PacejkaCurve Rock = new( 7.24f, 2.11f, 0.59f, 1f );
	public static readonly PacejkaCurve Sand = new( 5.13f, 1.2f, 0.443f, 0.5f );
	public static readonly PacejkaCurve Snow = new( 8.5f, 1.1f, 0.4f, 0.9f );
	public static readonly PacejkaCurve Tracks = new( 0.1f, 2f, 2f, 1f );

	public static PacejkaCurve GetPreset( PacejkaPreset preset )
	{
		return preset switch
		{
			PacejkaPreset.Asphalt => Asphalt,
			PacejkaPreset.AsphaltWet => AsphaltWet,
			PacejkaPreset.Generic => Generic,
			PacejkaPreset.Grass => Grass,
			PacejkaPreset.Dirt => Dirt,
			PacejkaPreset.Gravel => Gravel,
			PacejkaPreset.Ice => Ice,
			PacejkaPreset.Rock => Rock,
			PacejkaPreset.Sand => Sand,
			PacejkaPreset.Snow => Snow,
			PacejkaPreset.Tracks => Tracks,
			_ => Asphalt,
		};
	}
}

public enum PacejkaPreset
{
	Asphalt,
	AsphaltWet,
	Generic,
	Grass,
	Dirt,
	Gravel,
	Ice,
	Rock,
	Sand,
	Snow,
	Tracks,
}
