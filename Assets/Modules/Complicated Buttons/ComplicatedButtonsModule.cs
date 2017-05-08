using System;
using System.Linq;
using CrossoverModules;
using UnityEngine;

using Random = UnityEngine.Random;

public class ComplicatedButtonsModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public TextMesh[] Labels;
    public Material[] materials;

    static string[] words = { "Press", "Hold", "Detonate" };
    static string[] colorNames = { "White", "Red", "Blue", "Purple" };
    static int[,,] orders = {
        { { 1, 2, 3 }, { 2, 3, 1 }, { 3, 1, 2 }, { 1, 2, 3 } },
        { { 2, 1, 3 }, { 3, 2, 1 }, { 1, 3, 2 }, { 2, 3, 1 } },
        { { 3, 1, 2 }, { 1, 2, 3 }, { 2, 1, 3 }, { 3, 1, 2 } }
    };

    const string instructions = "PDPBRSDSRBPBRRSD";

    int[] order;
    int currentButton = 0;
    int[] actions;
    int moduleId;

    static int moduleIdCounter = 1;

    void Start()
    {
        moduleId = moduleIdCounter++;

        actions = new[] { 0, 1, 0 };

        for (int i = 0; i < 3; i++)
        {
            var j = i;
            Buttons[i].OnInteract += delegate { HandlePress(j, Buttons[j]); return false; };
            Labels[i].text = words[Random.Range(0, 3)];
            if (Labels[i].text == "Press") actions[i] += 2;

            var color = Random.Range(0, 4);
            Buttons[i].GetComponent<Renderer>().sharedMaterial = materials[color];
            if (color == 1 || color == 3) actions[i] += 8;
            if (color == 2 || color == 3) actions[i] += 4;

            Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: {2} {3}", moduleId, i + 1, colorNames[color], Labels[i].text);
        }

        BombModule.OnActivate += OnActivate;
    }

    void HandlePress(int button, KMSelectable buttonSelectable)
    {
        if (order == null)
            // Module not yet activated.
            return;

        Audio.PlaySoundAtTransform("tick", buttonSelectable.transform);
        buttonSelectable.AddInteractionPunch();

        if (order[currentButton] != button)
        {
            Debug.LogFormat("[Complicated Buttons #{0}] You pressed {1}, I expected {2}. Input reset.", moduleId, button, order[currentButton]);
            BombModule.HandleStrike();
            currentButton = 0;
            return;
        }
        Debug.LogFormat("[Complicated Buttons #{0}] Pressing {1} was correct.", moduleId, button);
        currentButton++;
        if (currentButton >= order.Length)
        {
            Debug.LogFormat("[Complicated Buttons #{0}] Module solved.", moduleId);
            BombModule.HandlePass();
        }
    }

    void OnActivate()
    {
        var col = Math.Min(6, BombInfo.GetBatteryCount()) / 2;
        var row = Array.IndexOf(words, Labels[0].text);

        Debug.LogFormat("[Complicated Buttons #{0}] Button order: {1}", moduleId, string.Join(", ", Enumerable.Range(0, 3).Select(i => orders[row, col, i].ToString()).ToArray()));

        order = Enumerable.Range(0, 3).Where(i => DetermineAction(orders[row, col, i])).Select(i => orders[row, col, i]).ToArray();

        if (order.Length == 0)
        {
            order = new[] { orders[row, col, 1] };
            Debug.LogFormat("[Complicated Buttons #{0}] No buttons to push. Must push Button {1}.", moduleId, order[0]);
        }
        else
            Debug.LogFormat("[Complicated Buttons #{0}] Buttons to push: {1}", moduleId, string.Join(", ", order.Select(i => i.ToString()).ToArray()));
    }

    bool DetermineAction(int button)
    {
        switch (instructions[actions[button - 1]])
        {
            case 'P':
                Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push.", moduleId, button);
                return true;
            case 'R':
                {
                    var res = BombInfo.GetSerialNumber().Distinct().Count() != BombInfo.GetSerialNumber().Length;
                    Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push if duplicate serial number characters ({2}).", moduleId, button, res);
                    return res;
                }
            case 'S':
                {
                    var res = BombInfo.IsPortPresent(KMBombInfoExtensions.KnownPortType.Serial);
                    Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push if serial port present ({2}).", moduleId, button, res);
                    return res;
                }
            case 'B':
                {
                    var res = BombInfo.GetBatteryHolderCount() >= 2;
                    Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Push if two or more battery holders ({2}).", moduleId, button, res);
                    return res;
                }
            default:
                Debug.LogFormat("[Complicated Buttons #{0}] Button {1}: Do not push.", moduleId, button);
                return false;
        }
    }
}