using System.Collections.Generic;
using System;
using Sandbox;
using Sandbox.Audio;

namespace Meteor.VehicleTool.Vehicle;
public partial class VehicleController
{

	[Property, FeatureEnabled( "Sound", Icon = "audio" )] public bool UseAudio { get; set; } = true;
	[Property, Range( 0, 1 ), Feature( "Sound" )] public MixerHandle SoundMixer { get; set; } = Mixer.Master;

	[Property, Feature( "Sound" ), Group( "Engine" )] public GameObject SoundParent { get; set; }
	[Property, Feature( "Sound" ), Group( "Engine" )] public Dictionary<int, SoundFile> AcsendingSounds { get; set; }
	[Property, Feature( "Sound" ), Group( "Engine" )] public Dictionary<int, SoundFile> DecsendingSounds { get; set; }
	[Property, Range( 0, 1 ), Feature( "Sound" ), Group( "Engine" )] public float EngineVolume { get; set; } = 1f;

	[Property, Feature( "Sound" ), Group( "Transmission" )] public SoundFile GearUpSound { get; set; }
	[Property, Feature( "Sound" ), Group( "Transmission" )] public SoundFile GearDownSound { get; set; }
	[Property, Range( 0, 1 ), Feature( "Sound" ), Group( "Transmission" )] public float GearVolume { get; set; } = 1f;

	private Dictionary<int, SoundHandle> AcsendingSoundHandles { get; set; }
	private List<int> AcsendingSoundTimes { get; set; }
	private Dictionary<int, SoundHandle> DecsendingSoundHandles { get; set; }
	private List<int> DecsendingSoundTimes { get; set; }

	[Sync( SyncFlags.Interpolate )] private float SmoothValue { get; set; }
	[Sync( SyncFlags.Interpolate )] private float SmoothVolume { get; set; }

	private void PlayGearShift( SoundFile file )
	{
		if ( !UseAudio || !file.IsValid() )
			return;
		SoundHandle snd = Sound.PlayFile( file );
		snd.Volume = GearVolume;
		snd.TargetMixer = SoundMixer.GetOrDefault();
		snd.Occlusion = true;
		snd.Position = WorldPosition;
	}
	private void OnGearUp() => PlayGearShift( GearUpSound );
	private void OnGearDown() => PlayGearShift( GearDownSound );

	private void RemoveSounds()
	{
		if ( AcsendingSoundHandles is not null )
			foreach ( var item in AcsendingSoundHandles.Values )
				item.Stop();

		if ( DecsendingSoundHandles is not null )
			foreach ( var item in DecsendingSoundHandles.Values )
				item.Stop();
	}
	private void UpdateSound()
	{
		SmoothValue = SmoothValue * (1 - 0.2f) + Engine.OutputRPM * 0.2f;
		SmoothVolume = SmoothVolume * (1 - 0.1f) + Engine.ThrottlePosition * 0.1f;

		var isAcsending = Engine.ThrottlePosition != 0;
		var isRunning = Engine.IsRunning;
		var pos = WorldPosition;
		if ( SoundParent.IsValid() )
			pos = SoundParent.WorldPosition;

		for ( int n = 0; n < AcsendingSoundTimes.Count; n++ )
		{
			var time = AcsendingSoundTimes[n]; // this
			float min = (n == 0) ? -100000f : AcsendingSoundTimes[n - 1]; // prev
			float max = n == (AcsendingSoundTimes.Count - 1) ? 100000f : AcsendingSoundTimes[n + 1]; // next

			float c = MathM.Fade( SmoothValue, min - 10f, time, max + 10 );
			float vol = c * MathM.Map( SmoothVolume, 0f, 1f, 0.5f, 1f ) * EngineVolume;

			SoundHandle soundObject = AcsendingSoundHandles[time];
			soundObject.Volume = isRunning ? isAcsending ? vol : vol * 0.5f : 0f;
			soundObject.Pitch = SmoothValue / time;
			soundObject.Position = pos;
		}

		for ( int n = 0; n < DecsendingSoundTimes.Count; n++ )
		{
			var time = DecsendingSoundTimes[n]; // this
			float min = (n == 0) ? -100000f : DecsendingSoundTimes[n - 1]; // prev
			float max = n == (DecsendingSoundTimes.Count - 1) ? 100000f : DecsendingSoundTimes[n + 1]; // next

			float c = MathM.Fade( SmoothValue, min - 10f, time, max + 10 );
			float vol = c * MathM.Map( SmoothVolume, 0f, 1f, 0.5f, 1f ) * EngineVolume;

			SoundHandle soundObject = DecsendingSoundHandles[time];
			soundObject.Volume = isRunning ? isAcsending ? vol * 0.5f : vol : 0;
			soundObject.Pitch = SmoothValue / time;
			soundObject.Position = pos;
		}

	}

	private async void LoadSoundsAsync()
	{
		AcsendingSoundTimes = [];
		AcsendingSoundHandles = [];
		DecsendingSoundTimes = [];
		DecsendingSoundHandles = [];
		foreach ( KeyValuePair<int, SoundFile> item in AcsendingSounds )
		{
			await item.Value.LoadAsync();
			SoundHandle snd = Sound.PlayFile( item.Value );
			snd.Volume = 0;

			snd.TargetMixer = SoundMixer.GetOrDefault();
			snd.Occlusion = true;
			snd.Distance = 9000;
			snd.Falloff = new Curve( new Curve.Frame( 0f, 1f, 0f, -1.8f ), new Curve.Frame( 0.05f, 0.08f, 3.5f, -3.5f ), new Curve.Frame( 0.2f, 0.04f, 0.16f, -0.16f ), new Curve.Frame( 1f, 0f ) );
			AcsendingSoundTimes.Add( item.Key );
			AcsendingSoundHandles.Add( item.Key, snd );
		}

		foreach ( KeyValuePair<int, SoundFile> item in DecsendingSounds )
		{
			await item.Value.LoadAsync();
			SoundHandle snd = Sound.PlayFile( item.Value );
			snd.Volume = 0;

			snd.TargetMixer = SoundMixer.GetOrDefault();
			snd.Occlusion = true;
			DecsendingSoundTimes.Add( item.Key );
			DecsendingSoundHandles.Add( item.Key, snd );
		}
	}
}
