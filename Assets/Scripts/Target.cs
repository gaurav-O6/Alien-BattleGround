using UnityEngine;

public class Target : MonoBehaviour
{
    public float RespawnTime = 1f;
    public ScoreManager ScoreManager;
    public void Hit()
    {
        Debug.Log("Target Hit!!");

        ScoreManager.AddScore();

        gameObject.SetActive(false);
        Invoke(nameof(Respawn), RespawnTime);
    }
    public void Respawn()
    {
        gameObject.SetActive(true);
    }
}
        