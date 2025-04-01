using Sandbox;
using System.Collections.Generic;
using System;
using Meteor.VehicleTool.Vehicle.Wheel;


namespace Meteor.VehicleTool.Vehicle.Powertrain;

public class Transmission : PowertrainComponent
{
	protected override void OnAwake()
	{
		base.OnAwake();
		Name ??= "Transmission";
		LoadGearsFromGearingProfile();
	}
	/// <summary>
	///     A class representing a single ground surface type.
	/// </summary>
	public partial class TransmissionGearingProfile
	{
		/// <summary>
		///     List of forward gear ratios starting from 1st forward gear.
		/// </summary>
		public List<float> ForwardGears { get; set; } = [3.59f, 2.02f, 1.38f, 1f, 0.87f];

		/// <summary>
		///     List of reverse gear ratios starting from 1st reverse gear.
		/// </summary>
		public List<float> ReverseGears { get; set; } = [-4f,];
	}

	public const float INPUT_DEADZONE = 0.05f;
	public float ReferenceShiftRPM => _referenceShiftRPM;

	private float _referenceShiftRPM;

	/// <summary>
	///     If true the gear input has to be held for the transmission to stay in gear, otherwise it goes to neutral.
	///     Used for hardware H-shifters.
	/// </summary>
	[Property] public bool HoldToKeepInGear { get; set; }

	/// <summary>
	///     Final gear multiplier. Each gear gets multiplied by this value.
	///     Equivalent to axle/differential ratio in real life.
	/// </summary>
	[Property] public float FinalGearRatio { get; set; } = 4.3f;
	/// <summary>
	///     [Obsolete, will be removed]
	///     Currently active gearing profile.
	///     Final gear ratio will be determined from this and final gear ratio.
	/// </summary>

	[Property] public TransmissionGearingProfile GearingProfile { get; set; } = new();


	/// <summary>
	/// A list of gears ratios in order of negative, neutral and then positive.
	/// E.g. -4, 0, 6, 4, 3, 2 => one reverse, 4 forward gears.
	/// </summary>
	[Property, ReadOnly, Group( "Info" )] public List<float> Gears = new();

	/// <summary>
	///     Number of forward gears.
	/// </summary>
	public int ForwardGearCount;

	/// <summary>
	///     Number of reverse gears.
	/// </summary>
	public int ReverseGearCount;

	/// <summary>
	///     How much inclines affect shift point position. Higher value will push the shift up and shift down RPM up depending
	///     on the current incline to prevent vehicle from upshifting at the wrong time.
	/// </summary>
	[Property, Range( 0, 4 )] public float InclineEffectCoeff { get; set; }

	/// <summary>
	/// Function that handles gear shifts.
	/// Use External transmission type and assign this delegate manually to use a custom
	/// gear shift function.
	/// </summary>
	public delegate void Shift( VehicleController vc );

	/// <summary>
	///     Function that changes the gears as required.
	///     Use transmissionType External and assign this delegate to use your own gear shift code.
	/// </summary>
	public Shift ShiftDelegate;

	/// <summary>
	///     Event that gets triggered when transmission shifts down.
	/// </summary>
	public event Action OnGearDownShift;

	/// <summary>
	///     Event that gets triggered when transmission shifts (up or down).
	/// </summary>
	public event Action OnGearShift;

	/// <summary>
	///     Event that gets triggered when transmission shifts up.
	/// </summary>
	public event Action OnGearUpShift;

	/// <summary>
	///     Time after shifting in which shifting can not be done again.
	/// </summary>
	[Property] public float PostShiftBan { get; set; } = 0.5f;

	public enum AutomaticTransmissionDNRShiftType
	{
		Auto,
		RequireShiftInput,
		RepeatInput,
	}

	/// <summary>
	///     Behavior when switching from neutral to forward or reverse gear.
	/// </summary>
	[Property] public AutomaticTransmissionDNRShiftType DNRShiftType { get; set; } = AutomaticTransmissionDNRShiftType.Auto;

	/// <summary>
	/// Speed at which the vehicle can switch between D/N/R gears.
	/// </summary>
	[Property] public float DnrSpeedThreshold { get; set; } = 0.4f;

	/// <summary>
	/// If set to >0, the clutch will need to be released to the value below the set number
	/// for gear shifts to occur.
	/// </summary>
	[Property] public float ClutchInputShiftThreshold { get; set; } = 1.0f;

