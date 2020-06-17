using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class menuSystem : MonoBehaviour
{
    Animator canvasAnimator;
    TextMeshProUGUI scoreText,highScoreText,endText;

    private void Awake() {
        scoreText = GameObject.Find("Canvas/score/scoreText").GetComponent<TextMeshProUGUI>();
        highScoreText = GameObject.Find("Canvas/highScore/highScoreText").GetComponent<TextMeshProUGUI>();
        highScoreText.text = PlayerPrefs.GetInt("highScore", 0).ToString();

        endText = GameObject.Find("endText").GetComponent<TextMeshProUGUI>();
        canvasAnimator = transform.GetComponent<Animator>();
    }

    void Start()
    {

    }

    void Update()
    {
        
    }

    //assign score function, it using when last game loaded
    public void assingScore(int score) {
        scoreText.text = score.ToString();
    }
    
    //add score and check for high score
    public void addScore(int score) {
        int newScore = int.Parse(scoreText.text) + score;
        scoreText.text = newScore.ToString();
        

        if(newScore > PlayerPrefs.GetInt("highScore", 0)) {
            PlayerPrefs.SetInt("highScore", newScore);
            highScoreText.text = PlayerPrefs.GetInt("highScore", 0).ToString();
        }
    }
    
    //menu up and down
    public void menuButton() {
        canvasAnimator.SetTrigger("changeStatus");
    }

    
    public void restartGame() {
        PlayerPrefs.SetInt("gameOver", 1);  //game is ended, so create random grid
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    //show the reason and restart game
    public void restartButton() {
        endText.text = "Creating Grid";
        canvasAnimator.SetTrigger("isEnd"); //this animation call restartGame funciton when it ends.
    }

    //game ending, isBomb show reason of ending
    public void endGame(bool isBomb) {
        //show the reason on the screen
        if(isBomb) {
            endText.text = "Bomb Exploded!";
        }
        else {
            endText.text = "No More Move!";
        }
        canvasAnimator.SetTrigger("isEnd");
    }


}
