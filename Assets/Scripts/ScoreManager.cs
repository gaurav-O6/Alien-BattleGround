using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int Score = 0;
    public TMPro.TMP_Text Scoretext;

    public void AddScore()
    {
        Score++;
        Scoretext.text = "Score: " + Score;
        Debug.Log("Score: " + Score);
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
