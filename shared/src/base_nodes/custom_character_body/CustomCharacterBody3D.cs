using System;
using System.Collections.Generic;
using Godot;

[Tool]
[GlobalClass]
public partial class CustomCharacterBody3D : KinematicBody3D
{
    /// <summary>
    /// Vector pointing upwards, used to determine what is a wall and what is a floor (or a ceiling) when calling <c>Move(double)</c>. Defaults to <c>Vector3.UP</c>. As the vector will be normalized it can't be equal to <c>Vector3.ZERO</c>, if you want all collisions to be reported as walls, consider using <c>MotionModeEnum.Floating</c> as <c>MotionMode</c>.
    /// </summary>
    [Export] public Vector3 UpDirection = Vector3.Up;
    /// <summary>
    /// If <c>true</c>, during a jump against the ceiling, the body will slide, if <c>false</c> it will be stopped and will fall vertically.
    /// </summary>
    [Export] public bool SlideOnCeiling = true;
    /// <summary>
    /// Minimum angle (in radians) where the body is allowed to slide when it encounters a slope. The default value equals 15 degrees. When <c>MotionMode</c> is <c>MotionModeEnum.Grounded</c>, it only affects movement if <c>FloorBlockOnWall</c> is <c>true</c>.
    /// </summary>
    [Export(PropertyHint.Range, "0,180,0.1,radians_as_degrees")] public float WallMinSlideAngle = Mathf.DegToRad(15);

    /// <summary>
    /// <para> If <c>true</c>, the body will not slide on slopes when calling <c>Move(double)</c> when the body is standing still. </para>
	/// <para> If <c>false</c>, the body will slide on floor's slopes when <c>Velocity</c> applies a downward force. </para>
    /// </summary>
    [ExportGroup("Floor")]
    [Export] public bool FloorStopOnSlope = true;
    /// <summary>
    /// <para> If <c>false</c> (by default), the body will move faster on downward slopes and slower on upward slopes. </para>
	/// <para> If <c>true</c>, the body will always move at the same speed on the ground no matter the slope. Note that you need to use <c>FloorSnapLength</c> to stick along a downward slope at constant speed. </para>
    /// </summary>
    [Export] public bool FloorConstantSpeed = false;
    /// <summary>
    /// If <c>true</c>, the body will be able to move on the floor only. This option avoids to be able to walk on walls, it will however allow to slide down along them.
    /// </summary>
    [Export] public bool FloorBlockOnWall = true;
    /// <summary>
    /// Maximum angle (in radians) where a slope is still considered a floor (or a ceiling), rather than a wall, when calling <c>Move(double)</c>. The default value equals 45 degrees.
    /// </summary>
    [Export(PropertyHint.Range, "0,180,0.1,radians_as_degrees")] public float FloorMaxAngle = Mathf.DegToRad(45);
    /// <summary>
    /// <para> Sets a snapping distance. When set to a value different from <c>0.0</c>, the body is kept attached to slopes when calling <c>Move(double)</c>. The snapping vector is determined by the given distance along the opposite direction of the <c>UpDirection</c>. </para>
    /// <para> As long as the snapping vector is in contact with the ground and the body moves against <c>UpDirection</c>, the body will remain attached to the surface. Snapping is not applied if the body moves along <c>UpDirection</c>, meaning it contains vertical rising velocity, so it will be able to detach from the ground when jumping or when the body is pushed up by something. If you want to apply a snap without taking into account the velocity, use <c>ApplyFloorSnap()</c>. </para>
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.01,or_greater,suffix:m")] public float FloorSnapLength = 0.1f;

