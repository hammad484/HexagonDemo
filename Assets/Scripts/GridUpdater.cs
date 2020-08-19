using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GridUpdater : CellManager {
	/* One "instance" to rule them all! */
	public static GridUpdater instance = null;

	/* Variables to assign from editor */
	public GameObject hexPrefab;
	public GameObject hexParent;
	public GameObject outParent;
	public Sprite outlineSprite;
	public Sprite hexagonSprite;

	/* Member variables */
	private int gridWidth;
	private int gridHeight;
	private int selectionStatus;
	private bool colorblindMode;
	private bool bombProduction;
	private bool gameEnd;
	private Vector2 selectedPosition;
	private HexaCell selectedHexagon;
	private List<List<HexaCell>> gameGrid;
	private List<HexaCell> selectedGroup;
	private List<HexaCell> bombs;
	private List<Color> colorList;

	/* Coroutine status variables */
	private bool gameInitializiationStatus;
	private bool hexagonRotationStatus;
	private bool hexagonExplosionStatus;
	private bool hexagonProductionStatus;



	/* Assigning singleton if available */
	void Awake() {
		if (instance == null)
			instance = this;
		else
			Destroy(this);
	}

	void Start() {
		gameEnd = false;
		bombProduction = false;
		hexagonRotationStatus = false;
		hexagonExplosionStatus = false;
		hexagonProductionStatus = false;
		bombs = new List<HexaCell>();
		selectedGroup = new List<HexaCell>();
		gameGrid = new List<List<HexaCell>>();
	}



	/* Wrapper function for grid initializer coroutine. Width and height should be set before this call */
	public void InitializeGrid() {
		List<int> missingCells = new List<int>();


		/* Initialize gameGrid and fill missingCells */
		for (int i = 0; i<GetGridWidth(); ++i) {
			for (int j = 0; j<GetGridHeight(); ++j)
				missingCells.Add(i);

			gameGrid.Add(new List<HexaCell>());
		}

		/* Fill grid with hexagons */

		StartCoroutine(ProduceHexagons(missingCells, ColoredGridProducer()));
	}



	/* Function to select the hex group on touch position, returns the selected hexagon */
	public void Select(Collider2D collider) {
		/* If selection is different than current hex, reset status and variable */
		if (selectedHexagon == null || !selectedHexagon.GetComponent<Collider2D>().Equals(collider)) {
			selectedHexagon = collider.gameObject.GetComponent<HexaCell>();
			selectedPosition.x = selectedHexagon.GetX();
			selectedPosition.y = selectedHexagon.GetY();
			selectionStatus = 0;
		}

		/* Else increase selection status without exceeding total number */
		else {
			selectionStatus = (++selectionStatus) % status_count;
		}

		DestructOutline();
		ConstructOutline();
	}



	/* Function to rotate the hex group on touch position */
	public void Rotate(bool clockWise) {
		/* Specifying that rotation started and destroying outliner*/
		DestructOutline();
		StartCoroutine(RotationCheckCoroutine(clockWise));
	}



	#region SelectHelpers
	/* Helper function for Select() to find all 3 hexagons to be outlined */
	private void FindHexagonGroup() {
		List<HexaCell> returnValue = new List<HexaCell>();
		Vector2 firstPos, secondPos;

		/* Finding 2 other required hexagon coordinates on grid */
		selectedHexagon = gameGrid[(int)selectedPosition.x][(int)selectedPosition.y];
		FindOtherHexagons(out firstPos, out secondPos);
		selectedGroup.Clear();
		selectedGroup.Add(selectedHexagon);
		selectedGroup.Add(gameGrid[(int)firstPos.x][(int)firstPos.y].GetComponent<HexaCell>());
		selectedGroup.Add(gameGrid[(int)secondPos.x][(int)secondPos.y].GetComponent<HexaCell>());
	}


	/* Helper function for FindHexagonGroup() to locate neighbours of selected hexagon */
	private void FindOtherHexagons(out Vector2 first, out Vector2 second) {
		HexaCell.NeighbourHexes neighbours = selectedHexagon.GetNeighbours();
		bool breakLoop = false;


		/* Picking correct neighbour according to selection position */
		do {
			switch (selectionStatus) {
				case 0: first = neighbours.up; second = neighbours.upRight; break;
				case 1: first = neighbours.upRight; second = neighbours.downRight; break;
				case 2: first = neighbours.downRight; second = neighbours.down; break;
				case 3: first = neighbours.down; second = neighbours.downLeft; break;
				case 4: first = neighbours.downLeft; second = neighbours.upLeft; break;
				case 5: first = neighbours.upLeft; second = neighbours.up; break;
				default: first = Vector2.zero; second = Vector2.zero; break;
			}

			/* Loop until two neighbours with valid positions are found */
			if (first.x < zero || first.x >= gridWidth || first.y < zero || first.y >= gridHeight || second.x < zero || second.x >= gridWidth || second.y < zero || second.y >= gridHeight) {
				selectionStatus = (++selectionStatus) % status_count;
			}
			else {
				breakLoop = true;
			}
		} while (!breakLoop);
	}
	#endregion



	#region RotateHelpers
	/* Function to check if all hexagons finished rotating */
	private IEnumerator RotationCheckCoroutine(bool clockWise) {
		List<HexaCell> explosiveHexagons = null;
		bool flag = true;

		
		/* Rotate selected group until an explosive hexagon found or maximum rotation reached */
		hexagonRotationStatus = true;
		for (int i=0; i<selectedGroup.Count; ++i) {
			/* Swap hexagons and wait until they are completed rotation */
			SwapHexagons(clockWise);
			yield return new WaitForSeconds(0.3f);

			/* Check if there is any explosion available, break loop if it is */
			explosiveHexagons = CheckExplosion(gameGrid);
			if (explosiveHexagons.Count > zero) {
				break;
			}
		}


		/* Indicate that rotation has ended and explosion starts */
		hexagonExplosionStatus = true;
		hexagonRotationStatus = false;


		/* Explode the hexagons until no explosive hexagons are available */
		while (explosiveHexagons.Count > zero) {
			if (flag) {
				hexagonProductionStatus = true;
				StartCoroutine(ProduceHexagons(ExplodeHexagons(explosiveHexagons)));
				flag = false;
			}
				
			else if (!hexagonProductionStatus) {
				explosiveHexagons = CheckExplosion(gameGrid);
				flag = true;
			}

			yield return new WaitForSeconds(0.3f);
		}

		hexagonExplosionStatus = false;
		FindHexagonGroup();
		ConstructOutline();
	}



	/* Helper function to swap positions of currently selected 3 hexagons 
	 * TODO: Bad function, try to improve (look for more clean 3 variable swap)
	 */
	private void SwapHexagons(bool clockWise) {
		int x1, x2, x3, y1, y2, y3;
		Vector2 pos1, pos2, pos3;
		HexaCell first, second, third;


		
		/* Taking each position to local variables to prevent data loss during rotation */
		first = selectedGroup[0];
		second = selectedGroup[1];
		third = selectedGroup[2];



		x1 = first.GetX();
		x2 = second.GetX();
		x3 = third.GetX();

		y1 = first.GetY();
		y2 = second.GetY();
		y3 = third.GetY();

		pos1 = first.transform.position;
		pos2 = second.transform.position;
		pos3 = third.transform.position;


		/* If rotation is clokwise, rotate to the position of element on next index, else rotate to previous index */
		if (clockWise) {
			first.Rotate(x2, y2, pos2);
			gameGrid[x2][y2] = first;

			second.Rotate(x3, y3, pos3);
			gameGrid[x3][y3] = second;

			third.Rotate(x1, y1, pos1);
			gameGrid[x1][y1] = third;
		}
		else {
			first.Rotate(x3, y3, pos3);
			gameGrid[x3][y3] = first;

			second.Rotate(x1, y1, pos1);
			gameGrid[x1][y1] = second;

			third.Rotate(x2, y2, pos2);
			gameGrid[x2][y2] = third;
		}
		
		first.GetComponent<Animator>().Play("HexagonRotate");
		second.GetComponent<Animator>().Play("HexagonRotate");
		third.GetComponent<Animator>().Play("HexagonRotate");

	}
	#endregion



	#region ExplosionHelpers
	/* Returns a list that contains hexagons which are ready to explode, returns an empty list if there is none */
	private List<HexaCell> CheckExplosion(List<List<HexaCell>> listToCheck) {
		List<HexaCell> neighbourList = new List<HexaCell>();
		List<HexaCell> explosiveList = new List<HexaCell>();
		HexaCell currentHexagon;
		HexaCell.NeighbourHexes currentNeighbours;
		Color currentColor;


		for (int i = 0; i<listToCheck.Count; ++i) {
			for (int j = 0; j<listToCheck[i].Count; ++j) {
				/* Take current hexagon informations */
				currentHexagon = listToCheck[i][j];
				currentColor = currentHexagon.GetColor();
				currentNeighbours = currentHexagon.GetNeighbours();

				/* Fill neighbour list with up-upright-downright neighbours with valid positions */
				if (IsValid(currentNeighbours.up)) neighbourList.Add(gameGrid[(int)currentNeighbours.up.x][(int)currentNeighbours.up.y]);
				else neighbourList.Add(null);

				if (IsValid(currentNeighbours.upRight)) neighbourList.Add(gameGrid[(int)currentNeighbours.upRight.x][(int)currentNeighbours.upRight.y]);
				else neighbourList.Add(null);

				if (IsValid(currentNeighbours.downRight)) neighbourList.Add(gameGrid[(int)currentNeighbours.downRight.x][(int)currentNeighbours.downRight.y]);
				else neighbourList.Add(null);


				/* If current 3 hexagons are all same color then add them to explosion list */
				for (int k = 0; k<neighbourList.Count-1; ++k) {
					if (neighbourList[k] != null && neighbourList[k+1] != null) {
						if (neighbourList[k].GetColor() == currentColor && neighbourList[k+1].GetColor() == currentColor) {
							if (!explosiveList.Contains(neighbourList[k]))
								explosiveList.Add(neighbourList[k]);
							if (!explosiveList.Contains(neighbourList[k+1]))
								explosiveList.Add(neighbourList[k+1]);
							if (!explosiveList.Contains(currentHexagon))
								explosiveList.Add(currentHexagon);
						}
					}
				}

				neighbourList.Clear();
			}
		}


		return explosiveList;
	}



	/* Function to clear explosive hexagons and tidy up the grid */
	private List<int> ExplodeHexagons(List<HexaCell> list) {
		List<int> missingColumns = new List<int>();
		float positionX, positionY;


		/* Check for bombs */
		foreach (HexaCell hex in bombs) {
			if (!list.Contains(hex)) {
				hex.Tick();
				if (hex.GetTimer() == zero) {
					gameEnd = true;
					UI_Manager.instance.GameEnd();
					StopAllCoroutines();
					return missingColumns;
				}
			}
		}

		/* Remove hexagons from game grid */
		foreach (HexaCell hex in list) {
			if (bombs.Contains(hex)) {
				bombs.Remove(hex);
			}
			UI_Manager.instance.Score(1);
			gameGrid[hex.GetX()].Remove(hex);
			missingColumns.Add(hex.GetX());
			Destroy(hex.gameObject);
		}

		/* Re-assign left hexagon positions */
		foreach (int i in missingColumns) {
			for (int j=0; j<gameGrid[i].Count; ++j) {
				positionX = GetGridStartCoordinateX() + (CellDisHorizontal * i);
				positionY = (CellDisVertical * j * 2) + Grid_vertical + (OnStepper(i) ? CellDisVertical : zero);
				gameGrid[i][j].SetY(j);
				gameGrid[i][j].SetX(i);
				gameGrid[i][j].ChangeWorldPosition(new Vector3(positionX, positionY, zero));
			}
		}

		/* Indicate the end of process and return the missing column list */
		hexagonExplosionStatus = false;
		return missingColumns;
	}
	#endregion



	#region OutlineMethods
	/* Function to clear the outline objects */
	private void DestructOutline() {
		if (outParent.transform.childCount > zero) {
			foreach (Transform child in outParent.transform)
				Destroy(child.gameObject);
		}
	}
	


	/* Function to build outline */
	private void ConstructOutline() {
		/* Get selected hexagon group */
		FindHexagonGroup();

		/* Creating outlines by creating black hexagons on same position with selected 
		 * hexagons and making them bigger than actual hexagons. AKA fake shader programming 
		 * Yes, I should learn shader programming... 
		 */
		foreach (HexaCell outlinedHexagon in selectedGroup) {
			GameObject go = outlinedHexagon.gameObject;
			GameObject outline = new GameObject("Outline");
			GameObject outlineInner = new GameObject("Inner Object");

			outline.transform.parent = outParent.transform;

			outline.AddComponent<SpriteRenderer>();
			outline.GetComponent<SpriteRenderer>().sprite = outlineSprite;
			outline.GetComponent<SpriteRenderer>().color = Color.white;
			outline.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, -1);
			outline.transform.localScale = Cell_Outline;

			outlineInner.AddComponent<SpriteRenderer>();
			outlineInner.GetComponent<SpriteRenderer>().sprite = hexagonSprite;
			outlineInner.GetComponent<SpriteRenderer>().color = go.GetComponent<SpriteRenderer>().color;
			outlineInner.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, -2);
			outlineInner.transform.localScale = go.transform.localScale;
			outlineInner.transform.parent = outline.transform;
		}
	}
	#endregion


	
	/* Produces new hexagons on given columns */
	private IEnumerator ProduceHexagons(List<int> columns, List<List<Color>> colorSeed = null) {
		Vector3 startPosition;
		float positionX, positionY;
		float startX = GetGridStartCoordinateX();
		bool stepperStatus;


		/* Indication for the beginning of hexagon production */
		hexagonProductionStatus = true;

		/* Produce new hexagon, set variables  */
		foreach (int i in columns) {
			/* Instantiate new hexagon and give a little delay */
			stepperStatus = OnStepper(i);
			positionX = startX + (CellDisHorizontal * i);
			positionY = (CellDisVertical * gameGrid[i].Count * 2)  + Grid_vertical + (stepperStatus ? CellDisVertical : zero);
			startPosition = new Vector3(positionX, positionY, zero);

			GameObject newObj = Instantiate(hexPrefab, startPosition, Quaternion.identity, hexParent.transform);
			HexaCell newHex = newObj.GetComponent<HexaCell>();
			yield return new WaitForSeconds(DelayInNxtCell);


			/* Set bomb if production signal has arrived */
			if (bombProduction) {
				newHex.SetBomb();
				bombs.Add(newHex);
				bombProduction = false;
			}

			/* Set world and grid positions of hexagon */
			if (colorSeed == null)
				newHex.SetColor(colorList[(int)(Random.value * Random_cell)%colorList.Count]);
			else 
				newHex.SetColor(colorSeed[i][gameGrid[i].Count]);

			newHex.ChangeGridPosition(new Vector2(i, gameGrid[i].Count));
			gameGrid[i].Add(newHex);
		}

		/* Indication for the end of hexagon production */
		hexagonProductionStatus = false;
	}



	/* Function to produce a grid with valid colors */
	private List<List<Color>> ColoredGridProducer() {
		List<List<Color>> returnValue = new List<List<Color>>();
		List<Color> checkList = new List<Color>();
		bool exit = true;


		/* Creating a color list without ready to explode neighbours */
		for (int i = 0; i<GetGridWidth(); ++i) {
			returnValue.Add(new List<Color>());
			for (int j = 0; j<GetGridHeight(); ++j) {
				returnValue[i].Add(colorList[(int)(Random.value * Random_cell)%colorList.Count]);
				do {
					exit = true;
					returnValue[i][j] = colorList[(int)(Random.value * Random_cell)%colorList.Count];
					if (i-1 >= zero && j-1 >= zero) {
						if (returnValue[i][j-1] == returnValue[i][j] || returnValue[i-1][j] == returnValue[i][j])
							exit = false;
					}
				} while (!exit);
			}
		}


		return returnValue;
	}



	#region GeneralHelpers
	/* Helper function to find out if Hexagon standing on stepper or on base.
	 * midIndex is the index of middle column of the grid
	 * If index of both middleColumn and current column has same parity then hexagon is on stepper
	 */
	public bool OnStepper(int x) {
		int midIndex = GetGridWidth()/_half;
		return (midIndex%2 == x%2);
	}

	/* Checks coroutine status variables to see if game is ready to take input */
	public bool InputAvailabile() {
		return !hexagonProductionStatus && !gameEnd && !hexagonRotationStatus && !hexagonExplosionStatus;
	}



	/* Helper function to find the x coordinate of the world position of first column */
	private float GetGridStartCoordinateX() {
		return gridWidth/_half * -CellDisHorizontal;
	}



	/* Helper function to validate a position if it is in game grid */
	private bool IsValid(Vector2 pos) {
		return pos.x >= zero && pos.x < GetGridWidth() && pos.y >= zero && pos.y <GetGridHeight();
	}



	private void PrintGameGrid() {
		string map = "";


		for (int i = GetGridHeight()-1; i>=0; --i) {
			for (int j = 0; j<GetGridWidth(); ++j) {
				if (gameGrid[j][i] == null)
					map +=  "0 - ";
				else
					map += "1 - ";
			}

			map += "\n";
		}

		print(map);
	}
	#endregion


	/* Setters & Getters */
	public void SetGridWidth(int width) { gridWidth = width; }
	public void SetGridHeight(int height) { gridHeight = height; }
	public void SetColorblindMode(bool mode) { colorblindMode = mode; }
	public void SetColorList(List<Color> list) { colorList = list; }
	public void SetBombProduction() { bombProduction = true; }

	public int GetGridWidth() { return gridWidth; }
	public int GetGridHeight() { return gridHeight; }
	public bool GetColorblindMode() { return colorblindMode; }
	public HexaCell GetSelectedHexagon() { return selectedHexagon; }
}
