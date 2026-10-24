using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int Health = 100;

    [Header("Score")]
    public ScoreManager ScoreManager;

    [Header("Death Animation")]
    public Animator AlienAnimator;
    public float DeathAnimationDuration = 3f;

    [Header("Disintegration Effect")]
    public VisualEffect DisintegrationEffectPrefab;
    public float DisintegrationEffectLifetime = 3f;

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
        {
            ScoreManager.AddScore();
        }
        else
        {
            Debug.LogWarning(
                "EnemyHealth: ScoreManager is not assigned.", this);
        }

        // Play the existing death animation.
        if (AlienAnimator != null)
        {
            AlienAnimator.SetTrigger("Die");
        }
        else
        {
            Debug.LogError(
                "EnemyHealth: AlienAnimator is not assigned.", this);
        }

        // Spawn the new Visual Effect Graph.
        if (DisintegrationEffectPrefab != null)
        {
            VisualEffect effect = Instantiate(
                DisintegrationEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            effect.Play();

            Destroy(effect.gameObject, DisintegrationEffectLifetime);
        }
        else
        {
            Debug.LogWarning(
                "EnemyHealth: Disintegration Effect is not assigned.",
                this);
        }

        StartCoroutine(DeactivateAfterDeath());
    }

    private IEnumerator DeactivateAfterDeath()
    {
        yield return new WaitForSeconds(DeathAnimationDuration);
        gameObject.SetActive(false);
    }
}
