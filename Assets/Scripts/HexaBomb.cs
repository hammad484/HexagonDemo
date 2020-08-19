using UnityEngine;

public class HexaBomb : HexaCell {
	public TextMesh DisplayText;
	private int Timer;


	public void Tick() { --Timer; DisplayText.text = Timer.ToString(); }
	public int GetClock() { return Timer; }
	public void SetClock(int value) { Timer = value; DisplayText.text = Timer.ToString(); }
}
