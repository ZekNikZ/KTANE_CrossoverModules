using System.Linq;
using UnityEngine;

public class SymbolicPasswordModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio Audio;
    public KMSelectable[] buttons;
    public KMSelectable submitButton;
    public MeshRenderer[] labels;
    public Material[] symbols;
    public Material blankScreen;

    // This is the arrangement of the symbols as printed in the manual.
    static char[,] symbolTable = new char[,] {
        {'Ϙ', 'Ӭ', '©', 'Ϭ', 'Ψ', 'Ϭ', '¿'},
        {'Ѧ', 'Ϙ', 'Ѽ', '¶', 'ټ', 'Ӭ', '☆'},
        {'ƛ', 'Ͽ', 'Ҩ', 'Ѣ', 'Ѣ', '҂', 'Ϙ'},
        {'Ϟ', 'Ҩ', 'Җ', 'Ѭ', 'Ͼ', 'æ', 'ƛ'},
        {'Ѭ', '☆', 'Ԇ', 'Җ', '¶', 'Ψ', 'Ҩ'},
        {'ϗ', 'ϗ', 'ƛ', '¿', 'Ѯ', 'Ҋ', 'Ӭ'},
        {'Ͽ', '¿', '☆', 'ټ', '★', 'Ω', 'Ѽ'}
    };

    // This is the order in which the materials are stored in ‘symbols’.
    static string characters = "©★☆ټҖΩѬѼϗϬϞѦæԆӬҊѮ¿¶ϾϿΨҨ҂ϘƛѢ";

    char[,] display;
    int x;
    int y;
    int moduleId;
    static int moduleIdCounter = 1;

    void Start()
    {
        moduleId = moduleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += OnActivate;

        buttons[0].OnInteract += delegate { return RotateVertical(buttons[0], 1); };
        buttons[1].OnInteract += delegate { return RotateVertical(buttons[1], 1); };
        buttons[2].OnInteract += delegate { return RotateVertical(buttons[2], 2); };
        buttons[3].OnInteract += delegate { return RotateVertical(buttons[3], 2); };
        buttons[4].OnInteract += delegate { return RotateVertical(buttons[4], 3); };
        buttons[5].OnInteract += delegate { return RotateVertical(buttons[5], 3); };
        buttons[6].OnInteract += delegate { return RotateHorizontal(buttons[6], 0, -1); };
        buttons[7].OnInteract += delegate { return RotateHorizontal(buttons[7], 0, 1); };
        buttons[8].OnInteract += delegate { return RotateHorizontal(buttons[8], 1, -1); };
        buttons[9].OnInteract += delegate { return RotateHorizontal(buttons[9], 1, 1); };

        submitButton.OnInteract += Submit;

        x = Random.Range(0, 5); // x = 0–4
        y = Random.Range(0, 6); // y = 0–5
        Debug.LogFormat("[Symbolic Password #{0}] Position in grid: ({1}, {2})", moduleId, x + 1, y + 1);

        var initialOrder = Enumerable.Range(0, 6).ToArray();
        ShuffleArray(initialOrder);
        display = new char[2, 3];
        for (int i = 0; i < 6; i++)
            display[i / 3, i % 3] = symbolTable[y + initialOrder[i] / 3, x + initialOrder[i] % 3];
        Debug.LogFormat("[Symbolic Password #{0}] Initial display: {1} / {2}", moduleId, new string(Enumerable.Range(0, 3).Select(i => display[0, i]).ToArray()), new string(Enumerable.Range(0, 3).Select(i => display[1, i]).ToArray()));
        Debug.LogFormat("[Symbolic Password #{0}] Solution: {1}", moduleId, new string(Enumerable.Range(0, 6).Select(i => symbolTable[y + i / 3, x + i % 3]).ToArray()).Insert(3, " / "));
    }

    void OnActivate()
    {
        RedrawSymbols();
    }

    bool RotateVertical(KMSelectable button, int column)
    {
        Audio.PlaySoundAtTransform("tick", button.transform);
        button.AddInteractionPunch();

        var temp = display[0, column - 1];
        display[0, column - 1] = display[1, column - 1];
        display[1, column - 1] = temp;
        RedrawSymbols();
        return false;
    }

    bool RotateHorizontal(KMSelectable button, int line, int direction = 0)
    {
        Audio.PlaySoundAtTransform("tick", button.transform);
        button.AddInteractionPunch();

        if (direction == -1)
        {
            var temp = display[line, 0];
            display[line, 0] = display[line, 1];
            display[line, 1] = display[line, 2];
            display[line, 2] = temp;
        }
        else
        {
            var temp = display[line, 2];
            display[line, 2] = display[line, 1];
            display[line, 1] = display[line, 0];
            display[line, 0] = temp;
        }

        RedrawSymbols();
        return false;
    }

    void RedrawSymbols()
    {
        for (int i = 0; i < 6; i++)
            labels[i].sharedMaterial = symbols[characters.IndexOf(display[i / 3, i % 3])];
    }

    bool Submit()
    {
        Audio.PlaySoundAtTransform("tick", this.transform);
        GetComponent<KMSelectable>().AddInteractionPunch();
        Debug.LogFormat("[Symbolic Password #{0}] You submitted: {1} / {2}", moduleId, new string(Enumerable.Range(0, 3).Select(i => display[0, i]).ToArray()), new string(Enumerable.Range(0, 3).Select(i => display[1, i]).ToArray()));

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (display[j, i] != symbolTable[y + j, x + i])
                {
                    BombModule.HandleStrike();
                    return false;
                }
            }
        }

        Debug.LogFormat("[Symbolic Password #{0}] Module solved.", moduleId);
        BombModule.HandlePass();
        return false;
    }

    void ShuffleArray<T>(T[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int r = Random.Range(0, i);
            T tmp = arr[i];
            arr[i] = arr[r];
            arr[r] = tmp;
        }
    }
}