    /// <summary>
    /// Sets the behavior to apply when you leave a moving platform. By default, to be physically accurate, when you leave the last platform velocity is applied. See <c>PlatformOnLeaveEnum</c> constants for available behavior.
    /// </summary>
    [ExportGroup("Moving Platform")]
    [Export] public PlatformOnLeaveEnum PlatformOnLeave = PlatformOnLeaveEnum.AddVelocity;
    /// <summary>
    /// Collision layers that will be included for detecting floor bodies that will act as moving platforms to be followed by the <c>AdvancedCharacterBody3D</c>. By default, all floor bodies are detected and propagate their velocity.
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)] public uint PlatformFloorLayers = uint.MaxValue;
    /// <summary>
    /// Collision layers that will be included for detecting wall bodies that will act as moving platforms to be followed by the <c>AdvancedCharacterBody3D</c>. By default, all wall bodies are ignored.
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)] public uint PlatformWallLayers = 0;

    /// <summary>
    /// <para> Extra margin used for collision recovery when calling <c>Move(double)</c>. </para>
    /// <para> If the body is at least this close to another body, it will consider them to be colliding and will be pushed away before performing the actual motion. </para>
    /// <para> A higher value means it's more flexible for detecting collision, which helps with consistently detecting walls and floors. </para>
    /// <para> A lower value forces the collision algorithm to use more exact detection, so it can be used in cases that specifically require precision, e.g at very low scale to avoid visible jittering, or for stability with a stack of character bodies. </para>
    /// </summary>
    [ExportGroup("Collision")]
    [Export(PropertyHint.Range, "0,1,0.01,or_greater,suffix:m")] public float SafeMargin = 0.001f;

    /// <summary>
    /// Maximum number of times the body can change direction before it stops when calling <c>Move(double)</c>.
    /// </summary>
    public int MaxSlides = 6;
    /// <summary>
    /// Current velocity vector (typically meters per second), used and modified during calls to <c>Move(double)</c>.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Returns <c>true</c> if the body collided with the floor on the last call of <c>Move(double)</c>. Otherwise, returns <c>false</c>. The <c>UpDirection</c> and <c>FloorMaxAngle</c> are used to determine whether a surface is "floor" or not.
    /// </summary>
    public bool IsOnFloor { get; private set; }
    /// <summary>
    /// Returns <c>true</c> if the body collided with a wall on the last call of <c>Move(double)</c>. Otherwise, returns <c>false</c>. The <c>UpDirection</c> and <c>FloorMaxAngle</c> are used to determine whether a surface is "wall" or not.
    /// </summary>
    public bool IsOnWall { get; private set; }
    /// <summary>
    /// Returns <c>true</c> if the body collided with the ceiling on the last call of <c>Move(double)</c>. Otherwise, returns <c>false</c>. The <c>UpDirection</c> and <c>FloorMaxAngle</c> are used to determine whether a surface is "ceiling" or not.
    /// </summary>
    public bool IsOnCeiling { get; private set; }
    /// <summary>
    /// Returns the collision normal of the floor at the last collision point. Only valid after calling <c>Move(double)</c> and when <c>IsOnFloor</c> returns <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Warning: The collision normal is not always the same as the surface normal.
    /// </remarks>
    public Vector3 FloorNormal { get; private set; }
    /// <summary>
    /// Returns the collision normal of the wall at the last collision point. Only valid after calling <c>Move(double)</c> and when <c>IsOnWall</c> returns <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Warning: The collision normal is not always the same as the surface normal.
    /// </remarks>
    public Vector3 WallNormal { get; private set; }
    /// <summary>
    /// Returns the last motion applied to the <c>AdvancedCharacterBody3D</c> during the last call to <c>Move(double)</c>. The movement can be split into multiple motions when sliding occurs, and this method return the last one, which is useful to retrieve the current direction of the movement.
    /// </summary>
    public Vector3 LastMotion { get; private set; }
    /// <summary>
    /// Returns the linear velocity of the platform at the last collision point. Only valid after calling <c>Move(double)</c>.
    /// </summary>
    public Vector3 PlatformVelocity { get; private set; }

    private uint PlatformLayer = 0;
    private Rid PlatformRid;
    private ulong PlatformObjectId;
    private Vector3 CeilingNormal;
    private Vector3 PlatformCeilingVelocity;
    private readonly List<KinematicCollision3D> MotionCollisions = [];

    private const float FloorAngleThreshold = 0.01f;
    private const float CmpEpsilon = 0.00001f;

    public override void _EnterTree()
    {
        ClearCollisionStates();
        PlatformRid = new();
        PlatformObjectId = 0;
        MotionCollisions.Clear();
        PlatformVelocity = Vector3.Zero;
    }

    public override void _MoveImplementation(double delta)
    {
        float floatDelta = (float)delta;

        if (AxisLockLinearX)
            Velocity.X = 0;
        if (AxisLockLinearY)
            Velocity.Y = 0;
        if (AxisLockLinearZ)
            Velocity.Z = 0;

        var gt = GlobalTransform;

        Vector3 currentPlatformVelocity = PlatformVelocity;

        if ((IsOnFloor || IsOnWall) && PlatformRid.IsValid)
        {
            bool excluded = false;
            if (IsOnFloor)
                excluded = (PlatformFloorLayers & PlatformLayer) == 0;
            else if (IsOnWall)
                excluded = (PlatformWallLayers & PlatformLayer) == 0;

            if (!excluded)
            {
                PhysicsDirectBodyState3D platformBodyState = null;

                // We need to check the PlatformRid object still exists before accessing.
                // A valid RID is no guarantee that the object has not been deleted.

                // We can only perform the ObjectDB lifetime check on Object derived objects.
                // Note that physics also creates RIDs for non-Object derived objects, these cannot
                // be lifetime checked through ObjectDB, and therefore there is a still a vulnerability
                // to dangling RIDs (access after free) in this scenario.
                if (PlatformObjectId == 0 || InstanceFromId(PlatformObjectId) != null)
                {
                    // This approach makes sure there is less delay between the actual body velocity and the one we saved.
                    platformBodyState = PhysicsServer3D.BodyGetDirectState(PlatformRid);
                }

                if (platformBodyState != null)
                {
                    Vector3 localPosition = gt.Origin - platformBodyState.Transform.Origin;
                    currentPlatformVelocity = platformBodyState.GetVelocityAtLocalPosition(localPosition);
                }
                else
                {
                    currentPlatformVelocity = Vector3.Zero;
                    PlatformRid = new();
                }
            }
            else currentPlatformVelocity = Vector3.Zero;
        }

        MotionCollisions.Clear();

        bool wasOnFloor = IsOnFloor;
        ClearCollisionStates();

        LastMotion = Vector3.Zero;

        if (!currentPlatformVelocity.IsZeroApprox())
        {
            var parameters = new PhysicsTestMotionParameters3D()
            {
                From = GlobalTransform,
                Motion = currentPlatformVelocity * floatDelta,
                Margin = SafeMargin,
                RecoveryAsCollision = true, // Also report collisions generated only from recovery.
                ExcludeBodies = [PlatformRid]
            };

            // This commented code does not work cause of engine flaw - ExcludeObjects is int array when object ids are random ulong values
            // if (PlatformObjectId != 0)
            //     parameters.ExcludeObjects = [PlatformObjectId];

            var collision = MoveAndCollideExtended(parameters, false, false);
            if (collision != null)
            {
                MotionCollisions.Add(collision);
                SetCollisionDirection(collision);
            }
        }

        MoveGrounded(floatDelta, wasOnFloor);

        if (PlatformOnLeave != PlatformOnLeaveEnum.DoNothing)
        {
            // Add last platform velocity when just left a moving platform.
            if (!IsOnFloor && !IsOnWall)
            {
                if (PlatformOnLeave == PlatformOnLeaveEnum.AddUpwardVelocity && currentPlatformVelocity.Dot(UpDirection) < 0)
                    currentPlatformVelocity = currentPlatformVelocity.Slide(UpDirection);
                Velocity += currentPlatformVelocity;
            }
        }
    }

    private void MoveGrounded(float delta, bool wasOnFloor)
    {
        Vector3 motion = Velocity * delta;
        Vector3 motionSlideUp = motion.Slide(UpDirection);
        Vector3 prevFloorNormal = FloorNormal;

        PlatformRid = new();
        PlatformObjectId = 0;
        PlatformVelocity = Vector3.Zero;
        PlatformCeilingVelocity = Vector3.Zero;
        FloorNormal = Vector3.Zero;
        WallNormal = Vector3.Zero;
        CeilingNormal = Vector3.Zero;

        // No sliding on first attempt to keep floor motion stable when possible,
        // When stop on slope is enabled or when there is no up direction.
        bool slidingEnabled = !FloorStopOnSlope;
        // Constant speed can be applied only the first time sliding is enabled.
        bool canApplyConstantSpeed = slidingEnabled;
        // If the platform's ceiling push down the body.
        bool applyCeilingVelocity = false;
        bool firstSlide = true;
        bool velDirFacingUp = Velocity.Dot(UpDirection) > 0;
        Vector3 totalTravel = Vector3.Zero;

        for (int i = 0; i < MaxSlides; ++i)
        {
            var parameters = new PhysicsTestMotionParameters3D()
            {
                From = GlobalTransform,
                Motion = motion,
                Margin = SafeMargin,
                MaxCollisions = 6, // There can be 4 collisions between 2 walls + 2 more for the floor.
                RecoveryAsCollision = true // Also report collisions generated only from recovery.
            };
            var collision = MoveAndCollideExtended(parameters, false, !slidingEnabled);
            bool collided = collision != null;

            LastMotion = motion;
            if (collided)
            {
                LastMotion = collision.GetTravel();

                MotionCollisions.Add(collision);

                bool wasOnWall = IsOnWall;
                var resultState = SetCollisionDirection(collision);

                // If we hit a ceiling platform, we set the vertical velocity to at least the platform one.
                if (IsOnCeiling && PlatformCeilingVelocity != Vector3.Zero && PlatformCeilingVelocity.Dot(UpDirection) < 0)
                {
                    // If ceiling sliding is on, only apply when the ceiling is flat or when the motion is upward.
                    if (!SlideOnCeiling || motion.Dot(UpDirection) < 0 || (CeilingNormal + UpDirection).Length() < 0.01f)
                    {
                        applyCeilingVelocity = true;
                        Vector3 ceilingVerticalVelocity = UpDirection * UpDirection.Dot(PlatformCeilingVelocity);
                        Vector3 motionVerticalVelocity = UpDirection * UpDirection.Dot(Velocity);
                        if (motionVerticalVelocity.Dot(UpDirection) > 0 || ceilingVerticalVelocity.LengthSquared() > motionVerticalVelocity.LengthSquared())
                            Velocity = ceilingVerticalVelocity + Velocity.Slide(UpDirection);
                    }
                }

                if (IsOnFloor && FloorStopOnSlope && (Velocity.Normalized() + UpDirection).Length() < 0.01f)
                {
                    var gt = GlobalTransform;
                    if (collision.GetTravel().Length() <= SafeMargin + CmpEpsilon)
                        gt.Origin -= collision.GetTravel();

                    GlobalTransform = gt;
                    Velocity = Vector3.Zero;
                    LastMotion = Vector3.Zero;
                    break;
                }

                if (collision.GetRemainder().IsZeroApprox())
                    break;

                // Apply regular sliding by default.
                bool applyDefaultSliding = true;

                // Wall collision checks.
                if (resultState.IsOnWall && (motionSlideUp.Dot(WallNormal) <= 0))
                {
                    // Move on floor only checks.
                    if (FloorBlockOnWall)
                    {
                        // Needs horizontal motion from current motion instead of motionSlideUp
                        // to properly test the angle and avoid standing on slopes
                        Vector3 horizontalMotion = motion.Slide(UpDirection);
                        Vector3 horizontalNormal = WallNormal.Slide(UpDirection).Normalized();
                        float motionAngle = Mathf.Abs(Mathf.Acos(-horizontalNormal.Dot(horizontalMotion.Normalized())));

                        // Avoid to move forward on a wall if FloorBlockOnWall is true.
                        // Applies only when the motion angle is under 90 degrees,
                        // in order to avoid blocking lateral motion along a wall.
                        if (motionAngle < 0.5 * Math.PI)
                        {
                            applyDefaultSliding = false;
                            if (wasOnFloor && !velDirFacingUp)
                            {
                                // Cancel the motion.
                                var gt = GlobalTransform;
                                float travelTotal = collision.GetTravel().Length();
                                float cancelDistMax = Mathf.Min(0.1f, SafeMargin * 20);
                                if (travelTotal <= SafeMargin + CmpEpsilon)
                                {
                                    gt.Origin -= collision.GetTravel();
                                    collision.SetTravel(Vector3.Zero); // Cancel for constant speed computation.
                                }
                                else if (travelTotal < cancelDistMax) // If the movement is large the body can be prevented from reaching the walls.
                                {
                                    gt.Origin -= collision.GetTravel().Slide(UpDirection);
                                    // Keep remaining motion in sync with amount canceled.
                                    motion = motion.Slide(UpDirection);
                                    collision.SetTravel(Vector3.Zero);
                                }
                                else
                                {
                                    // Travel is too high to be safely canceled, we take it into account.
                                    collision.SetTravel(collision.GetTravel().Slide(UpDirection));
                                    motion = collision.GetRemainder();
                                }
                                GlobalTransform = gt;
                                // Determines if you are on the ground, and limits the possibility of climbing on the walls because of the approximations.
                                SnapOnFloor(true, false);
                            }
                            else
                            {
                                // If the movement is not canceled we only keep the remaining.
                                motion = collision.GetRemainder();
                            }

                            // Apply slide on forward in order to allow only lateral motion on next step.
                            Vector3 forward = WallNormal.Slide(UpDirection).Normalized();
                            motion = motion.Slide(forward);

                            // Scales the horizontal velocity according to the wall slope.
                            if (velDirFacingUp)
                            {
                                Vector3 slideMotion = Velocity.Slide(collision.GetNormal(0));
                                // Keeps the vertical motion from velocity and add the horizontal motion of the projection.
                                Velocity = UpDirection * UpDirection.Dot(Velocity) + slideMotion.Slide(UpDirection);
                            }
                            else
                            {
                                Velocity = Velocity.Slide(forward);
                            }

                            // Allow only lateral motion along previous floor when already on floor.
                            // Fixes slowing down when moving in diagonal against an inclined wall.
                            if (wasOnFloor && !velDirFacingUp && (motion.Dot(UpDirection) > 0))
                            {
                                // Slide along the corner between the wall and previous floor.
                                Vector3 floorSide = prevFloorNormal.Cross(WallNormal);
                                if (floorSide != Vector3.Zero)
                                    motion = floorSide * motion.Dot(floorSide);
                            }

                            // Stop all motion when a second wall is hit (unless sliding down or jumping),
                            // in order to avoid jittering in corner cases.
                            bool stopAllMotion = wasOnWall && !velDirFacingUp;

                            // Allow sliding when the body falls.
                            if (!IsOnFloor && motion.Dot(UpDirection) < 0)
                            {
                                Vector3 slideMotion = motion.Slide(WallNormal);
                                // Test again to allow sliding only if the result goes downwards.
                                // Fixes jittering issues at the bottom of inclined walls.
                                if (slideMotion.Dot(UpDirection) < 0)
                                {
                                    stopAllMotion = false;
                                    motion = slideMotion;
                                }
                            }

                            if (stopAllMotion)
                            {
                                motion = Vector3.Zero;
                                Velocity = Vector3.Zero;
                            }
                        }
                    }

                    // Stop horizontal motion when under wall slide threshold.
                    if (wasOnFloor && (WallMinSlideAngle > 0) && resultState.IsOnWall)
                    {
                        Vector3 horizontalNormal = WallNormal.Slide(UpDirection).Normalized();
                        float motionAngle = Mathf.Abs(Mathf.Acos(-horizontalNormal.Dot(motionSlideUp.Normalized())));
                        if (motionAngle < WallMinSlideAngle)
                        {
                            motion = UpDirection * motion.Dot(UpDirection);
                            Velocity = UpDirection * Velocity.Dot(UpDirection);

                            applyDefaultSliding = false;
                        }
                    }
                }

                if (applyDefaultSliding)
                {
                    // Regular sliding, the last part of the test handle the case when you don't want to slide on the ceiling.
                    if ((slidingEnabled || !IsOnFloor) && (!IsOnCeiling || SlideOnCeiling || !velDirFacingUp) && !applyCeilingVelocity)
                    {
                        Vector3 slideMotion = collision.GetRemainder().Slide(collision.GetNormal(0));
                        if (IsOnFloor && !IsOnWall && !motionSlideUp.IsZeroApprox())
                        {
                            // Slide using the intersection between the motion plane and the floor plane,
                            // in order to keep the direction intact.
                            float motionLength = slideMotion.Length();
                            slideMotion = UpDirection.Cross(collision.GetRemainder()).Cross(FloorNormal);

                            // Keep the Length from default slide to change speed in slopes by default,
                            // when constant speed is not enabled.
                            slideMotion = slideMotion.Normalized();
                            slideMotion *= motionLength;
                        }

                        motion = slideMotion.Dot(Velocity) > 0 ? slideMotion : Vector3.Zero;

                        if (SlideOnCeiling && resultState.IsOnCeiling)
                        {
                            // Apply slide only in the direction of the input motion, otherwise just stop to avoid jittering when moving against a wall.
                            if (velDirFacingUp)
                                Velocity = Velocity.Slide(collision.GetNormal(0));
                            else // Avoid acceleration in slope when falling.
                                Velocity = UpDirection * UpDirection.Dot(Velocity);
                        }
                    }
                    // No sliding on first attempt to keep floor motion stable when possible.
                    else
                    {
                        motion = collision.GetRemainder();
                        if (resultState.IsOnCeiling && !SlideOnCeiling && velDirFacingUp)
                        {
                            Velocity = Velocity.Slide(UpDirection);
                            motion = motion.Slide(UpDirection);
                        }
                    }
                }

                totalTravel += collision.GetTravel();

                // Apply Constant Speed.
                if (wasOnFloor && FloorConstantSpeed && canApplyConstantSpeed && IsOnFloor && !motion.IsZeroApprox())
                {
                    Vector3 travelSlideUp = totalTravel.Slide(UpDirection);
                    motion = motion.Normalized() * Math.Max(0, motionSlideUp.Length() - travelSlideUp.Length());
                }
            }
            // When you move forward in a downward slope you don’t collide because you will be in the air.
            // This test ensures that constant speed is applied, only if the player is still on the ground after the snap is applied.
            else if (FloorConstantSpeed && firstSlide && OnFloorIfSnapped(wasOnFloor, velDirFacingUp))
            {
                canApplyConstantSpeed = false;
                slidingEnabled = true;
                var gt = GlobalTransform;
                gt.Origin -= motion;
                GlobalTransform = gt;

                // Slide using the intersection between the motion plane and the floor plane,
                // in order to keep the direction intact.
                Vector3 motionSlideNorm = UpDirection.Cross(motion).Cross(prevFloorNormal);
                motionSlideNorm = motionSlideNorm.Normalized();

                motion = motionSlideNorm * motionSlideUp.Length();
                collided = true;
            }

            if (!collided || motion.IsZeroApprox())
                break;

            canApplyConstantSpeed = !canApplyConstantSpeed && !slidingEnabled;
            slidingEnabled = true;
            firstSlide = false;
        }

        SnapOnFloor(wasOnFloor, velDirFacingUp);

        // Reset the gravity accumulation when touching the ground.
        if (IsOnFloor && !velDirFacingUp)
            Velocity = Velocity.Slide(UpDirection);
    }

    /// <summary>
    /// Allows to manually apply a snap to the floor regardless of the body's velocity. This function does nothing when <c>IsOnFloor</c> returns <c>true</c>.
    /// </summary>
    public void ApplyFloorSnap()
    {
        if (IsOnFloor)
            return;

        // Snap by at least collision margin to keep floor state consistent.
        float length = Math.Max(FloorSnapLength, SafeMargin);

        var parameters = new PhysicsTestMotionParameters3D()
        {
            From = GlobalTransform,
            Motion = -UpDirection * length,
            Margin = SafeMargin,
            MaxCollisions = 4,
            RecoveryAsCollision = true, // Also report collisions generated only from recovery.
            CollideSeparationRay = true
        };

        var collision = MoveAndCollideExtended(parameters, true, false);
        if (collision != null)
        {
            // Apply direction for floor only.
            var resultState = SetCollisionDirection(collision, new(true, false, false));

            if (resultState.IsOnFloor)
            {
                // Ensure that we only move the body along the up axis, because
                // move_and_collide may stray the object a bit when getting it unstuck.
                // Canceling this motion should not affect Move method, as previous
                // calls to move_and_collide already took care of freeing the body.
                if (collision.GetTravel().Length() > SafeMargin)
                    collision.SetTravel(UpDirection * UpDirection.Dot(collision.GetTravel()));
                else collision.SetTravel(Vector3.Zero);

                var from = parameters.From;
                from.Origin += collision.GetTravel();
                GlobalTransform = from;
            }
        }
    }

    private void SnapOnFloor(bool wasOnFloor, bool velDirFacingUp)
    {
        if (IsOnFloor || !wasOnFloor || velDirFacingUp)
            return;
        ApplyFloorSnap();
    }

    private bool OnFloorIfSnapped(bool wasOnFloor, bool velDirFacingUp)
    {
        if (UpDirection == Vector3.Zero || IsOnFloor || !wasOnFloor || velDirFacingUp)
            return false;
        return PerfornOnFloorIfSnappedCheck();
    }

    public bool OnFloorIfSnapped()
    {
        // Check if velocity is facing up direction
        if (UpDirection == Vector3.Zero || Velocity.Dot(UpDirection) > 0)
            return false;
        return PerfornOnFloorIfSnappedCheck();
    }

    private bool PerfornOnFloorIfSnappedCheck()
    {
        // Snap by at least collision margin to keep floor state consistent.
        float length = Math.Max(FloorSnapLength, SafeMargin);

        var parameters = new PhysicsTestMotionParameters3D()
        {
            From = GlobalTransform,
            Motion = -UpDirection * length,
            Margin = SafeMargin,
            MaxCollisions = 4,
            RecoveryAsCollision = true, // Also report collisions generated only from recovery.
            CollideSeparationRay = true
        };

        var collision = MoveAndCollideExtended(parameters, true, false);
        if (collision != null)
        {
            // Don't apply direction for any type.
            var resultState = SetCollisionDirection(collision, new(false, false, false));
            return resultState.IsOnFloor;
        }
        return false;
    }

    private CollisionState SetCollisionDirection(KinematicCollision3D collision, CollisionState? applyState = null)
    {
        applyState ??= new(true, true, true); // Default value for applyState

        var outputState = new CollisionState();

        float wallDepth = -1.0f;
        float floorDepth = -1.0f;

        bool wasOnWall = IsOnWall;
        Vector3 prevWallNormal = WallNormal;
        int wallCollisionCount = 0;
        Vector3 combinedWallNormal = Vector3.Zero;
        Vector3 tmpWallCol = Vector3.Zero; // Avoid duplicate on average calculation.

        for (int i = collision.GetCollisionCount() - 1; i >= 0; i--)
        {
            // Check if any collision is floor.
            float floorAngle = collision.GetAngle(i, UpDirection);
            if (floorAngle <= FloorMaxAngle + FloorAngleThreshold)
            {
                outputState.IsOnFloor = true;
                if (applyState.Value.IsOnFloor && collision.GetParticularDepth(i) > floorDepth)
                {
                    IsOnFloor = true;
                    FloorNormal = collision.GetNormal(i);
                    floorDepth = collision.GetParticularDepth(i);
                    SetPlatformData(collision, i);
                }
                continue;
            }

            // Check if any collision is ceiling.
            float ceilingAngle = collision.GetAngle(i, -UpDirection);
            if (ceilingAngle <= FloorMaxAngle + FloorAngleThreshold)
            {
                outputState.IsOnCeiling = true;
                if (applyState.Value.IsOnCeiling)
                {
                    PlatformCeilingVelocity = collision.GetColliderVelocity(i);
                    CeilingNormal = collision.GetNormal(i);
                    IsOnCeiling = true;
                }
                continue;
            }

            // Collision is wall by default.
            outputState.IsOnWall = true;

            if (applyState.Value.IsOnWall && collision.GetParticularDepth(i) > wallDepth)
            {
                IsOnWall = true;
                wallDepth = collision.GetParticularDepth(i);
                WallNormal = collision.GetNormal(i);

                // Don't apply wall velocity when the collider is a CharacterBody3D or AdvancedCharacterBody3D.
                if (InstanceFromId(collision.GetColliderId(i))
                    is not CharacterBody3D and not CustomCharacterBody3D)
                    SetPlatformData(collision, i);
            }

            // Collect normal for calculating average.
            if (!collision.GetNormal(i).IsEqualApprox(tmpWallCol))
            {
                tmpWallCol = collision.GetNormal(i);
                combinedWallNormal += collision.GetNormal(i);
                wallCollisionCount++;
            }
        }

        if (outputState.IsOnWall)
        {
            if (wallCollisionCount > 1 && !outputState.IsOnFloor)
            {
                // Check if wall normals cancel out to floor support.
                if (!outputState.IsOnFloor)
                {
                    combinedWallNormal = combinedWallNormal.Normalized();
                    float floorAngle = Mathf.Acos(combinedWallNormal.Dot(UpDirection));
                    if (floorAngle <= FloorMaxAngle + FloorAngleThreshold)
                    {
                        outputState.IsOnFloor = true;
                        outputState.IsOnWall = false;
                        if (applyState.Value.IsOnFloor)
                        {
                            IsOnFloor = true;
                            FloorNormal = combinedWallNormal;
                        }
                        if (applyState.Value.IsOnWall)
                        {
                            IsOnWall = wasOnWall;
                            WallNormal = prevWallNormal;
                        }
                    }
                }
            }
        }
        return outputState;
    }

    private void SetPlatformData(KinematicCollision3D collision, int collisionIndex)
    {
        PlatformRid = collision.GetColliderRid(collisionIndex);
        PlatformObjectId = collision.GetColliderId(collisionIndex);
        PlatformVelocity = collision.GetColliderVelocity(collisionIndex);
        PlatformLayer = PhysicsServer3D.BodyGetCollisionLayer(PlatformRid);
    }

    private void ClearCollisionStates()
    {
        IsOnFloor = false;
        IsOnWall = false;
        IsOnCeiling = false;
    }

    private struct CollisionState
    {
        public CollisionState(bool isOnFloor, bool isOnWall, bool isOnCeiling)
        {
            IsOnFloor = isOnFloor;
            IsOnWall = isOnWall;
            IsOnCeiling = isOnCeiling;
        }

        public bool IsOnFloor;
        public bool IsOnWall;
        public bool IsOnCeiling;
    }

    public enum PlatformOnLeaveEnum
    {
        AddVelocity,
        AddUpwardVelocity,
        DoNothing,
    }
}
