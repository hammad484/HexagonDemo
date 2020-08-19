using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class UI_Manager : CellManager {
	public Text scoreText;
	public GameObject gameOverScreen;
	public bool tick;
	private GridUpdater GridManagerObject;
	private int colorCount;
	private int blownHexagons;
	private int bombCount;

	public static UI_Manager instance;



	void Awake() {
		if (instance == null)
			instance = this;
		else
			Destroy(this);
	}

	void Start () {
		bombCount = zero;
		GridManagerObject = GridUpdater.instance;
		blownHexagons = zero;
		colorCount = 7;
		InitializeUI();


	}
	
	void Update () {
		if (tick) {
			StartGameButton();
			tick = false;
		}
	}



	public void Score(int x) {
		blownHexagons += x;
		scoreText.text = (CellScore * blownHexagons).ToString();
		if (Int32.Parse(scoreText.text) > Bomb_score*bombCount + Bomb_score) {
			++bombCount;
			GridManagerObject.SetBombProduction();
		}
	}

	public void GameEnd() {
		gameOverScreen.SetActive(true);
	}

	public void BackButton (string sceneName) {
		SceneManager.LoadScene(sceneName);
	}





	/* Sends options to required objects and starts game */
	public void StartGameButton() {
		GridManagerObject.SetGridHeight(grid_Hieght);
		GridManagerObject.SetGridWidth(grid_width);

		List<Color> colors = new List<Color>();

		colors.Add(Color.blue);
		colors.Add(Color.red);
		colors.Add(Color.yellow);
		colors.Add(Color.green);
		colors.Add(Color.cyan);
		GridManagerObject.SetColorList(colors);
		GridManagerObject.InitializeGrid();
	}



	/* Sets default values to UI elements */
	private void InitializeUI() {

		Default();
StartGameButton();
		
	}



	/* Assigns all options to default values */
	private void Default() {

		colorCount = color_count;
		scoreText.text = blownHexagons.ToString();
		
	}
}
