using UnityEngine;

/// <summary>
/// Agent which moves through a character controller.
/// </summary>
public class CharacterAgent : TransformAgent
{
    /// <summary>
    /// This agent's character controller.
    /// </summary>
    private CharacterController _characterController;

    /// <summary>
    /// Used to manually apply gravity.
    /// </summary>
    private float _velocityY;

    protected override void Start()
    {
        base.Start();
            
        // Get the character controller.
        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
        {
            _characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    /// <summary>
    /// Character controller movement.
    /// </summary>
    public override void Move()
    {
        if (_characterController == null)
        {
            return;
        }
        
        // Get the agent's position prior to any movement.
        Vector3 lastPosition = transform.position;
            
        // If the agent should not be moving, still call to move so gravity is applied.
        if (MovingToTarget)
        {
            // Calculate how fast we can move this frame.
            CalculateMoveVelocity();
                
            Vector3 position = transform.position;
            _characterController.Move(Vector3.MoveTowards(position, MoveTarget, MoveVelocity * Time.deltaTime) - position);
        }

        // Reset gravity if grounded.
        if (_characterController.isGrounded)
        {
            _velocityY = 0;
        }
        
        // Apply gravity.
        _velocityY += Physics.gravity.y * Time.deltaTime;
        _characterController.Move(new Vector3(0, _velocityY, 0));

        DidMove = transform.position != lastPosition;
            
        if (DidMove)
        {
            AddMessage($"Moved towards {MoveTarget}.");
        }
    }
}