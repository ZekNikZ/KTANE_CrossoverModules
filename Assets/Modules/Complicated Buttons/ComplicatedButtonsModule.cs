using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

using CrossoverModules;

public class ComplicatedButtonsModule : MonoBehaviour {

	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMAudio KMAudio;
	public KMSelectable Button1;
	public KMSelectable Button2;
	public KMSelectable Button3;
	public TextMesh Label1;
	public TextMesh Label2;
	public TextMesh Label3;
	public Material[] materials;

	string[] words = {"Press", "Hold", "Detonate"};
	int[,,] orders;
	string instructions = "PDPBRSDSRBPBRRSD";

	int batteryCount = 0;
	List<int> order;
	int currentButton = 0;
	int[] actions;

	void Start() {
		orders = new int[,,]{
			{{1,2,3}, {2,3,1}, {3,1,2}, {1,2,3}},
			{{2,1,3}, {3,2,1}, {1,3,2}, {2,3,1}},
			{{3,1,2}, {1,2,3}, {2,1,3}, {3,1,2}}
		};

		int num1 = 0;
		int num2 = 1;
		int num3 = 0;

		Button1.OnInteract += HandlePress1;
		Button2.OnInteract += HandlePress2;
		Button3.OnInteract += HandlePress3;

		int word1 = Random.Range(0,3);
		int word2 = Random.Range(0,3);
		int word3 = Random.Range(0,3);
		Label1.text = words[word1];
		Label2.text = words[word2];
		Label3.text = words[word3];
		if (words[word1] == "Press") num1 += 2;
		if (words[word2] == "Press") num2 += 2;
		if (words[word3] == "Press") num3 += 2;

		int color1 = Random.Range(0,4);
		int color2 = Random.Range(0,4);
		int color3 = Random.Range(0,4);
		Button1.GetComponent<Renderer>().sharedMaterial = materials[color1];
		Button2.GetComponent<Renderer>().sharedMaterial = materials[color2];
		Button3.GetComponent<Renderer>().sharedMaterial = materials[color3];
		if (color1 == 1 || color1 == 3) num1 += 8;
		if (color2 == 1 || color2 == 3) num2 += 8;
		if (color3 == 1 || color3 == 3) num3 += 8;
		if (color1 == 2 || color1 == 3) num1 += 4;
		if (color2 == 2 || color2 == 3) num2 += 4;
		if (color3 == 2 || color3 == 3) num3 += 4;

		actions = new int[] {num1, num2, num3};

		GetComponent<KMBombModule>().OnActivate += OnActivate;
	}

	void OnActivate() {
		order = DetermineOrder();
		Debug.Log("Battery Count: " + BombInfo.GetBatteryCount());
		string result1 = "Order: {";
		foreach (int button in order) {
			result1 += button;
			result1 += ", ";
		}
		Debug.Log(result1 + "}");
		string result2 = "Actions: {";
		foreach (int button in order) {
			result2 += instructions[actions[button - 1]];
			result2 += ", ";
		}
		Debug.Log(result2 + "}");
		string result3 = "All Actions: {";
		for (int i = 0; i < 3; i++) {
			result3 += instructions[actions[i]];
			result3 += ", ";
		}
		Debug.Log(result3 + "}");
	}

	bool HandlePress1() {
		HandlePress(1);
		return false;
	}

	bool HandlePress2() {
		HandlePress(2);
		return false;
	}

	bool HandlePress3() {
		HandlePress(3);
		return false;
	}

	void HandlePress(int button) {
        if (order == null)
            // Module not yet activated.
            return;

		KMAudio.PlaySoundAtTransform("tick", this.transform);
		GetComponent<KMSelectable>().AddInteractionPunch();
		if (order[currentButton] != button) {
			BombModule.HandleStrike();
			currentButton = 0;
			return;
		}
		currentButton++;
		if (currentButton >= order.Count) {
			BombModule.HandlePass(); // FIXME
		}
	}

	List<int> DetermineOrder() {
		int _batteries = batteryCount;
		if (batteryCount > 6) _batteries = 6;
		int batteries = _batteries / 2;
		List<int> result = new List<int>(); //TODO: FINISH
		//return new int[] {orders[IndexOf(words, Label1.text),batteries,0], orders[IndexOf<string>(words, Label1.text),batteries,1], orders[IndexOf(words, Label1.text),batteries,2]};
		if (DetermineAction(orders[IndexOf(words, Label1.text),batteries,0])) result.Add(orders[IndexOf(words, Label1.text),batteries,0]);
		if (DetermineAction(orders[IndexOf(words, Label1.text),batteries,1])) result.Add(orders[IndexOf(words, Label1.text),batteries,1]);
		if (DetermineAction(orders[IndexOf(words, Label1.text),batteries,2])) result.Add(orders[IndexOf(words, Label1.text),batteries,2]);
		if (result.Count() == 0) result.Add(orders[IndexOf(words, Label1.text),batteries,1]);
		return result;
	}

	bool DetermineAction(int button) {
		char ins = instructions[actions[button - 1]];
		switch (ins) {
			case 'P':
				return true;
			case 'D':
				return false;
			case 'R':
				return KMBombInfoExtensions.GetSerialNumber(BombInfo).Distinct().Count() != KMBombInfoExtensions.GetSerialNumber(BombInfo).Length;
			case 'S':
				return KMBombInfoExtensions.IsPortPresent(BombInfo, KMBombInfoExtensions.KnownPortType.Serial);
			case 'B':
				return KMBombInfoExtensions.GetBatteryHolderCount(BombInfo) >= 2;
			default:
				return false;
		}
	}

	int IndexOf<T>(T[] array, T element) {
		for (int i = 0; i < array.Length; i++) {
			if (array[i].Equals(element)) {
				return i;
			}
		}
		return -1;
	}
}