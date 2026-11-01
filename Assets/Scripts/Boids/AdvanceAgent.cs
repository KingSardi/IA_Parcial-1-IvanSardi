
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AdvanceAgent : Agent
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth = 1f;
    [SerializeField] private float _respawnDelay = 5f;
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxSteering = 3f;
    [SerializeField] private float _slowingDistance = 3f;
    [SerializeField] private float _minDistance = 0.1f;

    [Header("FlockingRadius")]
    [SerializeField] private float _separationRadius = 3f;
    [SerializeField] private float _cohesionRadius = 10f;
    [SerializeField] private float _alignmentRadius = 6f;
    [SerializeField, Range(0f, 1f)] private float separationWeight = 0.6f;
    [SerializeField, Range(0f, 1f)] private float cohesionWeight = 0.4f;
    [SerializeField, Range(0f, 1f)] private float alignmentWeight = 0.8f;

    [Header("Boundary Steering")]
    [SerializeField] private float _boundaryMargin = 3f;
    [SerializeField] private float _boundaryWeight = 2f;

    [Header("HunterRadius")]
    [SerializeField] private float _hunterDetectionRadius = 10f;

    [Header("InterestRadius")]
    [SerializeField] private float _interestDetectionRadius = 8f;
    [SerializeField] private float _interestInteractionRadius = 1.5f;
    [SerializeField] private float _interactionInterval = 1f;
    [SerializeField] private float _interactionDamage = 1f;
    [SerializeField] private float _interestScanInterval = 0.25f;
    private float _interestScanTimer;

    [Header("Visual Feedback")]
    [SerializeField] private TMP_Text _behaviourText;

    [Header("References")]
    [SerializeField] private Agent _target;

    private static readonly List<Agent> allAgents = new();

    private Agent _hunter;
    private Renderer _renderer;
    private InterestObject _interestTarget;

    private float _interactionTimer;
    private float _currentHealth;
    private bool _isDead;
    private bool _isCollected;

    public bool IsDead => _isDead;
    public bool IsCollected => _isCollected;
    public string CurrentBehaviour { get; private set; } = "FLOCKING";

    public enum SteeringModes
    {
        Seek,
        Flee,
        Arrive,
        Pursuit,
        Evade,
        Flocking
    }

    public SteeringModes currentSteering;

    private void Awake()
    {
        allAgents.Add(this);

        _renderer = GetComponent<Renderer>();
        _currentHealth = _maxHealth;

        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );

        _velocity = randomDirection.normalized * _maxSpeed;
        _interestScanTimer = Random.Range(0f, _interestScanInterval);
    }

    private void OnDestroy()
    {
        allAgents.Remove(this);
    }

    private void Start()
    {
        GameObject hunterObject = GameObject.FindGameObjectWithTag("Hunter");

        if (hunterObject != null)
            _hunter = hunterObject.GetComponent<Agent>();
    }

    private void Update()
    {
        if (_isDead)
        {
            CurrentBehaviour = "MUERTO";
            UpdateBehaviourText();
            UpdateBehaviourColor();
            return;
        }

        _interestScanTimer -= Time.deltaTime;

        if (_interestScanTimer <= 0f)
        {
            DetectInterestObject();
            _interestScanTimer = _interestScanInterval;
        }

        InteractWithInterestObject();

        Vector3 steering = SteeringVector();
        Vector3 boundarySteering = CalculateBoundarySteering() * _boundaryWeight;

        _velocity += steering + boundarySteering;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.001f)
            transform.forward = _velocity.normalized;

        UpdateBehaviourText();
        UpdateBehaviourColor();
    }

    private Vector3 CalculateBoundarySteering()
    {
        if (Bounds.Instance == null)
            return Vector3.zero;

        if (Bounds.Instance.Contains(transform.position, _boundaryMargin))
            return Vector3.zero;

        Vector3 direction = Bounds.Instance.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        return CalculateSteering(direction.normalized * _maxSpeed);
    }

    private Vector3 SteeringVector()
    {
        if (_hunter != null && InRange(_hunter.transform.position, _hunterDetectionRadius))
        {
            CurrentBehaviour = "EVADIENDO";
            return Evade(_hunter);
        }

        if (_interestTarget != null)
        {
            float distance = Vector3.Distance(transform.position, _interestTarget.transform.position);

            if (distance <= _interestInteractionRadius)
            {
                CurrentBehaviour = "INTERACTUANDO";
                return CalculateSteering(Vector3.zero);
            }

            CurrentBehaviour = "LLEGANDO";
            return Arrive(_interestTarget.transform.position);
        }

        switch (currentSteering)
        {
            case SteeringModes.Seek:
                CurrentBehaviour = "SEEK";
                return _target != null ? Seek(_target.transform.position) : Vector3.zero;

            case SteeringModes.Flee:
                CurrentBehaviour = "FLEE";
                return _target != null ? Flee(_target.transform.position) : Vector3.zero;

            case SteeringModes.Arrive:
                CurrentBehaviour = "ARRIVE";
                return _target != null ? Arrive(_target.transform.position) : Vector3.zero;

            case SteeringModes.Pursuit:
                CurrentBehaviour = "PURSUIT";
                return _target != null ? Pursuit(_target) : Vector3.zero;

            case SteeringModes.Evade:
                CurrentBehaviour = "EVADIENDO";
                return _target != null ? Evade(_target) : Vector3.zero;

            case SteeringModes.Flocking:
                CurrentBehaviour = "FLOCKING";
                return Flocking();

            default:
                return Vector3.zero;
        }
    }

    private Vector3 Flocking()
    {
        return CalculateSeparation(allAgents, _separationRadius) * separationWeight
             + CalculateAlignment(allAgents, _alignmentRadius) * alignmentWeight
             + CalculateCohesion(allAgents, _cohesionRadius) * cohesionWeight;
    }

    private Vector3 CalculateSeparation(IEnumerable<Agent> list, float radius)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (Agent item in list)
        {
            if (item == null || item == this)
                continue;

            if (InRange(item.transform.position, radius))
            {
                desired += item.transform.position - transform.position;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        desired /= count;

        return CalculateSteering(-desired.normalized * _maxSpeed);
    }

    private Vector3 CalculateAlignment(IEnumerable<Agent> list, float radius)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (Agent item in list)
        {
            if (item == null || item == this)
                continue;

            if (InRange(item.transform.position, radius))
            {
                desired += item.Velocity;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        desired /= count;

        return CalculateSteering(desired.normalized * _maxSpeed);
    }

    private Vector3 CalculateCohesion(IEnumerable<Agent> list, float radius)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (Agent item in list)
        {
            if (item == null || item == this)
                continue;

            if (InRange(item.transform.position, radius))
            {
                desired += item.transform.position;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        desired /= count;

        return Seek(desired);
    }

    private bool InRange(Vector3 position, float radius)
    {
        return (position - transform.position).sqrMagnitude <= radius * radius;
    }

    private Vector3 CalculateSteering(Vector3 desired)
    {
        Vector3 steering = desired - _velocity;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }

    private Vector3 DesiredVector(Vector3 target)
    {
        Vector3 desired = target - transform.position;
        desired.y = 0f;

        return desired.normalized * _maxSpeed;
    }

    private Vector3 Seek(Vector3 target)
    {
        return CalculateSteering(DesiredVector(target));
    }

    private Vector3 Flee(Vector3 target)
    {
        return CalculateSteering(-DesiredVector(target));
    }

    private Vector3 Arrive(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance < _minDistance)
            return CalculateSteering(Vector3.zero);

        float targetSpeed = _maxSpeed * (distance / Mathf.Max(_slowingDistance, 0.001f));
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        Vector3 desired = direction.normalized * desiredSpeed;

        return CalculateSteering(desired);
    }

    private Vector3 CalculateFuture(Agent target)
    {
        Vector3 direction = target.transform.position - transform.position;
        float distance = direction.magnitude;

        float prediction = distance / Mathf.Max(_maxSpeed + target.Velocity.magnitude, 0.001f);

        return target.transform.position + target.Velocity * prediction;
    }

    private Vector3 Pursuit(Agent target)
    {
        return Seek(CalculateFuture(target));
    }

    private Vector3 Evade(Agent target)
    {
        return Flee(CalculateFuture(target));
    }

    private void DetectInterestObject()
    {
        InterestObject[] interestObjects = FindObjectsByType<InterestObject>(FindObjectsSortMode.None);

        InterestObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (InterestObject interest in interestObjects)
        {
            if (interest == null)
                continue;

            float distance = Vector3.Distance(transform.position, interest.transform.position);

            if (distance <= _interestDetectionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closest = interest;
            }
        }

        _interestTarget = closest;
    }

    private void InteractWithInterestObject()
    {
        if (_interestTarget == null)
        {
            _interactionTimer = 0f;
            return;
        }

        float distance = Vector3.Distance(transform.position, _interestTarget.transform.position);

        if (distance > _interestInteractionRadius)
        {
            _interactionTimer = 0f;
            return;
        }

        _interactionTimer += Time.deltaTime;

        if (_interactionTimer >= _interactionInterval)
        {
            _interestTarget.TakeDamage(_interactionDamage);
            _interactionTimer = 0f;
        }
    }

    public void TakeDamage(float damage)
    {
        if (_isDead)
            return;

        _currentHealth -= damage;

        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _currentHealth = 0f;
        _isDead = true;
        _velocity = Vector3.zero;
    }

    public void Collect()
    {
        if (!_isDead || _isCollected)
            return;

        _isCollected = true;
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        if (_renderer != null)
            _renderer.enabled = false;

        if (_behaviourText != null)
            _behaviourText.gameObject.SetActive(false);

        yield return new WaitForSeconds(_respawnDelay);

        Respawn();
    }

    private void Respawn()
    {
        transform.position = GetRandomPosition();

        _currentHealth = _maxHealth;
        _isDead = false;
        _isCollected = false;
        _interestTarget = null;
        _interactionTimer = 0f;

        CurrentBehaviour = "FLOCKING";

        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );

        _velocity = randomDirection.normalized * _maxSpeed;

        if (_renderer != null)
            _renderer.enabled = true;

        if (_behaviourText != null)
            _behaviourText.gameObject.SetActive(true);

        UpdateBehaviourText();
        UpdateBehaviourColor();
    }


    private Vector3 GetRandomPosition()
    {
        if (Bounds.Instance == null)
            return transform.position;

        return Bounds.Instance.GetRandomPosition(transform.position.y);
    }


    private void UpdateBehaviourText()
    {
        if (_behaviourText == null)
            return;

        _behaviourText.text = CurrentBehaviour;
    }

    private void UpdateBehaviourColor()
    {
        if (_renderer == null)
            return;

        switch (CurrentBehaviour)
        {
            case "FLOCKING":
                _renderer.material.color = Color.green;
                break;

            case "EVADIENDO":
                _renderer.material.color = Color.red;
                break;

            case "LLEGANDO":
                _renderer.material.color = Color.yellow;
                break;

            case "INTERACTUANDO":
                _renderer.material.color = Color.blue;
                break;

            case "MUERTO":
                _renderer.material.color = Color.black;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Separation
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _separationRadius);

        // Cohesion
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _cohesionRadius);

        // Alignment
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _alignmentRadius);

        //Hunter
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _hunterDetectionRadius);

        //Interest
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _interestDetectionRadius);
    }
}