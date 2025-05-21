using UnityEngine;

[System.Serializable]
public class ScoreData
{
    public string imageName;
    public int highScore;
    public int totalQuestions;

    public ScoreData(string name, int score, int total)
    {
        imageName = name;
        highScore = score;
        totalQuestions = total;
    }
}