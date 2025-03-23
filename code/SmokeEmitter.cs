
using System;
using Meteor.VehicleTool.Vehicle;
using Meteor.VehicleTool.Vehicle.Wheel;
using Sandbox;
using static Sandbox.Gizmo;

public sealed class SmokeEmitter : Component
{
	[Property, RequireComponent] public ParticleSphereEmitter Emitter { get; set; }
	[Property, RequireComponent] public ParticleSpriteRenderer Renderer { get; set; }
	[Property, RequireComponent] public ParticleEffect Effect { get; set; }

	[Property] static Texture SmokeTexture { get; set; } = Texture.Load( "textures/smoketexturesheet.vtex_c" );

	[Button]
	public void SetupSmoke()
	{
		Effect ??= Components.GetOrCreate<ParticleEffect>( FindMode.InSelf );
		Effect.MaxParticles = 1000;
		Effect.Lifetime = new()
		{
			Type = ParticleFloat.ValueType.Range,
			Evaluation = ParticleFloat.EvaluationType.Particle,
			ConstantA = 2,
			ConstantB = 4.2f,
		};

		Effect.StartVelocity = new()
		{
			Type = ParticleFloat.ValueType.Range,
			Evaluation = ParticleFloat.EvaluationType.Particle,
			ConstantA = 10,
			ConstantB = 70,
		};

		Effect.Damping = 0.9f;
		Effect.ApplyRotation = true;
		Effect.Roll = new()
		{
			Type = ParticleFloat.ValueType.Range,
			Evaluation = ParticleFloat.EvaluationType.Particle,
			ConstantA = 0,
			ConstantB = 360,
		};
		Effect.ApplyShape = true;
		Effect.Scale = new()
		{
			Type = ParticleFloat.ValueType.Curve,
			Evaluation = ParticleFloat.EvaluationType.Life,
			CurveA = new( new List<Curve.Frame>() { new( 0, 10f ), new( 0.05f, 50f ), new( 1f, 200f ) } ),
		};
		Effect.ApplyColor = true;
		Effect.Alpha = new()
		{
			Type = ParticleFloat.ValueType.Curve,
			Evaluation = ParticleFloat.EvaluationType.Particle,
			CurveA = new( new List<Curve.Frame>() { new( 0, 0 ), new( 0.2f, 0.5f ), new( 1, 0 ) } ),
		};
		Effect.ApplyAlpha = true;
		Effect.Gradient = new()
		{
			Type = ParticleGradient.ValueType.Range,
			Evaluation = ParticleGradient.EvaluationType.Life,
			ConstantA = Color.White,
			ConstantB = Color.Transparent,
		};
		Effect.Force = true;
		Effect.ForceDirection = Vector3.Up * 100f;
		Effect.SheetSequence = true;
		Effect.SequenceSpeed = 0.5f;
		Effect.SequenceTime = 1f;

		Renderer ??= Components.GetOrCreate<ParticleSpriteRenderer>( FindMode.InSelf );
		Renderer.Texture = SmokeTexture;

		Emitter ??= Components.GetOrCreate<ParticleSphereEmitter>( FindMode.InSelf );
		Emitter.Velocity = 0;
		Emitter.Duration = 5;
		Emitter.Burst = 5;
		Emitter.Rate = 50;

	}


	private WheelCollider wheel;
	protected override void OnAwake()
	{
		wheel ??= Components.Get<WheelCollider>( FindMode.EverythingInAncestors );
	}

	private float smokeRate;
	void UpdateSmokeRate( float target )
	{
		if ( target > smokeRate )
		{
			smokeRate = MathX.Lerp( smokeRate, target, Time.Delta * 0.05f );
		}
		else if ( target < smokeRate )
		{
			smokeRate = MathX.Lerp( smokeRate, target, Time.Delta * 0.8f );
		}
	}
	protected override void OnUpdate()
	{

		float latEmission = wheel.IsSkiddingLaterally ? wheel.NormalizedLateralSlip : 0f;
		float lngEmission = wheel.IsSkiddingLongitudinally ? wheel.NormalizedLongitudinalSlip : 0f;

		if ( wheel.FrictionPreset.PeakValue > 0.9f )
			UpdateSmokeRate( Math.Clamp( latEmission + lngEmission, 0, 1 ) );
		else
			UpdateSmokeRate( 0 );

		Emitter.Rate = smokeRate * 100f;
		Emitter.Enabled = smokeRate > 0.1f;
		Emitter.RateOverDistance = smokeRate * 100f;

	}
}
