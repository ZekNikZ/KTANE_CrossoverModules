using UnityEngine;
using System.Collections;

using CrossoverModules;

public class SymbolicPasswordModule : MonoBehaviour {

	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMAudio Audio;
	public KMSelectable[] buttons;
	public KMSelectable submitButton;
	public MeshRenderer[] labels;
	public Material[] symbols;
	public Material blankScreen;

	int[,] grid;
	int[,] symbolTable;
	int x;
	int y;

	void Start () {
		GetComponent<KMBombModule>().OnActivate += OnActivate;

		buttons[0].OnInteract += B1;
		buttons[1].OnInteract += B2;
		buttons[2].OnInteract += B3;
		buttons[3].OnInteract += B4;
		buttons[4].OnInteract += B5;
		buttons[5].OnInteract += B6;
		buttons[6].OnInteract += B7;
		buttons[7].OnInteract += B8;
		buttons[8].OnInteract += B9;
		buttons[9].OnInteract += B10;

		submitButton.OnInteract += Submit;

		symbolTable = new int[,] {
			{24, 14, 0, 9, 21, 9, 17},
			{11, 24, 7, 18, 3, 14, 2},
			{25, 20, 22, 26, 26, 23, 24},
			{10, 22, 4, 6, 19, 12, 25},
			{6, 2, 13, 4, 18, 21, 22},
			{8, 8, 25, 17, 16, 15, 14},
			{20, 17, 2, 3, 1, 5, 7}
		};

		grid = new int[2, 3];
		x = Random.Range(0, 5);
		y = Random.Range(0, 6);
		int[] _symbols = new int[6];
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < 3; j++) {
				_symbols[3*i+j] = symbolTable[x + i, y + j];
			}
		}
		ShuffleArray(_symbols);
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < 3; j++) {
				grid[i,j] = _symbols[3*i+j];
			}
		}
	}

	void OnActivate () {
		RedrawSymbols();
	}
	
	bool B1 () {return RotateSymbols(1);}
	bool B2 () {return RotateSymbols(1);}
	bool B3 () {return RotateSymbols(2);}
	bool B4 () {return RotateSymbols(2);}
	bool B5 () {return RotateSymbols(3);}
	bool B6 () {return RotateSymbols(3);}
	bool B7 () {return RotateSymbols(4, -1);}
	bool B8 () {return RotateSymbols(4, 1);}
	bool B9 () {return RotateSymbols(5, -1);}
	bool B10 () {return RotateSymbols(5, 1);}

	bool RotateSymbols (int line, int direction = 0) {
		Audio.PlaySoundAtTransform("tick", this.transform);
		GetComponent<KMSelectable>().AddInteractionPunch();
		int temp = -1;
		if (line < 4) {
			temp = grid[0, line - 1];
			grid[0, line - 1] = grid[1, line - 1];
			grid[1, line - 1] = temp;
		} else {
			if (direction == -1) {
				temp = grid[line - 4, 0];
				grid[line - 4, 0] = grid[line - 4, 1];
				grid[line - 4, 1] = grid[line - 4, 2];
				grid[line - 4, 2] = temp;
			} else {
				temp = grid[line - 4, 2];
				grid[line - 4, 2] = grid[line - 4, 1];
				grid[line - 4, 1] = grid[line - 4, 0];
				grid[line - 4, 0] = temp;
			}
		}
		RedrawSymbols();
		return false;
	}

	void RedrawSymbols () {
		for (int i = 0; i < 6; i++) {
			labels[i].sharedMaterial = symbols[grid[i / 3, i % 3]];
		}
	}

	bool Submit () {
		Audio.PlaySoundAtTransform("tick", this.transform);
		GetComponent<KMSelectable>().AddInteractionPunch();
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < 3; j++) {
				if (grid[i,j] != symbolTable[x + i, y + j]) {
					BombModule.HandleStrike();
					return false;
				}
			}
		}
		BombModule.HandlePass();
		return false;
	}

	void ShuffleArray<T>(T[] arr) {
		for (int i = arr.Length - 1; i > 0; i--) {
			int r = Random.Range(0, i);
			T tmp = arr[i];
			arr[i] = arr[r];
			arr[r] = tmp;
		}
	}
}
