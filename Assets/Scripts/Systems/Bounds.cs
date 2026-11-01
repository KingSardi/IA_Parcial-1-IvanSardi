
using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [Header("Bounds")]
    [SerializeField] private float _height = 30f;
    [SerializeField] private float _width = 60f;
    [SerializeField] private bool _drawGizmos = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool Contains(Vector3 position, float margin = 0f)
    {
        float halfWidth = Mathf.Max(0f, _width * 0.5f - margin);
        float halfHeight = Mathf.Max(0f, _height * 0.5f - margin);

        Vector3 localPosition = position - transform.position;

        return Mathf.Abs(localPosition.x) <= halfWidth &&
               Mathf.Abs(localPosition.z) <= halfHeight;
    }

    public Vector3 GetRandomPosition(float y, float margin = 1f)
    {
        float halfWidth = Mathf.Max(0f, _width * 0.5f - margin);
        float halfHeight = Mathf.Max(0f, _height * 0.5f - margin);

        return new Vector3(
            Random.Range(transform.position.x - halfWidth, transform.position.x + halfWidth),
            y,
            Random.Range(transform.position.z - halfHeight, transform.position.z + halfHeight)
        );
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        Vector3 newPosition = position;

        float minX = transform.position.x - _width * 0.5f;
        float maxX = transform.position.x + _width * 0.5f;
        float minZ = transform.position.z - _height * 0.5f;
        float maxZ = transform.position.z + _height * 0.5f;

        if (position.x > maxX)
            newPosition.x = minX;
        else if (position.x < minX)
            newPosition.x = maxX;

        if (position.z > maxZ)
            newPosition.z = minZ;
        else if (position.z < minZ)
            newPosition.z = maxZ;

        return newPosition;
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(_width, 0f, _height));
    }
}
