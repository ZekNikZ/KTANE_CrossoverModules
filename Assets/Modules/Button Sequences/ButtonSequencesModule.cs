using UnityEngine;

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
    public Material[] LightColors; // ORANGE RED GREEN BLUE YELLOW WHITE

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
    int moduleId;
    static int moduleIdCounter = 1;

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

    private int[] HoldLightTable = new int[] { 2, 4, 0, 1 }; // RED BLUE YELLOW WHITE (mod 5)

    // Solution
    private int PanelCount;
    private const int MinPanels = 3;
    private const int MaxPanels = 5;
    private int[,] Solution;

    // Volatile Fields
    private int currentPanel = 0;
    private bool needsUpdate = false;
    private int[,] currentState;
    private int[,] lightState;
    private bool buttonsActive = true;
    int holdTimer = -1;
    int heldButton = -1;
    int holdBombTime = -1;

    void Awake() {
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

        Solution = new int[PanelCount, 3];
        currentState = new int[PanelCount, 3];
        FindSolution();
    }

    void Update() {
        if (holdTimer >= 0) holdTimer++;
        if (holdTimer > 40 && holdBombTime == -1) {
            int color = Random.Range(0, 4);
            switch(color) {
                case 0:
                    lightState[currentPanel, heldButton] = 3;
                    holdBombTime = 4;
                    break;
                case 1:
                    lightState[currentPanel, heldButton] = 5;
                    holdBombTime = 1;
                    break;
                case 2:
                    lightState[currentPanel, heldButton] = 4;
                    holdBombTime = 5;
                    break;
                case 3:
                    lightState[currentPanel, heldButton] = 1;
                    holdBombTime = 1;
                    break;
            }
            UpdateLights();
        }
        if (needsUpdate && DoorAnimator.GetCurrentAnimatorStateInfo(0).IsName("DoorOpen")) {
            UpdateButtons();
            UpdateLights();
            needsUpdate = false;
        }
    }

    void OnActivate() {
        UpdateButtons();
        UpdateLights();
        DoorAnimator.Play("Begin");
        foreach (var anim in ButtonAnimators) {
            anim.Play("Begin");
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, this.transform);
    }

    void FindSolution() {
        Debug.LogFormat("[Button Sequences #{0}] Panel Count: {1}", moduleId, PanelCount);
        Debug.LogFormat("[Button Sequences #{0}] Solution:", moduleId);
        int[] occurences = new int[Colors.Length];
        for (int i = 0; i < PanelCount; i++) {
            for (int j = 0; j < 3; j++) {
                Solution[i, j] = (OccurenceTable[PanelInfo[i, j].color, (++occurences[PanelInfo[i, j].color] - 1) % 5, 0] == PanelInfo[i, j].text ? 1 : 0) + (OccurenceTable[PanelInfo[i, j].color, (occurences[PanelInfo[i, j].color] - 1) % 5, 1] == PanelInfo[i, j].shape ? 1 : 0);
                Debug.LogFormat("[Button Sequences #{0}] Panel {1} Button {2} ({3} {4} {5}): Occurence #{6} {7}", moduleId, i + 1, j + 1, ColorNames[PanelInfo[i, j].color], ShapeNames[PanelInfo[i, j].shape], LabelNames[PanelInfo[i, j].text], occurences[PanelInfo[i, j].color], ActionNames[Solution[i, j]]);
            }
        }
    }

    // Button Methods
    void UpdateButtons() {
        for (int i = 0; i < 3; i++) {
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

    void LastPanel() {
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

    void NextPanel() {
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
            } else if (!correct) {
                BombModule.HandleStrike();
            } else {
                BombModule.HandlePass();
                for (int i = 0; i < 3; i++) {
                    lightState[currentPanel, i] = 2;
                }
                UpdateLights();
                foreach (var anim in ButtonAnimators) {
                    anim.Play("Solve");
                }
                DoorAnimator.Play("Solve");
                buttonsActive = false;
            }
        }
    }

    void PanelButtonRelease() {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, this.transform);
    }

    void ButtonRelease(int button) {
        ButtonAnimators[button].Play("ButtonUpAnimation");
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, this.transform);
        if (currentState[currentPanel, button] == Solution[currentPanel, button]) {
            BombModule.HandleStrike();
            lightState[currentPanel, button] = 1;
        } else {
            if (holdTimer > 40) {
                string time = BombInfo.GetFormattedTime();
                if (time.Contains(holdBombTime.ToString()) || time.Contains((holdBombTime + 5).ToString())) {
                    currentState[currentPanel, button] = 2;
                    lightState[currentPanel, button] = 2;
                } else {
                    BombModule.HandleStrike();
                    lightState[currentPanel, button] = 1;
                }
            } else if (Solution[currentPanel, button] == 1) {
                currentState[currentPanel, button] = 1;
                lightState[currentPanel, button] = 2;
            } else {
                BombModule.HandleStrike();
                lightState[currentPanel, button] = 1;
            }
        }
        UpdateLights();
        heldButton = -1;
        holdTimer = -1;
        holdBombTime = -1;
    }

    void HandlePress(int button) {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, this.transform);
        ButtonAnimators[button].Play("ButtonDownAnimation");
        heldButton = button;
        holdTimer = 0;
    }

    // Light Methods
    void UpdateLights() {
        for (int i = 0; i < 3; i++) {
            Lights[i].sharedMaterial = LightColors[lightState[currentPanel, i]];
        }
    }
}