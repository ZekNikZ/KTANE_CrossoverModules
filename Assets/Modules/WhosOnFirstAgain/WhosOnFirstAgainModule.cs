using UnityEngine;
using System.Collections;

public class WhosOnFirstAgainModule : MonoBehaviour {

	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMAudio Audio;
	public KMSelectable[] buttons;
	public MeshRenderer[] labels;
	public MeshRenderer screenLabel;
	public Material blankScreen;

	string[] words;
	string[,] groups;

	void Start () {
		GetComponent<KMBombModule>().OnActivate += OnActivate;

		words = new string[] {"READY", "FIRST", "NO", "BLANK", "NOTHING", "YES", "WHAT", "UHHH", "LEFT", "RIGHT", "MIDDLE", "OKAY", "WAIT", "PRESS", "YOU", "YOU ARE", "YOUR", "YOURE", "UR", "U", "UH HUH", "UH UH", "WHAT?", "DONE", "NEXT", "HOLD", "SURE", "LIKE", "LETS", "LET'S", "LET US", "LETTUCE"};
		groups = new string[,] {
			{"READY", "FIRST", "NO", "BLANK", "NOTHING", "YES", "WHAT", "UHHH"},
			{"LEFT", "RIGHT", "MIDDLE", "OKAY", "WAIT", "PRESS", "YOU", "YOU ARE"},
			{"YOUR", "YOURE", "UR", "U", "UH HUH", "UH UH", "WHAT?", "DONE"},
			{"NEXT", "HOLD", "SURE", "LIKE", "LETS", "LET'S", "LET US", "LETTUCE"}
		};
	}
	
	void OnActivate () {

	}
}
