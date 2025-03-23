using System;
using Sandbox;
namespace Meteor.VehicleTool.Vehicle.Wheel;

public partial class WheelCollider
{


	/// <summary>
	/// Constant torque acting similar to brake torque.
	/// Imitates rolling resistance.
	/// </summary>
	[Property, Range( 0, 500 ), Sync] public float RollingResistanceTorque { get; set; } = 230f;

	/// <summary>
	/// The percentage this wheel is contributing to the total vehicle load bearing.
	/// </summary>
	public float LoadContribution { get; set; } = 0.25f;

	/// <summary>
	/// Maximum load the tire is rated for in [N]. 
	/// Used to calculate friction.Default value is adequate for most cars but
	/// larger and heavier vehicles such as semi trucks will use higher values.
	/// A good rule of the thumb is that this value should be 2x the Load
	/// while vehicle is stationary.
	/// </summary>
	[Property, Sync] public float LoadRating { get; set; } = 5400;


	/// <summary>
	/// The amount of torque returned by the wheel.
	/// Under no-slip conditions this will be equal to the torque that was input.
	/// When there is wheel spin, the value will be less than the input torque.
	/// </summary>
	public float CounterTorque { get; private set; }

	[Property, Sync] public bool AutoSetFriction { get; set; } = true;
	[Property, Sync] public bool UseGroundVelocity { get; set; } = true;

	public PacejkaCurve FrictionPreset { get; set; } = PacejkaCurve.Asphalt;
	public float MotorTorque;
	public float BrakeTorque;
	public Friction ForwardFriction = new();
	public Friction SidewayFriction = new();
	public float RPM => Math.Abs( AngularVelocity * 9.55f );
	public float AngularVelocity = 0;
	public Vector3 FrictionForce;


	private Vector3 hitContactVelocity;
	private Vector3 hitForwardDirection;
	private Vector3 hitSidewaysDirection;
	private Rotation velocityRotation;
	private float axleAngle;



	public float LongitudinalSlip => ForwardFriction.Slip;
	public float LongitudinalSpeed => ForwardFriction.Speed;
	public virtual bool IsSkiddingLongitudinally => NormalizedLongitudinalSlip > 0.35f;
	public virtual float NormalizedLongitudinalSlip
	{
		get
		{
			float lngSlip = LongitudinalSlip;
			float absLngSlip = lngSlip < 0f ? -lngSlip : lngSlip;
			return absLngSlip < 0f ? 0f : absLngSlip > 1f ? 1f : absLngSlip;
		}
	}


	public float LateralSlip => SidewayFriction.Slip;
	public float LateralSpeed => SidewayFriction.Speed;
	public virtual bool IsSkiddingLaterally => NormalizedLateralSlip > 0.35f;

	public virtual float NormalizedLateralSlip
	{
		get
		{
			float latSlip = LateralSlip;
			float absLatSlip = latSlip < 0f ? -latSlip : latSlip;
			return absLatSlip < 0f ? 0f : absLatSlip > 1f ? 1f : absLatSlip;
		}
	}

	private void UpdateHitVariables()
	{
		if ( IsGrounded )
		{
			hitContactVelocity = CarBody.GetVelocityAtPoint( GroundHit.Point + CarBody.MassCenter ) - GroundVelocity;

			hitForwardDirection = GroundHit.Normal.Cross( TransformRotationSteer.Right ).Normal;
			hitSidewaysDirection = Rotation.FromAxis( GroundHit.Normal, 90f ) * hitForwardDirection;

			ForwardFriction.Speed = hitContactVelocity.Dot( hitForwardDirection ).InchToMeter();
			SidewayFriction.Speed = hitContactVelocity.Dot( hitSidewaysDirection ).InchToMeter();
		}
		else
		{
			ForwardFriction.Speed = 0f;
			SidewayFriction.Speed = 0f;
		}
	}

	public void UpdateFrictionPreset()
	{

		_ = Enum.TryParse( GroundHit.Surface.ResourceName, ignoreCase: true, out PacejkaPreset preset );

		FrictionPreset = PacejkaCurve.GetPreset( preset );

	}
	public Vector3 GroundVelocity { get; set; }
	private void UpdateGroundVelocity()
	{
		if ( GroundHit.Collider == null )
		{
			GroundVelocity = 0f;
			return;
		}

		if ( GroundHit.Collider is Collider collider )
			GroundVelocity = collider.GetVelocityAtPoint( WorldPosition );


	}