	/// <summary>
	///     Time it takes transmission to shift between gears.
	/// </summary>
	[Property] public float ShiftDuration { get; set; } = 0.2f;

	/// <summary>
	///     Intensity of variable shift point. Higher value will result in shift point moving higher up with higher engine
	///     load.
	/// </summary>
	[Property, Range( 0, 1 )] public float VariableShiftIntensity { get; set; } = 0.3f;

	/// <summary>
	///     If enabled shifting when in manual transmission will be instant, ignoring post shift ban.
	/// </summary>
	[Property] public bool IgnorePostShiftBanInManual { get; set; } = true;

	/// <summary>
	///     If enabled transmission will adjust both shift up and down points to match current load.
	/// </summary>
	[Property] public bool VariableShiftPoint { get; set; } = true;

	/// <summary>
	/// Current gear ratio.
	/// </summary>
	[Property, ReadOnly] public float CurrentGearRatio { get; set; }

	/// <summary>
	/// Is the transmission currently in the post-shift phase in which the shifting is disabled/banned to prevent gear hunting?
	/// </summary>
	[Property, ReadOnly] public bool IsPostShiftBanActive { get; set; }

	/// <summary>
	/// Is a gear shift currently in progress.
	/// </summary>
	[Property, ReadOnly] public bool IsShifting { get; set; }

	/// <summary>
	/// Progress of the current gear shift in range of 0 to 1.
	/// </summary>
	[Property, ReadOnly] public float ShiftProgress { get; set; }


	/// <summary>
	///     Current RPM at which transmission will aim to downshift. All the modifiers are taken into account.
	///     This value changes with driving conditions.
	/// </summary>
	[Property]
	public float DownshiftRPM
	{
		get => _downshiftRPM;
		set { _downshiftRPM = Math.Clamp( value, 0, float.MaxValue ); }
	}

	private float _downshiftRPM = 1400;
	/// <summary>
	///     RPM at which the transmission will try to downshift, but the value might get changed by shift modifier such
	///     as incline modifier.
	///     To get actual downshift RPM use DownshiftRPM.
	/// </summary>
	[Property]
	public float TargetDownshiftRPM => _targetDownshiftRPM;

	private float _targetDownshiftRPM;
	/// <summary>
	///     RPM at which automatic transmission will shift up. If dynamic shift point is enabled this value will change
	///     depending on load.
	/// </summary>
	[Property]
	public float UpshiftRPM
	{
		get => _upshiftRPM;
		set { _upshiftRPM = Math.Clamp( value, 0, float.MaxValue ); }
	}
	private float _upshiftRPM = 2800;

	/// <summary>
	///     RPM at which the transmission will try to upshift, but the value might get changed by shift modifier such
	///     as incline modifier.
	///     To get actual upshift RPM use UpshiftRPM.
	/// </summary>
	[Property]
	public float TargetUpshiftRPM => _targetUpshiftRPM;
	private float _targetUpshiftRPM;

	public enum TransmissionShiftType
	{
		Manual,
		Automatic,
		CVT,
	}

	/// <summary>
	///     Determines in which way gears can be changed.
	///     Manual - gears can only be shifted by manual user input.
	///     Automatic - automatic gear changing. Allows for gear skipping (e.g. 3rd->5th) which can be useful in trucks and
	///     other high gear count vehicles.
	///     AutomaticSequential - automatic gear changing but only one gear at the time can be shifted (e.g. 3rd->4th)
	/// </summary>
	[Property]
	public TransmissionShiftType TransmissionType
	{
		get => transmissionType; set
		{
			transmissionType = value;
			AssignShiftDelegate();
		}
	}
	/// <summary>
	/// Is the automatic gearbox sequential?
	/// Has no effect on manual transmission.
	/// </summary>
	[Property] public bool IsSequential { get; set; } = false;

	[Property] public bool AllowUpshiftGearSkipping { get; set; }

	[Property] public bool AllowDownshiftGearSkipping { get; set; } = true;

	private bool _repeatInputFlag;
	private float _smoothedThrottleInput;

	/// <summary>
	///     Timer needed to prevent manual transmission from slipping out of gear too soon when hold in gear is enabled,
	///     which could happen in FixedUpdate() runs twice for one Update() and the shift flag is reset
	///     resulting in gearbox thinking it has no shift input.
	/// </summary>
	private float _slipOutOfGearTimer = -999f;


