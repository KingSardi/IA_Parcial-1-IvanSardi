
using UnityEngine;

public class WolfVisionSensor : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float _viewDistance = 12f;
    [SerializeField, Range(0f, 360f)]
    private float _viewAngle = 120f;

    [Header("References")]
    [SerializeField] private Transform _hunter;
    [SerializeField] private LayerMask _obstacleLayer;
    public Vector3 LastKnownHunterPosition { get; private set; }

    public bool HasSeenHunter { get; private set; }

    public bool CanSeeHunter()
    {
        if (_hunter == null)
            return false;

        Vector3 direction = _hunter.position - transform.position;

        if (direction.sqrMagnitude > _viewDistance * _viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, direction);

        if (angle > _viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(
            transform.position,
            direction.normalized,
            direction.magnitude,
            _obstacleLayer))
        {
            return false;
        }

        LastKnownHunterPosition = _hunter.position;
        HasSeenHunter = true;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _viewDistance);
    }
}
