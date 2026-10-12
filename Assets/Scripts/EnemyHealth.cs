using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int Health = 100;
    public ScoreManager ScoreManager;
    public void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            Health = 0;
            Die();
            return;
        }
        Debug.Log("Enemy Health: " + Health);
    }

    void Die()
    {
        Debug.Log("Enemy has died.");
        ScoreManager.AddScore();
        gameObject.SetActive(false);

    }
    void Start()
    {
    }

    void Update()
    {
        
    }
}