	/// <summary>
	///     0 for neutral, less than 0 for reverse gears and lager than 0 for forward gears.
	///     Use 'ShiftInto' to set gear.
	/// </summary>
	[Property]
	public int Gear
	{
		get => IndexToGear( gearIndex );
		set => gearIndex = GearToIndex( value );
	}


	/// <summary>
	/// Current gear index in the gears list.
	/// Different from gear because gear uses -1 = R, 0 = N and D = 1, while this is the apsolute index
	/// in the range of 0 to gear list size minus one.
	/// Use Gear to get the actual gear.
	/// </summary>
	public int gearIndex;
	private TransmissionShiftType transmissionType = TransmissionShiftType.Automatic;

	private int GearToIndex( int g )
	{
		return g + ReverseGearCount;
	}

	private int IndexToGear( int g )
	{
		return g - ReverseGearCount;
	}
	/// <summary>
	///     Returns current gear name as a string, e.g. "R", "R2", "N" or "1"
	/// </summary>
	public string GearName
	{
		get
		{
			int gear = Gear;

			if ( _gearNameCache.TryGetValue( gear, out string gearName ) )
				return gearName;

			if ( gear == 0 )
				gearName = "N";
			else if ( gear > 0 )
				gearName = Gear.ToString();
			else
				gearName = "R" + (ReverseGearCount > 1 ? -gear : "");

			_gearNameCache[gear] = gearName;
			return gearName;
		}
	}

	private readonly Dictionary<int, string> _gearNameCache = new();

	public void LoadGearsFromGearingProfile()
	{
		if ( GearingProfile == null )
			return;

		int totalGears = GearingProfile.ReverseGears.Count + 1 + GearingProfile.ForwardGears.Count;
		if ( Gears == null )
			Gears = new( totalGears );
		else
		{
			Gears.Clear();
			Gears.Capacity = totalGears;
		}

		Gears.AddRange( GearingProfile.ReverseGears );
		Gears.Add( 0 );
		Gears.AddRange( GearingProfile.ForwardGears );
	}
	protected override void OnStart()
	{
		base.OnStart();
		LoadGearsFromGearingProfile();
		UpdateGearCounts();
		Gear = 0;

		AssignShiftDelegate();
	}

	/// <summary>
	///     Total gear ratio of the transmission for current gear.
	/// </summary>
	private float CalculateTotalGearRatio()
	{

		if ( TransmissionType == TransmissionShiftType.CVT )
		{
			float minRatio = Gears[gearIndex];
			float maxRatio = minRatio * 40f;
			float t = Math.Clamp( Controller.Engine.RPMPercent + (1f - Controller.Engine.ThrottlePosition), 0, 1 );
			float ratio = MathX.Lerp( maxRatio, minRatio, t ) * FinalGearRatio;
			return MathX.Lerp( CurrentGearRatio, ratio, Time.Delta * 5f );
		}
		else
			return Gears[gearIndex] * FinalGearRatio;
	}
	public override float QueryAngularVelocity( float angularVelocity, float dt )
	{
		InputAngularVelocity = angularVelocity;

		if ( CurrentGearRatio == 0 || OutputNameHash == 0 )
		{
			OutputAngularVelocity = 0f;
			return angularVelocity;
		}

		OutputAngularVelocity = InputAngularVelocity / CurrentGearRatio;
		return _output.QueryAngularVelocity( OutputAngularVelocity, dt ) * CurrentGearRatio;
	}

	public override float QueryInertia()
	{
		if ( OutputNameHash == 0 || CurrentGearRatio == 0 )
			return Inertia;

		return Inertia + _output.QueryInertia() / (CurrentGearRatio * CurrentGearRatio);
	}


	/// <summary>
	/// Calculates the would-be RPM if none of the wheels was slipping.
	/// </summary>
	/// <returns>RPM as it would be if the wheels are not slipping or in the air.</returns>
	private float CalculateNoSlipRPM()
	{
		float vehicleLocalVelocity = Controller.LocalVelocity.x.InchToMeter();

		// Get the average no-slip wheel RPM
		// Use the vehicle velocity as the friction velocities for the wheel are 0 when in air and 
		// because the shift RPM is not really required to be extremely precise, so slight offset 
		// between the vehicle position and velocity and the wheel ones is not important.
		// Still, calculate for each wheel since radius might be different.
		float angVelSum = 0f;
		foreach ( WheelCollider wheelComponent in Controller.MotorWheels )
		{
			angVelSum += vehicleLocalVelocity / wheelComponent.Radius.InchToMeter();
		}

		// Apply total gear ratio to get the no-slip condition RPM
		return MathM.AngularVelocityToRPM( angVelSum / Controller.MotorWheels.Count ) * CurrentGearRatio;
	}

