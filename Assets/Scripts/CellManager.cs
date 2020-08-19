using UnityEngine;



public class CellManager : MonoBehaviour {
	/* Constant variables */
	protected const int zero = 0;
	protected const int _double = 2;
	protected const int _half = 2;
	protected const int min_grid_width = 5;
	protected const int grid_width = 7;
	protected const int grid_Hieght = 8;
	protected const int color_count = 5;
	protected const int status_count = 6;
	protected const int Cell_slide_distance = 5;
	protected const int Cell_rotate = 9;
	protected const int CellScore = 5;
	protected const int Random_cell = 75486;
	protected const int Grid_vertical = -3;
	protected const int Bomb_timer = 5;
	protected const int Bomb_score = 1000;

	protected const float CellDisHorizontal = 0.445f;
	protected const float CellDisVertical = 0.23f;
	protected const float CellRotationThres = 0.05f;
	protected const float DelayInNxtCell = 0.025f;


	protected const string Tag = "Hexagon";


	protected readonly Vector3 Cell_Outline = new Vector3(0.685f, 0.685f, 0.685f);
}
