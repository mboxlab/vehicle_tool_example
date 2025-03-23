using Meteor.VehicleTool.Vehicle.Powertrain;
using Sandbox;

public sealed class Exhaust : Component
{
	[Property] public Engine Engine { get; set; }

	/// <summary>
	/// How much soot is emitted when throttle is pressed.
	/// </summary>
	[Property, Range( 0, 1 )] public float SootIntensity { get; set; } = 0.4f;

	/// <summary>
	/// Particle start speed is multiplied by this value based on engine RPM.
	/// </summary>
	[Property, Range( 1, 5 )] public float MaxSpeedMultiplier { get; set; } = 1.4f;

	/// <summary>
	/// Particle start size is multiplied by this value based on engine RPM.
	/// </summary>
	[Property, Range( 1, 5 )] public float MaxSizeMultiplier { get; set; } = 1.2f;

	/// <summary>
	/// Normal particle start color. Used when there is no throttle - engine is under no load.
	/// </summary>
	[Property] public Color NormalColor { get; set; } = new( 0.6f, 0.6f, 0.6f, 0.3f );

	/// <summary>
	/// Soot particle start color. Used under heavy throttle - engine is under load.
	/// </summary>
	[Property] public Color SootColor { get; set; } = new( 0.1f, 0.1f, 0.8f );
	[Property] static Texture SmokeTexture { get; set; } = Texture.Load( "materials/particles/smoke/render/smokeloop_i_0.vtex_c" );
	[Property, RequireComponent] public ParticleConeEmitter Emitter { get; set; }
	[Property, RequireComponent] public ParticleSpriteRenderer Renderer { get; set; }
	[Property, RequireComponent] public ParticleEffect Effect { get; set; }


	[Button]
	public void SetupSmoke()
	{
		Effect ??= Components.GetOrCreate<ParticleEffect>( FindMode.InSelf );
		Effect.MaxParticles = 1000;
		Effect.Lifetime = new()
		{
			Type = ParticleFloat.ValueType.Range,
			Evaluation = ParticleFloat.EvaluationType.Particle,
			ConstantA = 1.2f,
			ConstantB = 2f,
		};

		Effect.StartVelocity = 0;
		Effect.Damping = 1f;
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
			CurveA = new( new List<Curve.Frame>() { new( 0, 0 ), new( 0.05f, 5f ), new( 1f, 20f ) } ),
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
		Effect.Space = ParticleEffect.SimulationSpace.Local;
		Effect.Force = true;
		Effect.ForceDirection = new Vector3( 120, 0, 20 );
		Effect.SheetSequence = true;
		Effect.SequenceSpeed = 0.5f;
		Effect.SequenceTime = 1f;

		Renderer ??= Components.GetOrCreate<ParticleSpriteRenderer>( FindMode.InSelf );
		Renderer.Texture = SmokeTexture;
		Renderer.MotionBlur = true;

		Emitter ??= Components.GetOrCreate<ParticleConeEmitter>( FindMode.InSelf );
		Emitter.ConeAngle = 22;
		Emitter.ConeFar = 15;
		Emitter.Burst = 1;
		Emitter.Rate = 50;

	}


	private float _initStartSpeedMin;
	private float _initStartSpeedMax;
	private float _initStartSizeMin;
	private float _initStartSizeMax;
	private float _sootAmount;
	private ParticleFloat _minMaxCurve;

	protected override void OnStart()
	{
		_initStartSpeedMin = Effect.StartVelocity.ConstantA;
		_initStartSpeedMax = Effect.StartVelocity.ConstantB;

		_initStartSizeMin = Effect.Scale.ConstantA;
		_initStartSizeMax = Effect.Scale.ConstantB;

	}
	protected override void OnUpdate()
	{
		float engineLoad = Engine.Load;
		float rpmPercent = Engine.RPMPercent;
		_sootAmount = engineLoad * SootIntensity;

		Effect.Enabled = Engine.Enabled;


		// Color
		Effect.Tint = Color.Lerp( Effect.Tint, Color.Lerp( NormalColor, SootColor, _sootAmount ), Time.Delta * 7f );
		Effect.Tint = Effect.Tint.WithAlphaMultiplied( 10 / (Engine.Controller.CurrentSpeed + 10) );

		// Speed
		float speedMultiplier = MaxSpeedMultiplier - 1f;
		_minMaxCurve = Effect.StartVelocity;
		_minMaxCurve.ConstantA = _initStartSpeedMin + rpmPercent * speedMultiplier;
		_minMaxCurve.ConstantB = _initStartSpeedMax + rpmPercent * speedMultiplier;
		Effect.StartVelocity = _minMaxCurve;


		// Size
		float sizeMultiplier = MaxSizeMultiplier - 1f;
		_minMaxCurve = Effect.Scale;
		_minMaxCurve.ConstantA = _initStartSizeMin + rpmPercent * sizeMultiplier;
		_minMaxCurve.ConstantB = _initStartSizeMax + rpmPercent * sizeMultiplier;
		Effect.Scale = _minMaxCurve;

	}
}