	public override float ForwardStep( float torque, float inertiaSum, float dt )
	{
		InputTorque = torque;
		InputInertia = inertiaSum;

		UpdateGearCounts();

		if ( _output == null )
			return InputTorque;

		// Update current gear ratio
		CurrentGearRatio = CalculateTotalGearRatio();

		// Run the shift function
		_referenceShiftRPM = CalculateNoSlipRPM();
		ShiftDelegate.Invoke( Controller );

		// Reset any input related to shifting, now that the shifting has been processed
		//Controller.Input.ResetShiftFlags();

		// Run the physics step
		// No output, simply return the torque to the sender
		if ( OutputNameHash == 0 )
			return torque;

		// In neutral, do not send any torque but update components downstram
		if ( CurrentGearRatio < 1e-5f && CurrentGearRatio > -1e-5f )
		{
			OutputTorque = 0;
			OutputInertia = InputInertia;
			_output.ForwardStep( OutputTorque, OutputInertia, dt );
			return torque;
		}

		// Always send torque to keep wheels updated
		OutputTorque = torque * CurrentGearRatio;
		OutputInertia = (inertiaSum + Inertia) * (CurrentGearRatio * CurrentGearRatio);
		return _output.ForwardStep( torque * CurrentGearRatio, OutputInertia, dt ) / CurrentGearRatio;
	}
	private void UpdateGearCounts()
	{
		ForwardGearCount = 0;
		ReverseGearCount = 0;
		int gearCount = Gears.Count;
		for ( int i = 0; i < gearCount; i++ )
		{
			float gear = Gears[i];
			if ( gear > 0 )
				ForwardGearCount++;
			else if ( gear < 0 )
				ReverseGearCount++;
		}
	}

	private void AssignShiftDelegate()
	{
		if ( TransmissionType == TransmissionShiftType.Manual )
			ShiftDelegate = ManualShift;
		else if ( TransmissionType == TransmissionShiftType.Automatic )
			ShiftDelegate = AutomaticShift;
		else if ( TransmissionType == TransmissionShiftType.CVT )
			ShiftDelegate = CVTShift;
	}
	private void ManualShift( VehicleController car )
	{
		if ( car.IsShiftingUp )
		{
			ShiftInto( Gear + 1 );
			return;
		}

		if ( car.IsShiftingDown )
		{
			ShiftInto( Gear - 1 );
			return;
		}

		if ( HoldToKeepInGear )
		{
			_slipOutOfGearTimer += Time.Delta;
			if ( Gear != 0 && _slipOutOfGearTimer > 0.1f )
				ShiftInto( 0 );
		}
	}
	/// <summary>
	///     Shifts into given gear. 0 for neutral, less than 0 for reverse and above 0 for forward gears.
	///     Does nothing if the target gear is equal to current gear.
	/// </summary>
	public void ShiftInto( int targetGear, bool instant = false )
	{
		// Clutch is not pressed above the set threshold, exit and do not shift.
		if ( Controller.IsClutching >= ClutchInputShiftThreshold )
			return;

		int currentGear = Gear;
		bool isShiftFromOrToNeutral = targetGear == 0 || currentGear == 0;

		//Debug.Log($"Shift from {currentGear} into {targetGear}");

		// Check if shift can happen at all
		if ( targetGear == currentGear || targetGear < -100 )
			return;

		// Convert gear to gear list index
		int targetIndex = GearToIndex( targetGear );

		// Check for gear list bounds
		if ( targetIndex < 0 || targetIndex >= Gears.Count )
			return;

		if ( !IsShifting && (isShiftFromOrToNeutral || !IsPostShiftBanActive) )
		{
			ShiftCoroutine( currentGear, targetGear, isShiftFromOrToNeutral || instant );

			// If in neutral reset the repeated input flat required for repeat input reverse
			if ( targetGear == 0 )
				_repeatInputFlag = false;
		}
	}

