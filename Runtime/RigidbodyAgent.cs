using UnityEngine;

/// <summary>
/// Agent which moves through a rigidbody.
/// </summary>
public class RigidbodyAgent : Agent
{
    /// <summary>
    /// This agent's rigidbody.
    /// </summary>
    private Rigidbody _rigidbody;

    protected override void Start()
    {
        // Get the rigidbody.
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        // Since rotation is all done with the root visuals transform, freeze rigidbody rotation.
        if (_rigidbody != null)
        {
            _rigidbody.freezeRotation = true;
        }
    }
        
    public override void Move()
    {
        if (_rigidbody == null)
        {
            return;
        }
        
        Vector3 lastPosition = transform.position;
        
        if (MovingToTarget)
        {
            // Calculate how fast we can move this frame.
            CalculateMoveVelocity(Time.fixedDeltaTime);
                
            Vector3 position = transform.position;
            _rigidbody.AddForce(Vector3.MoveTowards(position, MoveTarget, MoveVelocity * Time.fixedDeltaTime) - position, ForceMode.VelocityChange);
        }
        else
        {
            MoveVelocity = 0;
        }
            
        DidMove = transform.position != lastPosition;
            
        if (DidMove)
        {
            AddMessage($"Moved towards {MoveTarget}.");
        }
    }
}