using UnityEngine;
using UnityEngine.AI;

public class AgentControl : MonoBehaviour
{
    public Transform home;
    NavMeshAgent agent;

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        agent.SetDestination(home.position);
    }

}
