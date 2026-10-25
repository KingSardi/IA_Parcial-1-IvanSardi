
using UnityEngine;

public class WolfCapture : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FSMAgent _hunter;
    [SerializeField] private SafeZoneDetector _safeZoneDetector;

    [Header("Capture")]
    [SerializeField] private float _captureRadius = 1.5f;

    private void Update()
    {
        if (_hunter == null || _hunter.IsDead)
            return;

        if (_safeZoneDetector != null && _safeZoneDetector.IsInsideSafeZone(_hunter.transform.position))
            return;

        Vector3 direction = _hunter.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > _captureRadius * _captureRadius)
            return;

        _hunter.Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _captureRadius);
    }
}
