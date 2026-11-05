
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int Health = 100;

    [Header("Score")]
    public ScoreManager ScoreManager;

    [Header("Death Animation")]
    public Animator AlienAnimator;
    public float DeathAnimationDuration = 3f;

    [Header("Dissolve Effect")]
    public EnemyDissolve EnemyDissolve;
    public float DissolveStartDelay = 1.5f;

    [Header("Spawner")]
    public EnemySpawner Spawner;

    private bool isDead = false;
    public bool IsDead => isDead;

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
        {
            ScoreManager.AddScore();
        }
        else
        {
            Debug.LogWarning(
                "EnemyHealth: ScoreManager is not assigned.", this);
        }

        if (AlienAnimator != null)
        {
            AlienAnimator.SetTrigger("Die");
        }
        else
        {
            Debug.LogError(
                "EnemyHealth: AlienAnimator is not assigned.", this);
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(DissolveStartDelay);

        if (EnemyDissolve != null)
        {
            EnemyDissolve.StartDissolve();
        }
        else
        {
            Debug.LogWarning(
                "EnemyHealth: EnemyDissolve is not assigned.", this);
        }

        float remainingTime =
            Mathf.Max(0f, DeathAnimationDuration - DissolveStartDelay);

        yield return new WaitForSeconds(remainingTime);

        if (Spawner != null)
        {
            Spawner.EnemyDied();
        }

        gameObject.SetActive(false);
    }
}
