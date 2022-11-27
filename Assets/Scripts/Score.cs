using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Score
{
    private int score = 0;
    
    public int getScore()
    {
        return score;
    }

    public void addScore(int points)
    {
        score += points;
    }
}
