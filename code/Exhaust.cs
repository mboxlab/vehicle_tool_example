using System;
using Meteor.VehicleTool.Vehicle;
using Meteor.VehicleTool.Vehicle.Powertrain;
using Sandbox;
using Sandbox.Audio;

public sealed class Exhaust : Component
{
	[Property, Group( "Components" )] public Engine Engine { get; set; }

	/// <summary>
	/// How much soot is emitted when throttle is pressed.
	/// </summary>
	[Property, Group( "Particle" ), Range( 0, 1 )] public float SootIntensity { get; set; } = 0.4f;

	/// <summary>
	/// Particle start speed is multiplied by this value based on engine RPM.
	/// </summary>
	[Property, Group( "Particle" ), Range( 1, 5 )] public float MaxSpeedMultiplier { get; set; } = 1.4f;

	/// <summary>
	/// Particle start size is multiplied by this value based on engine RPM.
	/// </summary>
	[Property, Group( "Particle" ), Range( 1, 5 )] public float MaxSizeMultiplier { get; set; } = 1.2f;

	/// <summary>
	/// Normal particle start color. Used when there is no throttle - engine is under no load.
	/// </summary>
	[Property, Group( "Particle" )] public Color NormalColor { get; set; } = new( 0.6f, 0.6f, 0.6f, 0.3f );

	/// <summary>
	/// Soot particle start color. Used under heavy throttle - engine is under load.
	/// </summary>
	[Property, Group( "Particle" )] public Color SootColor { get; set; } = new( 0.1f, 0.1f, 0.8f );
	static Texture SmokeTexture { get; set; } = Texture.Load( "materials/particles/smoke/render/smokeloop_i_0.vtex_c" );
	[Property, Group( "Components" ), RequireComponent] public ParticleConeEmitter Emitter { get; set; }
	[Property, Group( "Components" ), RequireComponent] public ParticleSpriteRenderer Renderer { get; set; }
	[Property, Group( "Components" ), RequireComponent] public ParticleEffect Effect { get; set; }

	[Property, Group( "Sound" ), Range( 0, 1 )] public MixerHandle SoundMixer { get; set; } = Mixer.Master;
	[Property, Group( "Sound" )] public SoundEvent PopSounds { get; set; }
	[Property, Group( "Sound" ), Range( 0f, 1f )] public float PopChance { get; set; } = 0.1f;
	[Property, Group( "Sound" )] public Dictionary<int, SoundFile> ExhaustSounds { get; set; }
	[Property, Group( "Sound" ), Range( 0f, 2f )] public float ExhaustVolume { get; set; } = 1;
	private Dictionary<int, SoundHandle> SoundHandles { get; set; }
	private List<int> SoundTimes { get; set; }
	private float SmoothValue { get; set; }
	private float SmoothVolume { get; set; }

	protected override void OnDestroy()
	{
		RemoveSounds();
	}

	private void RemoveSounds()
	{
		if ( SoundHandles is not null )
			foreach ( var item in SoundHandles.Values )
				item.Stop();
	}

	private void UpdateSound()
	{
		SmoothValue = SmoothValue * (1 - 0.2f) + Engine.OutputRPM * 0.2f;
		SmoothVolume = SmoothVolume * (1 - 0.1f) + Engine.ThrottlePosition * 0.1f;

		var isRunning = Engine.IsRunning;

		for ( int n = 0; n < SoundTimes.Count; n++ )
		{
			var time = SoundTimes[n]; // this
			float min = (n == 0) ? -100000f : SoundTimes[n - 1]; // prev
			float max = n == (SoundTimes.Count - 1) ? 100000f : SoundTimes[n + 1]; // next

			float c = MathM.Fade( SmoothValue, min - 10f, time, max + 10 );
			float vol = c * MathM.Map( SmoothVolume, 0f, 1f, 0.5f, 1f ) * ExhaustVolume;

			SoundHandle soundObject = SoundHandles[time];
			soundObject.Volume = isRunning ? vol : 0f;
			soundObject.Pitch = SmoothValue / time;
			soundObject.Position = WorldPosition;
		}
	}

	private async void LoadSoundsAsync()
	{
		SoundTimes = [];
		SoundHandles = [];

		foreach ( KeyValuePair<int, SoundFile> item in ExhaustSounds )
		{
			await item.Value.LoadAsync();
			SoundHandle snd = Sound.PlayFile( item.Value );
			snd.Volume = 0;

			snd.TargetMixer = SoundMixer.GetOrDefault();
			snd.Occlusion = true;
			snd.Distance = 25000;
			snd.Falloff = new Curve( new Curve.Frame( 0f, 1f, 0f, -1.8f ), new Curve.Frame( 0.05f, 0.08f, 3.5f, -3.5f ), new Curve.Frame( 0.2f, 0.04f, 0.16f, -0.16f ), new Curve.Frame( 1f, 0f ) );
			SoundTimes.Add( item.Key );
			SoundHandles.Add( item.Key, snd );
		}
	}

	private void OnRevLimiter()
	{
		if ( !PopSounds.IsValid() )
			return;

		if ( Random.Shared.Float( 0, 1 ) < PopChance )
		{
			var snd = Sound.Play( PopSounds );
			snd.Position = WorldPosition;
		}


	}

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

	protected override void OnAwake()
	{
		if ( ExhaustSounds.Count > 0 )
			LoadSoundsAsync();
	}
	protected override void OnStart()
	{
		_initStartSpeedMin = Effect.StartVelocity.ConstantA;
		_initStartSpeedMax = Effect.StartVelocity.ConstantB;

		_initStartSizeMin = Effect.Scale.ConstantA;
		_initStartSizeMax = Effect.Scale.ConstantB;

		Engine.OnRevLimiter += OnRevLimiter;
	}
	protected override void OnUpdate()
	{
		if ( SoundTimes is not null )
			UpdateSound();

		float engineLoad = Engine.Load;
		float rpmPercent = Engine.RPMPercent;
		_sootAmount = engineLoad * SootIntensity;

		Effect.Enabled = Engine.IsRunning;


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
