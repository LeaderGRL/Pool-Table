using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Score
{
    private int score = 0;
    
    private int getScore()
    {
        return score;
    }

    private void addScore(int points)
    {
        score += points;
    }
}
