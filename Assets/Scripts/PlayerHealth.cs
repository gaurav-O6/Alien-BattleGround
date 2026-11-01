using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int Health = 100;
    private bool IsDead = false;

    public PlayerMovement PlayerMovement;

    private Animator PlayerAnimator;
    private CharacterController Controller;

    private void Awake()
    {
        PlayerAnimator = GetComponentInChildren<Animator>();
        Controller = GetComponent<CharacterController>();
    }

    public bool IsPlayerDead()
    {
        return IsDead;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        Health -= damage;

        if (Health <= 0)
        {
            Health = 0;
            Die();
            return;
        }

        Debug.Log("Player Health: " + Health);
    }

    private void Die()
    {
        IsDead = true;

        Debug.Log("Player has died.");

        StartCoroutine(PlayDeathAnimationAfterLanding());
    }

    private IEnumerator PlayDeathAnimationAfterLanding()
    {
        if (Controller != null)
        {
            while (!Controller.isGrounded)
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning(
                "PlayerHealth: CharacterController not found. Playing death animation immediately.",
                this
            );
        }

        if (PlayerAnimator != null)
        {
            PlayerAnimator.SetFloat("Speed", 0f);
            PlayerAnimator.SetFloat("Horizontal", 0f);
            PlayerAnimator.SetFloat("Vertical", 0f);

            PlayerAnimator.ResetTrigger("Die");
            PlayerAnimator.SetTrigger("Die");
        }
        else
        {
            Debug.LogError(
                "PlayerHealth: No Animator found in the player hierarchy!",
                this
            );
        }
    }
}