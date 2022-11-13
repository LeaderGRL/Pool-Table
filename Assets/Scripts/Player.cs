using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Score score;
    private string[] ballType = { "striped", "filled" };
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string getBallType()
    {
        return ballType[Random.Range(0, ballType.Length)];
    }
}
