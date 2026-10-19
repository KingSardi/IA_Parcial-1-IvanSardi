
using UnityEngine;

public class WolfHowlEmitter : MonoBehaviour
{
    [Header("Howl Settings")]
    [SerializeField] private float _howlRadius = 15f;
    [SerializeField] private LayerMask _wolfLayer;

    private readonly Collider[] _nearbyWolves = new Collider[16];

    public void Howl(Vector3 hunterPosition)
    {
        int detectedCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            _howlRadius,
            _nearbyWolves,
            _wolfLayer
        );

        for (int i = 0; i < detectedCount; i++)
        {
            Collider detected = _nearbyWolves[i];

            if (detected.gameObject == gameObject)
                continue;

            if (detected.TryGetComponent(
                out WolfAlertReceiver receiver))
            {
                receiver.ReceiveAlert(hunterPosition);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _howlRadius);
    }
}