	private async void ShiftCoroutine( int currentGear, int targetGear, bool instant )
	{
		if ( IsShifting )
			return;

		float dt = Time.Delta;
		bool isManual = TransmissionType == TransmissionShiftType.Manual;

		//Debug.Log($"Shift from {currentGear} to {targetGear}, instant: {instant}");

		// Immediately start shift ban to prevent repeated shifts while this one has not finished
		if ( !isManual )
			IsPostShiftBanActive = true;

		IsShifting = true;
		ShiftProgress = 0f;

		// Run the first half of shift timer
		float shiftTimer = 0;
		float halfDuration = ShiftDuration * 0.5f;
		if ( !instant )
			while ( shiftTimer < halfDuration )
			{
				ShiftProgress = shiftTimer / ShiftDuration;
				shiftTimer += dt;
				await GameTask.DelayRealtimeSeconds( dt );
			}

		// Do the shift at the half point of shift duration
		Gear = targetGear;
		if ( currentGear < targetGear )
			OnGearUpShift?.Invoke();
		else
			OnGearDownShift?.Invoke();

		OnGearShift?.Invoke();

		// Run the second half of the shift timer
		if ( !instant )
			while ( shiftTimer < ShiftDuration )
			{
				ShiftProgress = shiftTimer / ShiftDuration;
				shiftTimer += dt;
				await GameTask.DelayRealtimeSeconds( dt );
			}


		// Shift has finished
		ShiftProgress = 1f;
		IsShifting = false;

		// Run post shift ban only if not manual as blocking user input feels unresponsive and post shift ban
		// exists to prevent auto transmission from hunting.
		if ( !isManual )
		{
			// Post shift ban timer
			float postShiftBanTimer = 0;
			while ( postShiftBanTimer < PostShiftBan )
			{
				postShiftBanTimer += dt;
				await GameTask.DelayRealtimeSeconds( dt );
			}

			// Post shift ban has finished
			IsPostShiftBanActive = false;
		}
	}
	private void CVTShift( VehicleController car ) => AutomaticShift( car );

