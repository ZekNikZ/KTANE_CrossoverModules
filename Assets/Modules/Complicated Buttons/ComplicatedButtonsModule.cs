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

    string[] words = { "Press", "Hold", "Detonate" };
    int[,,] orders;
    string instructions = "PDPBRSDSRBPBRRSD";

    int batteryCount = 0;
    List<int> order;
    int currentButton = 0;
    int[] actions;
    int moduleId;

    static int moduleIdCounter = 1;

    void Start() {
        moduleId = moduleIdCounter++;

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

        int word1 = Random.Range(0, 3);
        int word2 = Random.Range(0, 3);
        int word3 = Random.Range(0, 3);
        Label1.text = words[word1];
        Label2.text = words[word2];
        Label3.text = words[word3];
        if (words[word1] == "Press") num1 += 2;
        if (words[word2] == "Press") num2 += 2;
        if (words[word3] == "Press") num3 += 2;

        int color1 = Random.Range(0, 4);
        int color2 = Random.Range(0, 4);
        int color3 = Random.Range(0, 4);
        Button1.GetComponent<Renderer>().sharedMaterial = materials[color1];
        Button2.GetComponent<Renderer>().sharedMaterial = materials[color2];
        Button3.GetComponent<Renderer>().sharedMaterial = materials[color3];
        if (color1 == 1 || color1 == 3) num1 += 8;
        if (color2 == 1 || color2 == 3) num2 += 8;
        if (color3 == 1 || color3 == 3) num3 += 8;
        if (color1 == 2 || color1 == 3) num1 += 4;
        if (color2 == 2 || color2 == 3) num2 += 4;
        if (color3 == 2 || color3 == 3) num3 += 4;

        var colorNames = new[] { "White", "Red", "Blue", "Purple" };
        Debug.LogFormat("[Complicated Buttons #{0}] Button 1: {1} {2}", moduleId, colorNames[color1], words[word1]);
        Debug.LogFormat("[Complicated Buttons #{0}] Button 2: {1} {2}", moduleId, colorNames[color2], words[word2]);
        Debug.LogFormat("[Complicated Buttons #{0}] Button 3: {1} {2}", moduleId, colorNames[color3], words[word3]);

        actions = new int[] { num1, num2, num3 };

        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

	void OnActivate() {
        order = DetermineOrder();
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
            Debug.LogFormat("[Complicated Buttons #{0}] You pressed {1}, I expected {2}. Input reset.", moduleId, button, order[currentButton]);
            BombModule.HandleStrike();
            currentButton = 0;
            return;
        }
        Debug.LogFormat("[Complicated Buttons #{0}] Pressing {1} was correct.", moduleId, button);
        currentButton++;
		if (currentButton >= order.Count) {
            Debug.LogFormat("[Complicated Buttons #{0}] Module solved.", moduleId);
            BombModule.HandlePass();
        }
    }

	List<int> DetermineOrder() {
        int _batteries = batteryCount;
        if (batteryCount > 6) _batteries = 6;
        int batteries = _batteries / 2;

        Debug.LogFormat("[Complicated Buttons #{0}] Button order: {1}", moduleId, string.Join(", ", Enumerable.Range(0, 3).Select(i => orders[IndexOf(words, Label1.text), batteries, i].ToString()).ToArray()));

        List<int> result = new List<int>();
        if (DetermineAction(orders[IndexOf(words, Label1.text), batteries, 0])) result.Add(orders[IndexOf(words, Label1.text), batteries, 0]);
        if (DetermineAction(orders[IndexOf(words, Label1.text), batteries, 1])) result.Add(orders[IndexOf(words, Label1.text), batteries, 1]);
        if (DetermineAction(orders[IndexOf(words, Label1.text), batteries, 2])) result.Add(orders[IndexOf(words, Label1.text), batteries, 2]);

        if (result.Count() == 0)
        {
            result.Add(orders[IndexOf(words, Label1.text), batteries, 1]);
            Debug.LogFormat("[Complicated Buttons #{0}] No buttons to push. Must push Button {1}.", moduleId, result[0]);
        }
        else
            Debug.LogFormat("[Complicated Buttons #{0}] Buttons to push: {1}", moduleId, string.Join(", ", result.Select(i => i.ToString()).ToArray()));
        return result;
    }

	bool DetermineAction(int button) {
        char ins = instructions[actions[button - 1]];
		switch (ins) {
            case 'P':
                Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push.", moduleId, button);
                return true;
            case 'R':
                {
                    var res = KMBombInfoExtensions.GetSerialNumber(BombInfo).Distinct().Count() != KMBombInfoExtensions.GetSerialNumber(BombInfo).Length;
                    Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push if duplicate serial number characters ({2}).", moduleId, button, res);
                    return res;
                }
            case 'S':
                {
                    var res = KMBombInfoExtensions.IsPortPresent(BombInfo, KMBombInfoExtensions.KnownPortType.Serial);
                    Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push if serial port present ({2}).", moduleId, button, res);
                    return res;
                }
            case 'B':
                {
                    var res = KMBombInfoExtensions.GetBatteryHolderCount(BombInfo) >= 2;
                    Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push if two or more battery holders ({2}).", moduleId, button, res);
                    return res;
                }
            default:
                Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Do not push.", moduleId, button);
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