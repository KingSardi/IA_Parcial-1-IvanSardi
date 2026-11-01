
using UnityEngine;

public class HunterWolfSensor : MonoBehaviour
{
    [Header("Perception")]
    [SerializeField] private float _viewDistance = 12f;
    [SerializeField] private LayerMask _wolfLayer;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _eyeHeight = 0.5f;

    private readonly Collider[] _detectedWolves = new Collider[16];

    public Transform ClosestVisibleWolf { get; private set; }
    public bool CanSeeWolf => ClosestVisibleWolf != null;

    public void Scan()
    {
        ClosestVisibleWolf = null;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            _viewDistance,
            _detectedWolves,
            _wolfLayer
        );

        float closestDistance = float.MaxValue;
        Vector3 origin = transform.position + Vector3.up * _eyeHeight;

        for (int i = 0; i < count; i++)
        {
            Collider wolfCollider = _detectedWolves[i];

            if (wolfCollider == null)
                continue;

            WolfBrain wolf = wolfCollider.GetComponentInParent<WolfBrain>();

            if (wolf == null)
                continue;

            Vector3 targetPosition = wolf.transform.position + Vector3.up * _eyeHeight;
            Vector3 direction = targetPosition - origin;
            float sqrDistance = direction.sqrMagnitude;

            if (sqrDistance >= closestDistance)
                continue;

            float distance = Mathf.Sqrt(sqrDistance);

            if (Physics.Raycast(origin, direction.normalized, distance, _obstacleLayer))
                continue;

            closestDistance = sqrDistance;
            ClosestVisibleWolf = wolf.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _viewDistance);
    }
}
