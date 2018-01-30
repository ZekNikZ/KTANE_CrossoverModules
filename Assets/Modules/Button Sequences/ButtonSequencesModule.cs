using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ButtonSequencesModule : MonoBehaviour {

    // Module Hooks
    public KMBombModule BombModule;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable ModuleSelectable;

    // Buttons
    public KMSelectable NextPanelButton;
    public KMSelectable LastPanelButton;
    public KMSelectable[] Buttons;
    public MeshFilter[] ButtonMeshes;
    public Renderer[] ButtonStems;
    public MeshFilter[] ButtonHighlights;
    public TextMesh[] ButtonLabels;

    // Lights
    public Renderer[] Lights;
    public Material[] LightColors; // ORANGE RED GREEN BLUE YELLOW WHITE MAGENTA BLACK
    public GameObject[] StageIndicatorLights;

    // Animators
    public Animator DoorAnimator;
    public Animator[] ButtonAnimators;

    // Button Options
    public Mesh[] Meshes; // CIRCLE SQUARE HEXAGON
    public Material[] Colors; // RED BLUE YELLOW WHITE
    private Color[] TextColors = { Color.white, Color.white, Color.black, Color.black };
    private string[] TextLabels = { "A", "D", "H", "P" };

    // Button Option Names
    private string[] ShapeNames = { "CIRCLE", "SQUARE", "HEXAGON" };
    private string[] ColorNames = { "RED", "BLUE", "YELLOW", "WHITE" };
    private string[] LabelNames = { "ABORT", "DETONATE", "HOLD", "PRESS" };

    // Action Names
    private string[] ActionNames = { "DO NOTHING", "PRESS", "HOLD" };

    // Button 
    private ButtonInfo[,] PanelInfo;

    // Module Identifier
    private int moduleId;
    private static int moduleIdCounter = 1;

    // Helper Class
    private class ButtonInfo {
        public int color;
        public int text;
        public int shape;

        public ButtonInfo(int color, int text, int shape) {
            this.color = color;
            this.text = text;
            this.shape = shape;
        }
    }

    // Rule Tables
    private int[,,] OccurenceTable = new int[,,] {
        { // RED Buttons
            { 0, 1 }, { 1, 2 }, { 2, 0 }, { 0, 0 }, { 3, 1 }
        },
        { // BLUE Buttons
            { 2, 0 }, { 0, 1 }, { 1, 2 }, { 3, 1 }, { 3, 2 }
        },
        { // YELLOW Buttons
            { 1, 0 }, { 2, 2 }, { 0, 1 }, { 3, 0 }, { 2, 2 }
        },
        { // WHTIE Buttons
            { 2, 2 }, { 1, 1 }, { 3, 2 }, { 0, 0 }, { 1, 1 }
        }
    };

    // Solution
    private int PanelCount;
    private const int MinPanels = 4;
    private const int MaxPanels = 4;
    private int[,] Solution;

    // Volatile Fields
    private int currentPanel = 0;
    private bool needsUpdate = false;
    private int[,] currentState;
    private int[,] lightState;
    private bool buttonsActive = true;
    private float holdTimer = -1;
    private int heldButton = -1;
    private int holdBombTime = -1;

    private void Awake() {
        // Module Identifier
        moduleId = moduleIdCounter++;

        // Module Hooks
        BombModule.OnActivate += OnActivate;

        // Button Hooks
        NextPanelButton.OnInteract += delegate { NextPanel(); return false; };
        LastPanelButton.OnInteract += delegate { LastPanel(); return false; };
        NextPanelButton.OnInteractEnded += delegate { PanelButtonRelease(); };
        LastPanelButton.OnInteractEnded += delegate { PanelButtonRelease(); };
        for (int i = 0; i < 3; i++) {
            var j = i;
            Buttons[i].OnInteract += delegate { HandlePress(j); return false; };
            Buttons[i].OnInteractEnded += delegate { ButtonRelease(j); };
        }

        PanelCount = Random.Range(MinPanels, MaxPanels + 1);

        PanelInfo = new ButtonInfo[PanelCount, 3];
        /*
        ButtonInfo[] buttons = new ButtonInfo[15];

        for (int i = 0; i < 11; i++) {
            buttons[i] = new ButtonInfo(Random.Range(0, 4), Random.Range(0, 4), Random.Range(0, 3));
        }
        for (int i = 11; i < 15; i++) {
            buttons[i] = null;
        }
        ShuffleArray(buttons);

        for (int i = 0; i < PanelCount; i++) {
            for (int j = 0; j < 3; j++) {
                PanelInfo[i, j] = buttons[i*3 + j];
            }
        }*/

        for (int i = 0; i < PanelCount; i++) {
            for (int j = 0; j < 3; j++) {
                PanelInfo[i, j] = new ButtonInfo(
                        Random.Range(0, 4),
                        Random.Range(0, 4),
                        Random.Range(0, 3)
                    );
            }
        }

        UpdateButtons();

        lightState = new int[PanelCount, 3];
        UpdateLights();

        for (int i = 0; i < StageIndicatorLights.Length; i++) {
            StageIndicatorLights[i].SetActive(false);
        }

        Solution = new int[PanelCount, 3];
        currentState = new int[PanelCount, 3];
        FindSolution();
    }

    private void Update() {
        //if (holdTimer >= 0) holdTimer++;
        if (holdBombTime == -1 && holdTimer > 0 && Time.time - holdTimer > 0.5f) {
            if (Solution[currentPanel, heldButton] == 1) {
                Debug.LogFormat("[Button Sequences #{0}] Panel {1} Button {2} is being held, even though it shouldn't be.", moduleId, currentPanel + 1, heldButton + 1);
            } else {
                string colorName;
                int color = Random.Range(0, 5);
                switch (color) {
                    case 0:
                        lightState[currentPanel, heldButton] = 3;
                        holdBombTime = 2;
                        colorName = "BLUE";
                        break;
                    case 1:
                        lightState[currentPanel, heldButton] = 5;
                        holdBombTime = 7;
                        colorName = "WHITE";
                        break;
                    case 2:
                        lightState[currentPanel, heldButton] = 4;
                        holdBombTime = 3;
                        colorName = "YELLOW";
                        break;
                    case 3:
                        lightState[currentPanel, heldButton] = 6;
                        holdBombTime = 4;
                        colorName = "MAGENTA";
                        break;
                    case 4:
                        lightState[currentPanel, heldButton] = 8;
                        holdBombTime = 0;
                        colorName = "CYAN";
                        break;
                    default:
                        holdBombTime = -1;
                        colorName = "ERROR";
                        break;
                }
                Debug.LogFormat("[Button Sequences #{0}] Panel {1} Button {2} is being held. LED Color: {3}. Release when timer contains a \"{4}\".", moduleId, currentPanel + 1, heldButton + 1, colorName, holdBombTime);
                UpdateLights();
            }
        }
        if (needsUpdate && DoorAnimator.GetCurrentAnimatorStateInfo(0).IsName("DoorOpen")) {
            UpdateButtons();
            UpdateLights();
            needsUpdate = false;
        }
    }

    private void OnActivate() {
        UpdateButtons();
        UpdateLights();
        DoorAnimator.Play("Begin");
        foreach (var anim in ButtonAnimators) {
            anim.Play("Begin");
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, this.transform);
    }

    private void FindSolution() {
        Debug.LogFormat("[Button Sequences #{0}] Panel Count: {1}", moduleId, PanelCount);
        Debug.LogFormat("[Button Sequences #{0}] Solution:", moduleId);
        int[] occurences = new int[Colors.Length];
        for (int i = 0; i < PanelCount; i++) {
            for (int j = 0; j < 3; j++) {
                if (PanelInfo[i, j] == null) {
                    Solution[i, j] = 0;
                } else {
                    Solution[i, j] = (OccurenceTable[PanelInfo[i, j].color, (++occurences[PanelInfo[i, j].color] - 1) % 5, 0] == PanelInfo[i, j].text ? 1 : 0) + (OccurenceTable[PanelInfo[i, j].color, (occurences[PanelInfo[i, j].color] - 1) % 5, 1] == PanelInfo[i, j].shape ? 1 : 0);
                    Debug.LogFormat("[Button Sequences #{0}] Panel {1} Button {2} ({3} {4} {5}): Occurence #{6} {7}", moduleId, i + 1, j + 1, ColorNames[PanelInfo[i, j].color], ShapeNames[PanelInfo[i, j].shape], LabelNames[PanelInfo[i, j].text], occurences[PanelInfo[i, j].color], ActionNames[Solution[i, j]]);
                }
            }
        }
    }

    // Button Methods
    private void UpdateButtons() {
        for (int i = 0; i < 3; i++) {
            if (PanelInfo[currentPanel, i] == null) {
                Buttons[i].gameObject.SetActive(false);
            } else {
                Buttons[i].gameObject.SetActive(true);
                ButtonMeshes[i].mesh = Meshes[PanelInfo[currentPanel, i].shape];
                ButtonMeshes[i].gameObject.GetComponent<Renderer>().sharedMaterial = Colors[PanelInfo[currentPanel, i].color];
                ButtonStems[i].sharedMaterial = Colors[PanelInfo[currentPanel, i].color];
                ButtonLabels[i].text = TextLabels[PanelInfo[currentPanel, i].text];
                ButtonLabels[i].color = TextColors[PanelInfo[currentPanel, i].color];
                ButtonHighlights[i].mesh = Meshes[PanelInfo[currentPanel, i].shape];
                foreach (var filter in ButtonHighlights[i].gameObject.GetComponentsInChildren<MeshFilter>(true)) {
                    filter.mesh = Meshes[PanelInfo[currentPanel, i].shape];
                }
            }
        }
    }

    private void LastPanel() {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);
        if (buttonsActive) {
            if (currentPanel > 0) {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, this.transform);
                foreach (var anim in ButtonAnimators) {
                    anim.Play("HideButtonAnimation");
                }
                DoorAnimator.Play("DoorClose");
                needsUpdate = true;
                currentPanel--;
            }
        }
    }

    private void NextPanel() {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);
        if (buttonsActive) {
            bool correct = true;
            for (int i = 0; i < 3; i++) {
                if (Solution[currentPanel, i] != currentState[currentPanel, i]) {
                    correct = false;
                    break;
                }
            }
            if (correct && currentPanel < PanelCount - 1) {
                Debug.LogFormat("[Button Sequences #{0}] Panel {1} completed successfully.", moduleId, currentPanel + 1);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, this.transform);
                foreach (var anim in ButtonAnimators) {
                    anim.Play("HideButtonAnimation");
                }
                DoorAnimator.Play("DoorClose");
                needsUpdate = true;
                for (int i = 0; i < 3; i++) {
                    lightState[currentPanel, i] = 2;
                }
                currentPanel++;
                for (int i = 0; i < currentPanel; i++) {
                    StageIndicatorLights[i].SetActive(true);
                }
            } else if (!correct) {
                Debug.LogFormat("[Button Sequences #{0}] Strike: User tried to move past panel {1} without completing it.", moduleId, currentPanel + 1);
                BombModule.HandleStrike();
            } else {
                Debug.LogFormat("[Button Sequences #{0}] Panel {1} completed successfully.", moduleId, currentPanel + 1);
                Debug.LogFormat("[Button Sequences #{0}] Module Solved.", moduleId, currentPanel + 1);
                BombModule.HandlePass();
                for (int i = 0; i < 3; i++) {
                    lightState[currentPanel, i] = 2;
                }
                UpdateLights();
                foreach (var anim in ButtonAnimators) {
                    anim.Play("Solve");
                }
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, this.transform);
                DoorAnimator.Play("Solve");
                for (int i = 0; i < StageIndicatorLights.Length; i++) {
                    StageIndicatorLights[i].SetActive(true);
                }
                buttonsActive = false;
            }
        }
    }

    private void PanelButtonRelease() {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, this.transform);
    }

    private void ButtonRelease(int button) {
        ButtonAnimators[button].Play("ButtonUpAnimation");
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, this.transform);
        if (currentState[currentPanel, button] == Solution[currentPanel, button]) {
            BombModule.HandleStrike();
            lightState[currentPanel, button] = 1;
            if (Solution[currentPanel, button] == 0) {
                Debug.LogFormat("[Button Sequences #{0}] Strike: Panel {1} Button {2} pressed/held when not needed.", moduleId, currentPanel + 1, button + 1);
            } else {
                Debug.LogFormat("[Button Sequences #{0}] Strike: Panel {1} Button {2} pressed when already dealt with.", moduleId, currentPanel + 1, button + 1);
            }
        } else {
            if (Time.time - holdTimer > 0.5f) {
                string time = BombInfo.GetFormattedTime();
                if (time.Length == 4) {
                    time = '0' + time;
                }
                if (Solution[currentPanel, button] == 1) {
                    BombModule.HandleStrike();
                    lightState[currentPanel, button] = 1;
                    Debug.LogFormat("[Button Sequences #{0}] Panel {1} Button {2} held when it shouldn't have been.", moduleId, currentPanel + 1, button + 1);
                } else if (time.Contains(holdBombTime.ToString())) {
                    currentState[currentPanel, button] = 2;
                    lightState[currentPanel, button] = 2;
                    Debug.LogFormat("[Button Sequences #{0}] Panel {1} Button {2} held successfully.", moduleId, currentPanel + 1, button + 1);
                } else {
                    BombModule.HandleStrike();
                    lightState[currentPanel, button] = 1;
                    Debug.LogFormat("[Button Sequences #{0}] Strike: Panel {1} Button {2} released at improper time. Current Time: {3}", moduleId, currentPanel + 1, button + 1, time);
                }
            } else if (Solution[currentPanel, button] == 1) {
                currentState[currentPanel, button] = 1;
                lightState[currentPanel, button] = 2;
                Debug.LogFormat("[Button Sequences #{0}] Panel {1} Button {2} pressed successfully.", moduleId, currentPanel + 1, button + 1);
            } else {
                BombModule.HandleStrike();
                lightState[currentPanel, button] = 1;
                Debug.LogFormat("[Button Sequences #{0}] Strike: Panel {1} Button {2} pressed when it should have been held.", moduleId, currentPanel + 1, button + 1);
            }
        }
        UpdateLights();
        heldButton = -1;
        holdTimer = -1;
        holdBombTime = -1;
    }

    private void HandlePress(int button) {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, this.transform);
        ButtonAnimators[button].Play("ButtonDownAnimation");
        heldButton = button;
        holdTimer = Time.time;
    }

    // Light Methods
    private void UpdateLights() {
        for (int i = 0; i < 3; i++) {
            if (PanelInfo[currentPanel, i] == null) {
                Lights[i].sharedMaterial = LightColors[7];
            } else {
                Lights[i].sharedMaterial = LightColors[lightState[currentPanel, i]];
            }
        }
    }

    private void ShuffleArray<T>(T[] arr) {
        for (int t = 0; t < arr.Length; t++) {
            T tmp = arr[t];
            int r = Random.Range(t, arr.Length);
            arr[t] = arr[r];
            arr[r] = tmp;
        }
    }

    private int _heldIndex = -1;
    private string TwitchHelpMessage = "Tap the buttons with !{0} tap 1 3. Move to the next panel !{0} down. Move to the previous panel with !{0} up. Cycle the panels done so far with !{0} cycle.  Hold the button with !{0} hold 2. Release the button with !{0} release 7.  Buttons are 1, 2, and 3. If the only thing you need to do on the current panel are taps, you can tap and move to the next panel with !{0} tap 1 2 down.";
    private IEnumerator ProcessTwitchCommand(string inputCommand)
    {
        string[] split = inputCommand.ToLowerInvariant().Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
        switch (split[0])
        {
            case "cycle":
                if (split.Length > 1) yield break;
                yield return null;
                if (_heldIndex > -1)
                {
                    yield return string.Format("sendtochaterror you can not cycle the panels until button {0} is released", _heldIndex + 1);
                    yield break;
                }
                yield return null;
                int panel = currentPanel;
                while (currentPanel != 0)
                {
                    LastPanelButton.OnInteract();
                    LastPanelButton.OnInteractEnded();
                    yield return null;
                    while (!DoorAnimator.GetCurrentAnimatorStateInfo(0).IsName("OpenIdle")) yield return null;
                }
                while (currentPanel != panel)
                {
                    yield return new WaitForSeconds(3.0f);
                    NextPanelButton.OnInteract();
                    NextPanelButton.OnInteractEnded();
                    yield return null;
                    while (!DoorAnimator.GetCurrentAnimatorStateInfo(0).IsName("OpenIdle")) yield return null;
                }
                yield return new WaitForSeconds(1.0f);
                break;
            case "up":
            case "u":
                if (split.Length > 1) yield break;
                yield return null;
                if (_heldIndex > -1)
                {
                    yield return string.Format("sendtochaterror you can not move to the previous panel until button {0} is released", _heldIndex + 1);
                    yield break;
                }
                LastPanelButton.OnInteract();
                LastPanelButton.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
                break;
            case "down":
            case "d":
                if (split.Length > 1) yield break;
                yield return null;
                if (_heldIndex > -1)
                {
                    yield return string.Format("sendtochaterror you can not move to the next panel until button {0} is released", _heldIndex + 1);
                    yield break;
                }
                yield return "strikemessage attempting to move to the next panel";
                NextPanelButton.OnInteract();
                NextPanelButton.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);
                break;
            case "hold":
                int holdIndex;
                if (split.Length != 2 || !int.TryParse(split[1], out holdIndex) || holdIndex < 1 || holdIndex > 3) yield break;
                yield return null;
                if (_heldIndex > -1)
                {
                    yield return string.Format("sendtochaterror you can not hold button {1} until button {0} is released", _heldIndex + 1, holdIndex);
                    yield break;
                }
                _heldIndex = holdIndex - 1;
                Buttons[_heldIndex].OnInteract();
                yield return new WaitForSeconds(0.1f);
                break;
            case "release":
                int releaseTime;
                if (split.Length != 2 || !int.TryParse(split[1], out releaseTime) || releaseTime < 0 || releaseTime > 9) yield break;
                if (_heldIndex == -1)
                {
                    yield return null;
                    yield return "sendtochaterror you are not currently holding any buttons.";
                    yield break;
                }
                yield return null;
                string time = BombInfo.GetFormattedTime();
                if (time.Length == 4)
                    time = '0' + time;
                while (!time.Contains(releaseTime.ToString()))
                {
                    yield return null;
                    time = BombInfo.GetFormattedTime();
                    if (time.Length == 4)
                        time = '0' + time;
                }
                Buttons[_heldIndex].OnInteractEnded();
                _heldIndex = -1;
                yield return new WaitForSeconds(0.1f);
                break;
            case "tap":
                string[] validTaps = new[] { "up", "u", "down", "d", "1", "2", "3" };
                if (split.Skip(1).Any(x => !validTaps.Contains(x))) yield break;
                if (split.Length == 1) yield break;
                yield return null;
                if (_heldIndex > -1)
                {
                    yield return string.Format("sendtochaterror you can not tap any buttons or change panels until button {0} is released", _heldIndex + 1);
                    yield break;
                }
                foreach (string tap in split.Skip(1))
                {
                    if (tap == "up" || tap == "u")
                    {
                        LastPanelButton.OnInteract();
                        LastPanelButton.OnInteractEnded();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                    if (tap == "down" || tap == "d")
                    {
                        yield return "strikemessage attempting to move to the next panel";
                        NextPanelButton.OnInteract();
                        NextPanelButton.OnInteractEnded();
                        yield return new WaitForSeconds(0.1f);
                        break;
                    }
                    yield return "strikemessage tapping button " + tap;
                    int tapIndex = int.Parse(tap);
                    Buttons[tapIndex - 1].OnInteract();
                    Buttons[tapIndex - 1].OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                }
                break;
            default:
                yield break;
        }
    }
}