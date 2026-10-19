
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int Health = 100;
    public ScoreManager ScoreManager;
    public Animator AlienAnimator;
    public float DeathAnimationDuration = 3f;

    private bool isDead = false;

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        Health -= damage;

        if (Health <= 0)
        {
            Health = 0;
            Die();
            return;
        }

        Debug.Log("Enemy Health: " + Health);
    }

    private void Die()
    {
        isDead = true;

        Debug.Log("Enemy has died.");

        if (ScoreManager != null)
            ScoreManager.AddScore();
        else
            Debug.LogWarning("EnemyHealth: ScoreManager is not assigned.");

        if (AlienAnimator != null)
            AlienAnimator.SetTrigger("Die");
        else
            Debug.LogError("EnemyHealth: AlienAnimator is not assigned.");

        StartCoroutine(DeactivateAfterDeath());
    }

    private IEnumerator DeactivateAfterDeath()
    {
        yield return new WaitForSeconds(DeathAnimationDuration);
        gameObject.SetActive(false);
    }
}
