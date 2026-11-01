
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

    [Header("Vision Visualization")]
    [SerializeField] private bool _showVision = true;
    [SerializeField] private Material _visionMaterial;
    [SerializeField, Range(3, 120)]
    private int _visionSegments = 40;
    [SerializeField] private float _visionHeight = 0.05f;

    public Vector3 LastKnownHunterPosition { get; private set; }
    public bool HasSeenHunter { get; private set; }

    private GameObject _visionObject;
    private Mesh _visionMesh;

    private void Start()
    {
        CreateVisionMesh();
    }

    private void LateUpdate()
    {
        if (_visionObject == null)
            return;

        _visionObject.SetActive(_showVision);

        if (_showVision)
            GenerateVisionMesh();
    }

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

    private void CreateVisionMesh()
    {
        _visionObject = new GameObject("VisionCone");
        _visionObject.transform.SetParent(transform, false);
        _visionObject.transform.localPosition =
            new Vector3(0f, _visionHeight, 0f);

        MeshFilter meshFilter = _visionObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = _visionObject.AddComponent<MeshRenderer>();

        _visionMesh = new Mesh();
        _visionMesh.name = "Wolf Vision Cone";

        meshFilter.mesh = _visionMesh;

        if (_visionMaterial != null)
            meshRenderer.material = _visionMaterial;

        GenerateVisionMesh();
    }


    private void GenerateVisionMesh()
    {
        int segments = Mathf.Max(3, _visionSegments);

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle =
                -_viewAngle * 0.5f +
                (_viewAngle / segments) * i;

            float radians = angle * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Sin(radians),
                0f,
                Mathf.Cos(radians)
            );

            float distance = _viewDistance;

            Vector3 worldDirection =
                transform.TransformDirection(direction);

            if (Physics.Raycast(
                transform.position,
                worldDirection,
                out RaycastHit hit,
                _viewDistance,
                _obstacleLayer))
            {
                distance = hit.distance;
            }

            vertices[i + 1] = direction * distance;
        }

        for (int i = 0; i < segments; i++)
        {
            int index = i * 3;

            triangles[index] = 0;
            triangles[index + 1] = i + 2;
            triangles[index + 2] = i + 1;
        }

        _visionMesh.vertices = vertices;
        _visionMesh.triangles = triangles;
        _visionMesh.RecalculateNormals();
        _visionMesh.RecalculateBounds();
    }


    private void OnDestroy()
    {
        if (_visionMesh != null)
            Destroy(_visionMesh);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _viewDistance);
    }
}