	private Vector3 lowSpeedReferencePosition;
	private bool lowSpeedReferenceIsSet;
	private Vector3 currentPosition;
	private Vector3 referenceError;
	private Vector3 correctiveForce;
	private void UpdateFriction( float dt )
	{
		if ( AutoSetFriction )
			UpdateFrictionPreset();

		if ( UseGroundVelocity )
			UpdateGroundVelocity();

		var motorTorque = MotorTorque;
		var brakeTorque = BrakeTorque;


		float allWheelLoadSum = Controller.CombinedLoad;

		LoadContribution = allWheelLoadSum == 0 ? 1f : Load / allWheelLoadSum;


		float mRadius = Radius.InchToMeter();

		float invDt = 1f / dt;
		float invRadius = 1f / mRadius;
		float inertia = Inertia;
		float invInertia = 1f / Inertia;

		float loadClamped = Load < 0f ? 0f : Load > LoadRating ? LoadRating : Load;
		float forwardLoadFactor = loadClamped * 1.35f;
		float sideLoadFactor = loadClamped * 1.9f;

		float loadPercent = Math.Clamp( Load / LoadRating, 0f, 1f );
		float slipLoadModifier = 1f - loadPercent * 0.4f;


		float mass = CarBody.PhysicsBody.Mass;
		float absForwardSpeed = Math.Abs( ForwardFriction.Speed );
		float forwardForceClamp = mass * LoadContribution * absForwardSpeed * invDt;
		float absSideSpeed = Math.Abs( SidewayFriction.Speed );
		float sideForceClamp = mass * LoadContribution * absSideSpeed * invDt;

		float forwardSpeedClamp = 1.5f * (dt / 0.005f);
		forwardSpeedClamp = Math.Clamp( forwardSpeedClamp, 1.5f, 10f );
		float clampedAbsForwardSpeed = Math.Max( absForwardSpeed, forwardSpeedClamp );

		// Calculate effect of camber on friction
		float camberFrictionCoeff = Math.Max( 0, WorldRotation.Up.Dot( GroundHit.Normal ) );


		float peakForwardFrictionForce = FrictionPreset.PeakValue * forwardLoadFactor * ForwardFriction.Grip;
		float absCombinedBrakeTorque = Math.Max( 0, brakeTorque + RollingResistanceTorque );
		float signedCombinedBrakeTorque = absCombinedBrakeTorque * -Math.Sign( ForwardFriction.Speed );
		float signedCombinedBrakeForce = signedCombinedBrakeTorque * invRadius;
		float motorForce = motorTorque * invRadius;
		float forwardInputForce = motorForce + signedCombinedBrakeForce;
		float absMotorTorque = Math.Abs( motorTorque );
		float absBrakeTorque = Math.Abs( brakeTorque );

		float maxForwardForce = Math.Min( peakForwardFrictionForce, forwardForceClamp );
		maxForwardForce = absMotorTorque < absBrakeTorque ? maxForwardForce : peakForwardFrictionForce;
		ForwardFriction.Force = forwardInputForce > maxForwardForce ? maxForwardForce
			: forwardInputForce < -maxForwardForce ? -maxForwardForce : forwardInputForce;

		bool wheelIsBlocked = false;
		if ( IsGrounded )
		{
			float combinedWheelForce = motorForce + absCombinedBrakeTorque * invRadius * -Math.Sign( AngularVelocity );
			float wheelForceClampOverflow = 0;
			if ( (combinedWheelForce >= 0 && AngularVelocity < 0) || (combinedWheelForce < 0 && AngularVelocity > 0) )
			{
				float absWheelForceClamp = Math.Abs( AngularVelocity ) * inertia * invRadius * invDt;
				float absCombinedWheelForce = combinedWheelForce < 0 ? -combinedWheelForce : combinedWheelForce;
				float wheelForceDiff = absCombinedWheelForce - absWheelForceClamp;
				wheelForceClampOverflow = Math.Max( 0, wheelForceDiff ) * Math.Sign( combinedWheelForce );
				combinedWheelForce = Math.Clamp( combinedWheelForce, -absWheelForceClamp, absWheelForceClamp );
			}
			AngularVelocity += combinedWheelForce * mRadius * invInertia * dt;

			// Surface (corrective) force
			float noSlipAngularVelocity = ForwardFriction.Speed * invRadius;
			float angularVelocityError = AngularVelocity - noSlipAngularVelocity;
			float angularVelocityCorrectionForce = Math.Clamp( -angularVelocityError * inertia * invRadius * invDt, -maxForwardForce, maxForwardForce );

			if ( absMotorTorque < absBrakeTorque && Math.Abs( wheelForceClampOverflow ) > Math.Abs( angularVelocityCorrectionForce ) )
			{
				wheelIsBlocked = true;
				AngularVelocity += ForwardFriction.Speed > 0 ? 1e-10f : -1e-10f;
			}
			else
			{
				AngularVelocity += angularVelocityCorrectionForce * mRadius * invInertia * dt;
			}
		}
		else
		{
			float maxBrakeTorque = AngularVelocity * inertia * invDt + motorTorque;
			maxBrakeTorque = maxBrakeTorque < 0 ? -maxBrakeTorque : maxBrakeTorque;
			float brakeTorqueSign = AngularVelocity < 0f ? -1f : 1f;
			float clampedBrakeTorque = Math.Clamp( absCombinedBrakeTorque, -maxBrakeTorque, maxBrakeTorque );
			AngularVelocity += (motorTorque - brakeTorqueSign * clampedBrakeTorque) * invInertia * dt;
		}

		float absAngularVelocity = AngularVelocity < 0 ? -AngularVelocity : AngularVelocity;
		float maxCounterTorque = inertia * absAngularVelocity;
		CounterTorque = Math.Clamp( (signedCombinedBrakeForce - ForwardFriction.Force) * mRadius, -maxCounterTorque, maxCounterTorque );

		ForwardFriction.Slip = (ForwardFriction.Speed - AngularVelocity * mRadius) / clampedAbsForwardSpeed;
		ForwardFriction.Slip *= slipLoadModifier;


		SidewayFriction.Slip = MathF.Atan2( SidewayFriction.Speed, clampedAbsForwardSpeed ).RadianToDegree() * 0.01111f;
		SidewayFriction.Slip *= slipLoadModifier;

		float sideSlipSign = Math.Sign( SidewayFriction.Slip );
		float absSideSlip = Math.Abs( SidewayFriction.Slip );
		float peakSideFrictionForce = FrictionPreset.PeakValue * sideLoadFactor * SidewayFriction.Grip;
		float sideForce = -sideSlipSign * FrictionPreset.Evaluate( absSideSlip ) * sideLoadFactor * SidewayFriction.Grip;
		SidewayFriction.Force = Math.Clamp( sideForce, -sideForceClamp, sideForceClamp );
		SidewayFriction.Force *= camberFrictionCoeff;

		if ( IsGrounded && absForwardSpeed < 0.12f && absSideSpeed < 0.12f )
		{
			float verticalOffset = suspensionTotalLength.InchToMeter() + mRadius;
			var transformPosition = WorldPosition;

			var transformUp = TransformRotationSteer.Up;
			currentPosition.x = transformPosition.x - transformUp.x * verticalOffset;
			currentPosition.y = transformPosition.y - transformUp.y * verticalOffset;
			currentPosition.z = transformPosition.z - transformUp.z * verticalOffset;

			if ( !lowSpeedReferenceIsSet )
			{
				lowSpeedReferenceIsSet = true;
				lowSpeedReferencePosition = currentPosition;
			}
			else
			{
				if ( GroundHit.Collider != null )
				{
					lowSpeedReferencePosition += GroundVelocity * dt;
				}
				referenceError.x = lowSpeedReferencePosition.x - currentPosition.x;
				referenceError.y = lowSpeedReferencePosition.y - currentPosition.y;
				referenceError.z = lowSpeedReferencePosition.z - currentPosition.z;

				correctiveForce.x = invDt * LoadContribution * mass * referenceError.x;
				correctiveForce.y = invDt * LoadContribution * mass * referenceError.y;
				correctiveForce.z = invDt * LoadContribution * mass * referenceError.z;

				if ( wheelIsBlocked && absAngularVelocity < 0.5f )
				{
					ForwardFriction.Force += correctiveForce.Dot( hitForwardDirection );
				}
				SidewayFriction.Force += correctiveForce.Dot( hitSidewaysDirection );

			}

		}
		else
		{
			lowSpeedReferenceIsSet = false;
		}

		ForwardFriction.Force = Math.Clamp( ForwardFriction.Force, -peakForwardFrictionForce, peakForwardFrictionForce );
		SidewayFriction.Force = Math.Clamp( SidewayFriction.Force, -peakSideFrictionForce, peakSideFrictionForce );

		if ( absForwardSpeed > 2f || absAngularVelocity > 4f )
		{
			float forwardSlipPercent = ForwardFriction.Slip / FrictionPreset.PeakSlip;
			float sideSlipPercent = SidewayFriction.Slip / FrictionPreset.PeakSlip;
			float slipCircleLimit = MathF.Sqrt( forwardSlipPercent * forwardSlipPercent + sideSlipPercent * sideSlipPercent );
			if ( slipCircleLimit > 1f )
			{
				float beta = MathF.Atan2( sideSlipPercent, forwardSlipPercent * 1.75f );
				float sinBeta = MathF.Sin( beta );
				float cosBeta = MathF.Cos( beta );

				float absForwardForce = ForwardFriction.Force < 0 ? -ForwardFriction.Force : ForwardFriction.Force;

				float absSideForce = SidewayFriction.Force < 0 ? -SidewayFriction.Force : SidewayFriction.Force;
				float f = absForwardForce * cosBeta * cosBeta + absSideForce * sinBeta * sinBeta;

				ForwardFriction.Force = 0.5f * ForwardFriction.Force - 1f * f * cosBeta;
				SidewayFriction.Force = 0.5f * SidewayFriction.Force - 1f * f * sinBeta;
			}
		}

		if ( IsGrounded )
		{
			FrictionForce.x = (hitSidewaysDirection.x * SidewayFriction.Force + hitForwardDirection.x * ForwardFriction.Force).MeterToInch();
			FrictionForce.y = (hitSidewaysDirection.y * SidewayFriction.Force + hitForwardDirection.y * ForwardFriction.Force).MeterToInch();
			FrictionForce.z = (hitSidewaysDirection.z * SidewayFriction.Force + hitForwardDirection.z * ForwardFriction.Force).MeterToInch();

			Vector3 forcePosition = GroundHit.Point + TransformRotationSteer.Up * 0.8f * MaxSuspensionLength;
			CarBody.ApplyForceAt( forcePosition, FrictionForce );
		}
		else
			FrictionForce = Vector3.Zero;
	}
}
