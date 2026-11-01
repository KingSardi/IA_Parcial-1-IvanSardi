
using System.Collections;
using UnityEngine;

public class WolfHowlEmitter : MonoBehaviour
{
    [Header("Howl Settings")]
    [SerializeField] private float _howlRadius = 15f;
    [SerializeField] private LayerMask _wolfLayer;

    [Header("Howl Visual")]
    [SerializeField] private bool _showHowlWave = true;
    [SerializeField] private Material _howlMaterial;
    [SerializeField] private float _waveDuration = 0.75f;
    [SerializeField] private float _waveHeight = 0.12f;
    [SerializeField] private float _waveWidth = 0.12f;
    [SerializeField, Range(16, 128)]
    private int _waveSegments = 64;

    private readonly Collider[] _nearbyWolves = new Collider[16];

    private LineRenderer _waveRenderer;
    private Coroutine _waveCoroutine;

    public void Howl(Vector3 hunterPosition)
    {
        // Comunicación original entre lobos
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

        // Nuevo: efecto visual del aullido
        if (_showHowlWave)
            PlayHowlWave();
    }

    private void PlayHowlWave()
    {
        if (_waveRenderer == null)
            CreateWaveRenderer();

        if (_waveCoroutine != null)
            StopCoroutine(_waveCoroutine);

        _waveCoroutine = StartCoroutine(AnimateWave());
    }

    private void CreateWaveRenderer()
    {
        GameObject waveObject = new GameObject("HowlWave");
        waveObject.transform.SetParent(transform, false);

        _waveRenderer = waveObject.AddComponent<LineRenderer>();

        _waveRenderer.useWorldSpace = true;
        _waveRenderer.loop = true;
        _waveRenderer.positionCount = _waveSegments;
        _waveRenderer.startWidth = _waveWidth;
        _waveRenderer.endWidth = _waveWidth;

        _waveRenderer.material = _howlMaterial;

        _waveRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        _waveRenderer.receiveShadows = false;
        _waveRenderer.enabled = false;
    }

    private IEnumerator AnimateWave()
    {
        _waveRenderer.enabled = true;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _waveDuration);

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float radius = Mathf.Lerp(0f, _howlRadius, progress);
            float alpha = 1f - progress;

            Color fadeColor = new Color(1f, 1f, 1f, alpha);

            _waveRenderer.startColor = fadeColor;
            _waveRenderer.endColor = fadeColor;

            Vector3 center = transform.position;
            center.y = _waveHeight;

            for (int i = 0; i < _waveSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / _waveSegments;

                Vector3 point = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                _waveRenderer.SetPosition(i, point);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        _waveRenderer.enabled = false;
        _waveCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _howlRadius);
    }
}
