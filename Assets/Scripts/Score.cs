using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour {

    GameBoard game;
    public Text score;

	// Use this for initialization
	void Start () {
        game = FindObjectOfType<GameBoard>();
        game.ScoreUpdated += UpdateScore;
	}

    void UpdateScore() {
        score.text = game.score.ToString();
    }
}
