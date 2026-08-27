using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves Mario left and right, and carries him along when the floor under his feet
/// is a moving platform.
///
/// It OWNS its speed: no other class writes into the field from outside. A temporary
/// boost is asked for through SetSpeedMultiplier, so the lightning effect never has to
/// know what the normal speed is, or remember to put it back.
/// </summary>
public class PlayerMovement : MonoBehaviour, IFacing
{
    [Tooltip("Normal walking speed, before any power up")]
    [SerializeField] private float speed = 5f;

    private float speedMultiplier = 1f;
    private float facingDirection = 1f;
    private float direction;
    private Rigidbody2D rigid;
    private IPlatformProvider platformProvider;

    /// <summary>Speed actually used right now: normal speed times the active multiplier.</summary>
    public float CurrentSpeed
    {
        get { return speed * speedMultiplier; }
    }

    /// <summary>Which way Mario looks right now. Weapons aim by this, not by the scale.</summary>
    public float FacingDirection
    {
        get { return facingDirection; }
    }

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        platformProvider = GetComponent<IPlatformProvider>();
    }

    /// <summary>
    /// Used by timed effects. 1.5 means "+50% while the effect lasts", 1 means normal.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier > 0f ? multiplier : 1f;
    }

    private void FixedUpdate()
    {
        ReadInput();
        ApplyMovement();
    }

    private void ReadInput()
    {
        direction = 0f;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            direction = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            direction = 1f;
    }

    private void ApplyMovement()
    {
        if (rigid == null)
            return;

        float platformSpeedX = GetPlatformSpeedX();

        if (direction != 0f)
        {
            // Walking ON a platform means platform speed PLUS his own steps.
            rigid.linearVelocity = new Vector2(platformSpeedX + direction * CurrentSpeed, rigid.linearVelocity.y);

            facingDirection = direction > 0f ? 1f : -1f;
            transform.localScale = new Vector3(facingDirection, 1, 1);
            return;
        }

        // Standing still on a platform: match its speed exactly, so he keeps the spot
        // he is standing on. The velocity is REWRITTEN every step, which is also why
        // friction can no longer drag him a second time.
        if (platformSpeedX != 0f)
            rigid.linearVelocity = new Vector2(platformSpeedX, rigid.linearVelocity.y);
    }

    /// <summary>
    /// Horizontal speed of the floor under our feet, or 0 on normal ground.
    /// The platform is only asked HOW FAR it moved - it never pushes us itself,
    /// so a lift or a conveyor needs no change here.
    /// </summary>
    private float GetPlatformSpeedX()
    {
        if (platformProvider == null)
            return 0f;

        IRideablePlatform platform = platformProvider.CurrentPlatform;
        if (platform == null)
            return 0f;

        return platform.Delta.x / Time.fixedDeltaTime;
    }
}
