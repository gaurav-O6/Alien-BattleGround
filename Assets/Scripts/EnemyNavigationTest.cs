using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigationTest : MonoBehaviour
{
    private NavMeshAgent agent;

    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Vector3 destination = new Vector3(5f, 0f, -3f);
        agent.SetDestination(destination);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