	/// <summary>
	///     Handles automatic and automatic sequential shifting.
	/// </summary>
	private void AutomaticShift( VehicleController car )
	{
		float vehicleSpeed = car.CurrentSpeed;

		float throttleInput = car.SwappedThrottle;
		float brakeInput = car.SwappedBrakes;
		int currentGear = Gear;
		// Assign base shift points
		_targetDownshiftRPM = _downshiftRPM;
		_targetUpshiftRPM = _upshiftRPM;

		// Calculate shift points for variable shift RPM
		if ( VariableShiftPoint )
		{
			// Smooth throttle input so that the variable shift point does not shift suddenly and cause gear hunting
			_smoothedThrottleInput = MathX.Lerp( _smoothedThrottleInput, throttleInput, Time.Delta * 2f );
			float revLimiterRPM = car.Engine.RevLimiterRPM;

			_targetUpshiftRPM = _upshiftRPM + Math.Clamp( _smoothedThrottleInput * VariableShiftIntensity, 0f, 1f ) * _upshiftRPM;
			_targetUpshiftRPM = Math.Clamp( _targetUpshiftRPM, _upshiftRPM, revLimiterRPM * 0.97f );

			_targetDownshiftRPM = _downshiftRPM + Math.Clamp( _smoothedThrottleInput * VariableShiftIntensity, 0f, 1f ) * _downshiftRPM;
			_targetDownshiftRPM = Math.Clamp( _targetDownshiftRPM, car.Engine.IdleRPM * 1.1f, _targetUpshiftRPM * 0.7f );

			// Add incline modifier
			float inclineModifier = Math.Clamp( car.WorldRotation.Forward.Dot( Vector3.Up ) * InclineEffectCoeff, 0f, 1f );

			_targetUpshiftRPM += revLimiterRPM * inclineModifier;
			_targetDownshiftRPM += revLimiterRPM * inclineModifier;
		}


		// In neutral
		if ( currentGear == 0 )
		{
			if ( DNRShiftType == AutomaticTransmissionDNRShiftType.Auto )
			{
				if ( throttleInput > INPUT_DEADZONE )
					ShiftInto( 1 );
				else if ( brakeInput > INPUT_DEADZONE )
					ShiftInto( -1 );
			}
			else if ( DNRShiftType == AutomaticTransmissionDNRShiftType.RequireShiftInput )
			{
				if ( car.IsShiftingUp )
					ShiftInto( 1 );
				else if ( car.IsShiftingDown )
					ShiftInto( -1 );
			}
			else if ( DNRShiftType == AutomaticTransmissionDNRShiftType.RepeatInput )
			{
				if ( _repeatInputFlag == false && throttleInput < INPUT_DEADZONE && brakeInput < INPUT_DEADZONE )
					_repeatInputFlag = true;

				if ( _repeatInputFlag )
				{
					if ( throttleInput > INPUT_DEADZONE )
						ShiftInto( 1 );
					else if ( brakeInput > INPUT_DEADZONE )
						ShiftInto( -1 );
				}
			}
		}
		// In reverse
		else if ( currentGear < 0 )
		{
			// Shift into neutral
			if ( DNRShiftType == AutomaticTransmissionDNRShiftType.RequireShiftInput )
			{
				if ( car.IsShiftingUp )
					ShiftInto( 0 );
			}
			else
			{
				if ( vehicleSpeed < DnrSpeedThreshold && (brakeInput > INPUT_DEADZONE || throttleInput < INPUT_DEADZONE) )
					ShiftInto( 0 );
			}

			// Reverse upshift
			float absGearMinusOne = currentGear - 1;
			absGearMinusOne = absGearMinusOne < 0 ? -absGearMinusOne : absGearMinusOne;
			if ( _referenceShiftRPM > TargetUpshiftRPM && absGearMinusOne < ReverseGearCount )
				ShiftInto( currentGear - 1 );
			// Reverse downshift
			else if ( _referenceShiftRPM < TargetDownshiftRPM && currentGear < -1 )
				ShiftInto( currentGear + 1 );
		}
		// In forward
		else
		{
			if ( vehicleSpeed > 0.4f )
			{
				// Upshift
				if ( currentGear < ForwardGearCount && _referenceShiftRPM > TargetUpshiftRPM )
				{
					if ( !IsSequential && AllowUpshiftGearSkipping )
					{
						int g = currentGear;

						while ( g < ForwardGearCount )
						{
							g++;

							float wouldBeEngineRPM = ReverseTransmitRPM( _referenceShiftRPM / CurrentGearRatio, g );
							float shiftDurationPadding = Math.Clamp( ShiftDuration, 0, 1 ) * (_targetUpshiftRPM - _targetDownshiftRPM) * 0.25f;

							if ( wouldBeEngineRPM < _targetDownshiftRPM + shiftDurationPadding )
							{
								g--;
								break;
							}
						}
						if ( g != currentGear )
						{
							ShiftInto( g );
						}
					}
					else
					{
						ShiftInto( currentGear + 1 );
					}
				}
				// Downshift
				else if ( _referenceShiftRPM < TargetDownshiftRPM )
				{
					// Non-sequential
					if ( !IsSequential && AllowDownshiftGearSkipping )
					{
						if ( currentGear != 1 )
						{
							int g = currentGear;
							while ( g > 1 )
							{
								g--;
								float wouldBeEngineRPM = ReverseTransmitRPM( _referenceShiftRPM / CurrentGearRatio, g );
								if ( wouldBeEngineRPM > _targetUpshiftRPM )
								{
									g++;
									break;
								}
							}

							if ( g != currentGear )
							{
								ShiftInto( g );
							}
						}
						else if ( vehicleSpeed < DnrSpeedThreshold && throttleInput < INPUT_DEADZONE
																  && DNRShiftType !=
																  AutomaticTransmissionDNRShiftType
																	 .RequireShiftInput )
						{
							ShiftInto( 0 );
						}
					}
					// Sequential
					else
					{
						if ( currentGear != 1 )
						{
							ShiftInto( currentGear - 1 );
						}
						else if ( vehicleSpeed < DnrSpeedThreshold && throttleInput < INPUT_DEADZONE &&
								 brakeInput < INPUT_DEADZONE
								 && DNRShiftType !=
								 AutomaticTransmissionDNRShiftType.RequireShiftInput )
						{
							ShiftInto( 0 );
						}
					}
				}
			}
			// Shift into neutral
			else
			{
				if ( DNRShiftType != AutomaticTransmissionDNRShiftType.RequireShiftInput )
				{
					if ( throttleInput < INPUT_DEADZONE )
					{
						ShiftInto( 0 );
					}
				}
				else
				{
					if ( car.IsShiftingDown )
					{
						ShiftInto( 0 );
					}
				}
			}
		}
	}

	/// <summary>
	///     Converts axle RPM to engine RPM for given gear in Gears list.
	/// </summary>
	public float ReverseTransmitRPM( float inputRPM, int g )
	{
		float outRpm = inputRPM * Gears[GearToIndex( g )] * FinalGearRatio;
		return Math.Abs( outRpm );
	}

}
