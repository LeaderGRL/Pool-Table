using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Score score;
    public string ballType;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string getRandomBallType()
    {
        string[] ballsType = { "striped", "filled" };
        return ballsType[Random.Range(0, ballType.Length)];
    }
}
