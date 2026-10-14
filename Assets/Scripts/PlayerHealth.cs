using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
   public int Health = 100;
    bool IsDead = false;

    public PlayerMovement PlayerMovement;
    public bool IsPlayerDead()
    {
        return IsDead;
    }
    public void TakeDamage(int damage)
    {
        if (IsDead)
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


    void Die()
    {
        Debug.Log("Player has died.");
        IsDead = true;

        Animator playerAnimator = GetComponentInChildren<Animator>();

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetFloat("Horizontal", 0f);
            playerAnimator.SetFloat("Vertical", 0f);
        }

        PlayerMovement.enabled = false;
    }

    void Start()
    {
       

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
