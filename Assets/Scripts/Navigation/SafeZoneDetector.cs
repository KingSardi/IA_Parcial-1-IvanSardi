
using UnityEngine;

public class SafeZoneDetector : MonoBehaviour
{
    [Header("Safe Zones")]
    [SerializeField] private Collider[] _safeZones;

    public Collider[] SafeZones => _safeZones;

    public bool IsInsideSafeZone(Vector3 position)
    {
        foreach (Collider safeZone in _safeZones)
        {
            if (safeZone == null || !safeZone.enabled)
                continue;

            Vector3 closestPoint = safeZone.ClosestPoint(position);

            if ((closestPoint - position).sqrMagnitude <= 0.0001f)
                return true;
        }

        return false;
    }
}
