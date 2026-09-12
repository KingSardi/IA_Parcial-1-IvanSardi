using UnityEngine;

public class InterestObject : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 10f;

    private float _currentHealth;

    public float CurrentHealth => _currentHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;

        Debug.Log($"{name} recibió {damage} de daño. Vida restante: {_currentHealth}");

        if (_currentHealth <= 0f)
        {
            DestroyInterestObject();
        }
    }

    private void DestroyInterestObject()
    {
        Debug.Log($"{name} fue destruido.");

        Destroy(gameObject);
    }

}
