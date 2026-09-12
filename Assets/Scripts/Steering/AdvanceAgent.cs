using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class AdvanceAgent : Agent
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth = 10f;
    [SerializeField] private float _respawnDelay = 5f;
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxSteering = 3f;
    [SerializeField] private float _slowingDistance = 3f;
    [SerializeField] private float _minDistance = 0.1f;
    private float _interactionTimer;
    private float _currentHealth;
    private bool _isDead;
    public bool IsDead => _isDead;

    private static List<Agent> allAgents = new List<Agent>();
    
    [Header("FlockingRadius")]
    [SerializeField] private float _separationRadius = 2f;
    [SerializeField] private float _cohesionRadius = 2f;
    [SerializeField] private float _alignmentRadius = 2f;

    [Header("HunterRadius")]
    [SerializeField] private float _hunterDetectionRadius = 8f;

    [Header("InterestRadius")]
    [SerializeField] private float _interestDetectionRadius = 9f;
    [SerializeField] private float _interestInteractionRadius = 1.5f;
    [SerializeField] private float _interactionInterval = 1f;
    [SerializeField] private float _interactionDamage = 1f;

    [SerializeField, Range(0f, 1f)] private float separationWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float cohesionWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float alignmentWeight = 1f;


    [Header("References")]
    [SerializeField] private Agent _target;
    private Agent _hunter;
    private Renderer _renderer;
    private InterestObject _interestTarget;

    public enum SteeringModes { Seek, Flee, Arrive, Pursuit, Evade, Flocking }
    public SteeringModes currentSteering;

    private void Awake()
    {
        allAgents.Add(this);

        _renderer = GetComponent<Renderer>();

        _currentHealth = _maxHealth;

        Vector3 randomDirection = new Vector3(Random.Range(-1, 1), 0f, Random.Range(-1, 1));
        _velocity += randomDirection.normalized * _maxSpeed;
    }

    private void Start()
    {
        GameObject hunterObject = GameObject.FindGameObjectWithTag("Hunter");

        if (hunterObject != null)
        {
            _hunter = hunterObject.GetComponent<Agent>();
        }
    }

    private void Update()
    {
        if (_isDead)
            return;

        DetectInterestObject();
        InteractWithInterestObject();

        _velocity += SteeringVector();
        transform.position += _velocity * Time.deltaTime;

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;

        transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }    

    private Vector3 SteeringVector()
    {
        if (_hunter != null &&
    InRange(_hunter.transform.position, _hunterDetectionRadius))
        {
            return Evade(_hunter);
        }
        
        if (_interestTarget != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                _interestTarget.transform.position
            );

            if (distance <= _interestInteractionRadius)
            {
                return CalculateSteering(Vector3.zero);
            }

            return Arrive(_interestTarget.transform.position);
        }

        switch (currentSteering)
        {
            case SteeringModes.Seek:
                return Seek(_target.transform.position);
            case SteeringModes.Flee:
                return Flee(_target.transform.position);
            case SteeringModes.Arrive:
                return Arrive(_target.transform.position);
            case SteeringModes.Pursuit:
                return Pursuit(_target);
            case SteeringModes.Evade:
                return Evade(_target);
            case SteeringModes.Flocking:
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
        Vector3 dessired = default;
        int count = 0;

        foreach (var item in list)
        {
            if (item == this) continue;

            if (InRange(item.transform.position, radius))
            {
                dessired += (item.transform.position - transform.position);
                count++;
            }
        }

        if(count == 0) return Vector3.zero;
        dessired /= count;

        return CalculateSteering(-dessired.normalized * _maxSpeed);
    }

    private Vector3 CalculateAlignment(IEnumerable<Agent> list, float radius)
    {
        Vector3 desired = default;
        int count = 0;

        foreach (var item in list)
        {
            if (item == this) continue;

            if (InRange(item.transform.position, radius))
            {
                desired += item.Velocity;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return CalculateSteering(desired.normalized * _maxSpeed);
    }

    private Vector3 CalculateCohesion(IEnumerable<Agent> list, float radius)
    {
        Vector3 desired = default;
        int count = 0;

        foreach (var item in list)
        {
            if (item == this) continue;

            if (InRange(item.transform.position, radius))
            {
                desired += item.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return Seek(desired);
    }

    private bool InRange(Vector3 pos, float radius) => (pos - transform.position).sqrMagnitude <= radius * radius;
     

    private Vector3 CalculateSteering(Vector3 desired)
    {
        Vector3 steering = desired - _velocity;

        steering = Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);

        return steering;
    }

    private Vector3 DesiredVector(Vector3 target)
    {
        Vector3 desired = (target - transform.position).normalized;
        desired *= _maxSpeed;

        return desired;
    }

    private Vector3 Seek(Vector3 target)
    {
        var desired = DesiredVector(target);

        return CalculateSteering(desired);
    }

    private Vector3 Flee(Vector3 target)
    {
        var desired = DesiredVector(target);

        return CalculateSteering(-desired);
    }

    private Vector3 Arrive(Vector3 target)
    {
        Vector3 direction = target - transform.position;

        float distance = direction.magnitude;

        if (distance < _minDistance)
            return Vector3.zero;

        float targetSpeed = _maxSpeed * (distance / _slowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        Vector3 desired = direction.normalized * desiredSpeed;
        Vector3 steering = CalculateSteering(desired);

        return CalculateSteering(desired);
    }

    private Vector3 CalculateFuture(Agent target)
    {
        Vector3 direccion = target.transform.position - transform.position;

        float distance = direccion.magnitude;
        var prediction = distance / (_maxSpeed + target.Velocity.magnitude);

        Vector3 futurePosition = target.transform.position + target.Velocity * prediction;

        return futurePosition;
    }

    private Vector3 Pursuit(Agent target)
    {
        var futurePosition = CalculateFuture(target);

        return Seek(futurePosition);
    }

    private Vector3 Evade(Agent target)
    {
        var futurePosition = CalculateFuture(target);

        return Flee(futurePosition);
    }

    private void DetectInterestObject()
    {
        InterestObject[] interestObjects = FindObjectsByType<InterestObject>(
            FindObjectsSortMode.None
        );

        InterestObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (InterestObject interest in interestObjects)
        {
            float distance = Vector3.Distance(
                transform.position,
                interest.transform.position
            );

            if (distance <= _interestDetectionRadius &&
                distance < closestDistance)
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

        float distance = Vector3.Distance(
            transform.position,
            _interestTarget.transform.position
        );

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

        Debug.Log($"{name} recibió {damage} de daño. Vida: {_currentHealth}");

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        _currentHealth = 0f;
        _isDead = true;

        _velocity = Vector3.zero;

        Debug.Log($"{name} murió y quedó inactivo.");
    }

    public void Collect()
    {
        if (!_isDead)
            return;

        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        _renderer.enabled = false;

        yield return new WaitForSeconds(_respawnDelay);

        Respawn();
    }

    private void Respawn()
    {
        transform.position = GetRandomPosition();

        _currentHealth = _maxHealth;
        _isDead = false;

        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        );

        _velocity = randomDirection.normalized * _maxSpeed;

        _renderer.enabled = true;

        Debug.Log($"{name} reapareció.");
    }

    private Vector3 GetRandomPosition()
    {
        float randomX = Random.Range(-28f, 28f);
        float randomZ = Random.Range(-13f, 13f);

        return new Vector3(randomX, transform.position.y, randomZ);
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