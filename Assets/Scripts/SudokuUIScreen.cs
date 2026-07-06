using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Attach this component to an empty GameObject in a portrait Unity scene.
/// It builds a Sudoku-style mobile UI and a settings modal with light/dark modes.
/// </summary>
public sealed class SudokuUIScreen : MonoBehaviour
{
    private const string ThemePrefsKey = "SudokuUIScreen.Theme";
    private const string LanguagePrefsKey = "SudokuUIScreen.Language";
    private const string DefaultLanguagePrefsValue = "English";
    private const string ResultSoundResourcePath = "Audio/slodkabonanza-pop-sound-effect-197846";
    private const int GridSize = 9;
    private const int StageCount = 10;
    private const float MaxPhoneAspect = 9f / 16f;

    private enum ThemeMode
    {
        Light,
        Dark
    }

    private enum AppScreen
    {
        Main,
        Difficulty,
        Game
    }

    private enum SudokuDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    private enum GameLanguage
    {
        English,
        Chinese,
        Russian,
        Hindi,
        Korean,
        French
    }

    private enum ThemeRole
    {
        Background,
        TopBar,
        Surface,
        SurfaceAlt,
        Cell,
        PeerCell,
        SameNumberCell,
        SelectedCell,
        GridLine,
        ThickGridLine,
        Text,
        MutedText,
        NoteText,
        Accent,
        AccentSoft,
        Pill,
        ModalBackdrop
    }

    private struct ThemeColors
    {
        public Color Background;
        public Color TopBar;
        public Color Surface;
        public Color SurfaceAlt;
        public Color Cell;
        public Color PeerCell;
        public Color SameNumberCell;
        public Color SelectedCell;
        public Color GridLine;
        public Color ThickGridLine;
        public Color Text;
        public Color MutedText;
        public Color NoteText;
        public Color Accent;
        public Color AccentSoft;
        public Color Pill;
        public Color ModalBackdrop;
    }

    private sealed class ImageBinding
    {
        public Image Target;
        public ThemeRole Role;
        public float Alpha;
    }

    private sealed class TextBinding
    {
        public Text Target;
        public ThemeRole Role;
        public float Alpha;
    }

    private sealed class LocalizedTextBinding
    {
        public Text Target;
        public Func<string> GetValue;
    }

    private sealed class LanguageButtonBinding
    {
        public GameLanguage Language;
        public Image Background;
        public Text Label;
    }

    private sealed class CellView
    {
        public Image Background;
        public Text NumberText;
    }

    private struct Move
    {
        public int Row;
        public int Col;
        public int BeforeValue;
        public int AfterValue;
        public int BeforeNote;
        public int AfterNote;
    }

    private struct PuzzleConfig
    {
        public string Label;
        public int HintCount;
        public int[,] Puzzle;
        public int[,] Solution;
    }

    private readonly int[,] values = new int[GridSize, GridSize]
    {
        { 0, 4, 0, 5, 0, 0, 7, 9, 0 },
        { 5, 0, 2, 0, 8, 0, 0, 4, 6 },
        { 1, 0, 3, 4, 0, 6, 5, 8, 0 },
        { 0, 0, 5, 3, 0, 7, 9, 6, 0 },
        { 0, 3, 0, 0, 9, 0, 0, 5, 0 },
        { 9, 0, 4, 8, 0, 5, 2, 0, 0 },
        { 0, 2, 9, 1, 0, 8, 0, 0, 5 },
        { 7, 0, 0, 2, 5, 0, 4, 1, 9 },
        { 0, 5, 1, 0, 0, 0, 0, 2, 0 }
    };

    private readonly bool[,] locked = new bool[GridSize, GridSize]
    {
        { false, true,  false, false, false, false, true,  true,  false },
        { true,  false, true,  false, true,  false, false, false, true  },
        { true,  false, false, true,  false, true,  false, true,  false },
        { false, false, true,  true,  false, true,  true,  false, false },
        { false, true,  false, false, true,  false, false, true,  false },
        { false, false, true,  true,  false, true,  true,  false, false },
        { false, true,  false, true,  false, true,  false, false, true  },
        { true,  false, false, false, true,  false, true,  false, true  },
        { false, true,  true,  false, false, false, false, true,  false }
    };

    private readonly int[,] baseSolution = new int[GridSize, GridSize]
    {
        { 5, 3, 4, 6, 7, 8, 9, 1, 2 },
        { 6, 7, 2, 1, 9, 5, 3, 4, 8 },
        { 1, 9, 8, 3, 4, 2, 5, 6, 7 },
        { 8, 5, 9, 7, 6, 1, 4, 2, 3 },
        { 4, 2, 6, 8, 5, 3, 7, 9, 1 },
        { 7, 1, 3, 9, 2, 4, 8, 5, 6 },
        { 9, 6, 1, 5, 3, 7, 2, 8, 4 },
        { 2, 8, 7, 4, 1, 9, 6, 3, 5 },
        { 3, 4, 5, 2, 8, 6, 1, 7, 9 }
    };

    private readonly int[,] currentSolution = new int[GridSize, GridSize];
    private readonly int[,] notes = new int[GridSize, GridSize];

    private readonly List<ImageBinding> imageBindings = new List<ImageBinding>();
    private readonly List<TextBinding> textBindings = new List<TextBinding>();
    private readonly List<LocalizedTextBinding> localizedTextBindings = new List<LocalizedTextBinding>();
    private readonly List<LanguageButtonBinding> languageButtonBindings = new List<LanguageButtonBinding>();
    private readonly Stack<Move> history = new Stack<Move>();

    private ThemeMode themeMode = ThemeMode.Light;
    private GameLanguage currentLanguage = GameLanguage.English;
    private Font uiFont;
    private Sprite squareSprite;
    private Sprite roundedSprite;
    private Sprite pillSprite;
    private Sprite circleSprite;

    private RectTransform safeRoot;
    private RectTransform phoneRoot;
    private RectTransform mainScreenRoot;
    private RectTransform difficultyScreenRoot;
    private RectTransform gameScreenRoot;
    private RectTransform topBarRect;
    private RectTransform gameCardRect;
    private RectTransform statusRowRect;
    private RectTransform boardRect;
    private RectTransform controlsRect;
    private RectTransform toolsRect;
    private RectTransform numbersRect;
    private RectTransform settingsOverlay;
    private RectTransform settingsPanel;
    private RectTransform rulesOverlay;
    private RectTransform languageOverlay;
    private RectTransform clearOverlay;
    private CellView[,] cells;
    private Image lightModeButtonImage;
    private Image darkModeButtonImage;
    private Image languageButtonImage;
    private Text lightModeButtonText;
    private Text darkModeButtonText;
    private Text languageButtonText;
    private Text difficultyText;
    private Text mistakesText;
    private Text timerText;
    private Text pauseText;
    private Text notesBadgeText;
    private Text hintBadgeText;
    private Text clearMessageText;
    private Text clearBodyText;
    private Text resultPrimaryButtonText;
    private Text resultSecondaryButtonText;
    private Vector2 lastLayoutSize = new Vector2(-1f, -1f);
    private AudioSource resultAudioSource;
    private AudioClip resultSound;

    private AppScreen currentScreen = AppScreen.Main;
    private SudokuDifficulty currentDifficulty = SudokuDifficulty.Normal;
    private int currentStageIndex;
    private int selectedRow = 6;
    private int selectedCol = 2;
    private int hintRemaining = 2;
    private int mistakeCount;
    private int puzzleRunSeed = 1;
    private bool notesMode;
    private bool timerRunning;
    private bool clearInProgress;
    private bool resultIsGameOver;
    private float elapsedSeconds;
    private int lastDisplayedSecond = -1;

    private void Awake()
    {
        ConfigureMobileRuntime();

        uiFont = LoadBuiltinFont();
        squareSprite = CreateSolidSprite();
        roundedSprite = CreateRoundedSprite(48, 12);
        pillSprite = CreateRoundedSprite(64, 31);
        circleSprite = CreateCircleSprite(64);

        string savedTheme = PlayerPrefs.GetString(ThemePrefsKey, "Light");
        themeMode = savedTheme == "Dark" ? ThemeMode.Dark : ThemeMode.Light;
        currentLanguage = ParseLanguage(PlayerPrefs.GetString(LanguagePrefsKey, DefaultLanguagePrefsValue));

        EnsureEventSystem();
        SetupResultAudio();
        BuildInterface();
        LoadPuzzle(SudokuDifficulty.Normal, 0);
        ApplyTheme();
        ShowScreen(AppScreen.Main);
        UpdateTimerText(true);
    }

    private void Update()
    {
        ApplyResponsiveLayout(false);

        if (!timerRunning)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;
        UpdateTimerText(false);
    }

    private void BuildInterface()
    {
        Canvas canvas = BuildCanvas();
        safeRoot = CreateRect("Safe Area", canvas.transform);
        Stretch(safeRoot);
        safeRoot.gameObject.AddComponent<SafeAreaFitter>();

        RectTransform background = CreateRect("Background", safeRoot);
        Stretch(background);
        BindImage(AddImage(background, ThemeRole.Background, false), ThemeRole.Background);

        phoneRoot = CreateRect("Phone Viewport", safeRoot);
        Stretch(phoneRoot);

        mainScreenRoot = CreateRect("Main Screen", phoneRoot);
        Stretch(mainScreenRoot);

        difficultyScreenRoot = CreateRect("Difficulty Screen", phoneRoot);
        Stretch(difficultyScreenRoot);

        gameScreenRoot = CreateRect("Game Screen", phoneRoot);
        Stretch(gameScreenRoot);

        BuildMainScreen(mainScreenRoot);
        BuildDifficultyScreen(difficultyScreenRoot);
        BuildTopBar(gameScreenRoot);
        BuildGameCard(gameScreenRoot);
        BuildControlCard(gameScreenRoot);
        BuildSettingsModal(phoneRoot);
        BuildRulesModal(phoneRoot);
        BuildLanguageModal(phoneRoot);
        BuildClearOverlay(phoneRoot);
        Canvas.ForceUpdateCanvases();
        ApplyResponsiveLayout(true);
    }

    private Canvas BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Sudoku UI", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void BuildMainScreen(RectTransform parent)
    {
        RectTransform hero = CreateRect("Main Hero", parent);
        StretchTop(hero, 48f, 48f, 220f, 430f);

        RectTransform badge = CreateRect("Main Badge", hero);
        AnchorLeft(badge, 0f, 0f, 210f, 58f);
        BindImage(AddImage(badge, ThemeRole.Pill, true), ThemeRole.Pill);
        CreateLocalizedText(badge, 24, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Bold, delegate { return T("mainBadge"); });

        RectTransform title = CreateRect("Main Title", hero);
        StretchTop(title, 0f, 0f, 88f, 132f);
        Text titleText = CreateLocalizedText(title, 82, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Bold, delegate { return T("appTitle"); });
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 46;
        titleText.resizeTextMaxSize = 86;

        RectTransform subtitle = CreateRect("Main Subtitle", hero);
        StretchTop(subtitle, 4f, 40f, 228f, 82f);
        CreateLocalizedText(subtitle, 31, TextAnchor.MiddleLeft, ThemeRole.MutedText, FontStyle.Normal, delegate { return T("mainSubtitle"); });

        BuildMiniBoard(hero);

        RectTransform startButton = CreateLargeButton(parent, "Start Button", delegate { return T("start"); }, 760f, ThemeRole.Accent, ThemeRole.Text, delegate
        {
            ShowScreen(AppScreen.Difficulty);
        });

        RectTransform startHint = CreateRect("Start Hint", startButton);
        StretchBottom(startHint, 32f, 32f, 10f, 30f);
        CreateLocalizedText(startHint, 19, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Normal, delegate { return T("startHint"); });

        CreateLargeButton(parent, "Settings Button", delegate { return T("settings"); }, 910f, ThemeRole.SurfaceAlt, ThemeRole.Accent, ShowSettings);
        CreateLargeButton(parent, "Game Rules Button", delegate { return T("gameRules"); }, 1060f, ThemeRole.SurfaceAlt, ThemeRole.Accent, ShowRules);
    }

    private void BuildMiniBoard(RectTransform parent)
    {
        RectTransform board = CreateRect("Mini Board", parent);
        AnchorRight(board, 0f, 12f, 242f, 242f);
        BindImage(AddImage(board, ThemeRole.SurfaceAlt, true), ThemeRole.SurfaceAlt);

        int[,] preview =
        {
            { 5, 0, 9 },
            { 0, 7, 0 },
            { 1, 0, 4 }
        };

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                RectTransform tile = CreateRect("Mini Tile " + row + "-" + col, board);
                tile.anchorMin = new Vector2(col / 3f, 1f - (row + 1) / 3f);
                tile.anchorMax = new Vector2((col + 1) / 3f, 1f - row / 3f);
                tile.offsetMin = new Vector2(6f, 6f);
                tile.offsetMax = new Vector2(-6f, -6f);
                BindImage(AddImage(tile, row == col ? ThemeRole.Pill : ThemeRole.Cell, true), row == col ? ThemeRole.Pill : ThemeRole.Cell);

                int value = preview[row, col];
                if (value != 0)
                {
                    CreateText(tile, value.ToString(), 34, TextAnchor.MiddleCenter, ThemeRole.Accent, FontStyle.Bold);
                }
            }
        }
    }

    private void BuildDifficultyScreen(RectTransform parent)
    {
        RectTransform header = CreateRect("Difficulty Header", parent);
        StretchTop(header, 44f, 44f, 120f, 180f);

        RectTransform title = CreateRect("Difficulty Title", header);
        StretchTop(title, 0f, 0f, 0f, 86f);
        Text titleText = CreateLocalizedText(title, 58, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Bold, delegate { return T("selectLevel"); });
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 34;
        titleText.resizeTextMaxSize = 60;

        RectTransform backButton = CreateSmallTextButton(header, "Difficulty Back", delegate { return T("back"); }, 0f, 106f, 152f, 58f, delegate
        {
            ShowScreen(AppScreen.Main);
        });
        backButton.SetAsLastSibling();

        CreateRoundIconButton(header, "Difficulty Settings", "⚙", 38, 72f, 72f, -2f, 2f, ShowSettings);

        BuildDifficultyCard(parent, SudokuDifficulty.Easy, 350f, 55);
        BuildDifficultyCard(parent, SudokuDifficulty.Normal, 575f, 30);
        BuildDifficultyCard(parent, SudokuDifficulty.Hard, 800f, 22);
    }

    private void BuildDifficultyCard(
        RectTransform parent,
        SudokuDifficulty difficulty,
        float top,
        int clues)
    {
        RectTransform card = CreateRect(difficulty + " Card", parent);
        StretchTop(card, 54f, 54f, top, 180f);
        Image background = AddImage(card, ThemeRole.Surface, true);
        BindImage(background, ThemeRole.Surface);

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        button.onClick.AddListener(delegate { StartGame(difficulty); });

        RectTransform titleRect = CreateRect("Title", card);
        StretchTop(titleRect, 34f, 220f, 26f, 64f);
        CreateLocalizedText(titleRect, 42, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Bold, delegate { return GetDifficultyLabel(difficulty); });

        RectTransform detailRect = CreateRect("Detail", card);
        StretchTop(detailRect, 36f, 220f, 94f, 42f);
        CreateLocalizedText(detailRect, 24, TextAnchor.MiddleLeft, ThemeRole.MutedText, FontStyle.Normal, delegate { return GetDifficultyDetail(difficulty); });

        RectTransform hintPill = CreateRect("Hint Pill", card);
        AnchorRight(hintPill, 34f, 30f, 150f, 58f);
        BindImage(AddImage(hintPill, ThemeRole.Pill, true), ThemeRole.Pill);
        CreateLocalizedText(hintPill, 24, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Bold, delegate { return T("hint") + " " + GetHintCount(difficulty); });

        RectTransform cluesText = CreateRect("Clues", card);
        AnchorRight(cluesText, 36f, 102f, 170f, 42f);
        CreateLocalizedText(cluesText, 23, TextAnchor.MiddleRight, ThemeRole.Accent, FontStyle.Bold, delegate { return clues + " " + T("clues"); });
    }

    private RectTransform CreateLargeButton(
        RectTransform parent,
        string name,
        Func<string> getLabel,
        float top,
        ThemeRole backgroundRole,
        ThemeRole textRole,
        UnityEngine.Events.UnityAction onClick)
    {
        RectTransform buttonRect = CreateRect(name, parent);
        StretchTop(buttonRect, 64f, 64f, top, 122f);
        Image background = AddImage(buttonRect, backgroundRole, true);
        BindImage(background, backgroundRole);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        button.onClick.AddListener(onClick);

        RectTransform labelRect = CreateRect("Label", buttonRect);
        StretchTop(labelRect, 28f, 28f, 18f, 72f);
        Text labelText = CreateLocalizedText(labelRect, 42, TextAnchor.MiddleCenter, textRole, FontStyle.Bold, getLabel);
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 26;
        labelText.resizeTextMaxSize = 44;

        return buttonRect;
    }

    private RectTransform CreateSmallTextButton(
        RectTransform parent,
        string name,
        Func<string> getLabel,
        float left,
        float top,
        float width,
        float height,
        UnityEngine.Events.UnityAction onClick)
    {
        RectTransform buttonRect = CreateRect(name, parent);
        AnchorLeft(buttonRect, left, top, width, height);
        Image background = AddImage(buttonRect, ThemeRole.SurfaceAlt, true);
        BindImage(background, ThemeRole.SurfaceAlt);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        button.onClick.AddListener(onClick);

        CreateLocalizedText(buttonRect, 24, TextAnchor.MiddleCenter, ThemeRole.Accent, FontStyle.Bold, getLabel);
        return buttonRect;
    }

    private void BuildTopBar(RectTransform parent)
    {
        topBarRect = CreateRect("Top Bar", parent);
        StretchTop(topBarRect, 0f, 0f, 0f, 150f);
        BindImage(AddImage(topBarRect, ThemeRole.TopBar, false), ThemeRole.TopBar);

        CreateRoundIconButton(topBarRect, "Back", "<", 42, 72f, 72f, 28f, 38f, BackFromGame);

        RectTransform title = CreateRect("Screen Title", topBarRect);
        AnchorLeft(title, 118f, 34f, 330f, 74f);
        CreateLocalizedText(title, 48, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Bold, delegate { return T("appTitle"); });

        RectTransform subtitle = CreateRect("Screen Subtitle", topBarRect);
        AnchorLeft(subtitle, 122f, 96f, 330f, 40f);
        CreateLocalizedText(subtitle, 23, TextAnchor.MiddleLeft, ThemeRole.MutedText, FontStyle.Normal, delegate { return T("topSubtitle"); });

        CreateRoundIconButton(topBarRect, "Theme", "◐", 38, 78f, 78f, -178f, 38f, ShowSettings);
        CreateRoundIconButton(topBarRect, "Settings", "⚙", 42, 78f, 78f, -72f, 38f, ShowSettings);
    }

    private void BuildGameCard(RectTransform parent)
    {
        gameCardRect = CreateRect("Game Card", parent);
        StretchTop(gameCardRect, 10f, 10f, 170f, 1200f);
        BindImage(AddImage(gameCardRect, ThemeRole.Surface, true), ThemeRole.Surface);

        BuildStatusRow(gameCardRect);

        boardRect = CreateRect("Sudoku Board", gameCardRect);
        AnchorCenterTop(boardRect, 0f, 136f, 1036f, 1036f);
        BindImage(AddImage(boardRect, ThemeRole.Cell, false), ThemeRole.Cell);

        BuildBoardCells(boardRect);
        BuildBoardLines(boardRect);
    }

    private void BuildStatusRow(RectTransform parent)
    {
        statusRowRect = CreateRect("Status Row", parent);
        StretchTop(statusRowRect, 20f, 20f, 16f, 92f);

        RectTransform difficulty = CreateRect("Difficulty Pill", statusRowRect);
        AnchorLeft(difficulty, 0f, 16f, 238f, 68f);
        BindImage(AddImage(difficulty, ThemeRole.Pill, true), ThemeRole.Pill);
        difficultyText = CreateText(difficulty, GetDifficultyLabel(currentDifficulty), 34, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Normal);
        difficultyText.resizeTextForBestFit = true;
        difficultyText.resizeTextMinSize = 18;
        difficultyText.resizeTextMaxSize = 34;

        RectTransform mistakes = CreateRect("Mistakes", statusRowRect);
        AnchorLeft(mistakes, 258f, 16f, 328f, 68f);
        mistakesText = CreateText(mistakes, T("mistakes") + ": 0/3", 30, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Normal);
        mistakesText.resizeTextForBestFit = true;
        mistakesText.resizeTextMinSize = 16;
        mistakesText.resizeTextMaxSize = 30;

        RectTransform pause = CreateIconButton(statusRowRect, "Pause", "II", 42, 88f, 72f, -34f, 14f, TogglePause);
        pauseText = pause.GetComponentInChildren<Text>();

        RectTransform timer = CreateRect("Timer", statusRowRect);
        AnchorRight(timer, 132f, 15f, 190f, 70f);
        timerText = CreateText(timer, "01:40", 34, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Normal);
        timerText.resizeTextForBestFit = true;
        timerText.resizeTextMinSize = 20;
        timerText.resizeTextMaxSize = 34;
    }

    private void BuildBoardCells(RectTransform board)
    {
        cells = new CellView[GridSize, GridSize];

        RectTransform cellLayer = CreateRect("Cells", board);
        Stretch(cellLayer);

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                int capturedRow = row;
                int capturedCol = col;

                RectTransform cell = CreateRect("Cell " + row + "-" + col, cellLayer);
                AnchorGridCell(cell, row, col);

                Image background = AddImage(cell, ThemeRole.Cell, false);
                Button button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                ConfigureButton(button);
                button.onClick.AddListener(delegate { SelectCell(capturedRow, capturedCol); });

                Text number = CreateText(cell, string.Empty, 66, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Normal);
                RectTransform numberRect = number.rectTransform;
                numberRect.offsetMin = new Vector2(4f, -2f);
                numberRect.offsetMax = new Vector2(-4f, -2f);
                number.alignByGeometry = true;
                number.lineSpacing = 0.86f;
                number.resizeTextForBestFit = true;
                number.resizeTextMinSize = 30;
                number.resizeTextMaxSize = 66;

                cells[row, col] = new CellView
                {
                    Background = background,
                    NumberText = number
                };
            }
        }
    }

    private void BuildBoardLines(RectTransform board)
    {
        RectTransform lineLayer = CreateRect("Grid Lines", board);
        Stretch(lineLayer);

        for (int i = 0; i <= GridSize; i++)
        {
            bool thick = i % 3 == 0;
            float width = thick ? 8f : 3.5f;
            ThemeRole role = thick ? ThemeRole.ThickGridLine : ThemeRole.GridLine;
            float p = i / 9f;

            RectTransform vertical = CreateRect("Vertical Line " + i, lineLayer);
            vertical.anchorMin = new Vector2(p, 0f);
            vertical.anchorMax = new Vector2(p, 1f);
            vertical.pivot = new Vector2(0.5f, 0.5f);
            vertical.sizeDelta = new Vector2(width, 0f);
            vertical.anchoredPosition = Vector2.zero;
            Image verticalLine = AddImage(vertical, role, false);
            verticalLine.raycastTarget = false;
            BindImage(verticalLine, role);

            RectTransform horizontal = CreateRect("Horizontal Line " + i, lineLayer);
            horizontal.anchorMin = new Vector2(0f, 1f - p);
            horizontal.anchorMax = new Vector2(1f, 1f - p);
            horizontal.pivot = new Vector2(0.5f, 0.5f);
            horizontal.sizeDelta = new Vector2(0f, width);
            horizontal.anchoredPosition = Vector2.zero;
            Image horizontalLine = AddImage(horizontal, role, false);
            horizontalLine.raycastTarget = false;
            BindImage(horizontalLine, role);
        }
    }

    private void BuildControlCard(RectTransform parent)
    {
        controlsRect = CreateRect("Control Card", parent);
        StretchBottom(controlsRect, 10f, 10f, 24f, 470f);
        BindImage(AddImage(controlsRect, ThemeRole.Surface, true), ThemeRole.Surface);

        toolsRect = CreateRect("Tools", controlsRect);
        StretchTop(toolsRect, 34f, 34f, 34f, 160f);
        BuildToolButton(toolsRect, 0, "↺", "undo", UndoMove);
        BuildToolButton(toolsRect, 1, "⌫", "erase", EraseSelected);
        BuildToolButton(toolsRect, 2, "✎", "notes", ToggleNotes);
        BuildToolButton(toolsRect, 3, "!", "hint", UseHint);

        numbersRect = CreateRect("Number Pad", controlsRect);
        StretchBottom(numbersRect, 28f, 28f, 32f, 150f);

        for (int i = 1; i <= 9; i++)
        {
            int capturedNumber = i;
            RectTransform numberButton = CreateRect("Number " + i, numbersRect);
            float minX = (i - 1) / 9f;
            float maxX = i / 9f;
            numberButton.anchorMin = new Vector2(minX, 0f);
            numberButton.anchorMax = new Vector2(maxX, 1f);
            numberButton.offsetMin = Vector2.zero;
            numberButton.offsetMax = Vector2.zero;

            Image hitArea = AddImage(numberButton, ThemeRole.Surface, false);
            hitArea.color = new Color(1f, 1f, 1f, 0f);

            Button button = numberButton.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;
            ConfigureButton(button);
            button.onClick.AddListener(delegate { EnterNumber(capturedNumber); });

            Text numberText = CreateText(numberButton, i.ToString(), 78, TextAnchor.MiddleCenter, ThemeRole.Accent, FontStyle.Normal);
            numberText.alignByGeometry = true;
            numberText.lineSpacing = 0.86f;
            numberText.resizeTextForBestFit = true;
            numberText.resizeTextMinSize = 44;
            numberText.resizeTextMaxSize = 84;
        }
    }

    private void BuildToolButton(RectTransform parent, int index, string icon, string labelKey, UnityEngine.Events.UnityAction action)
    {
        RectTransform slot = CreateRect(labelKey + " Button", parent);
        float minX = index / 4f;
        float maxX = (index + 1) / 4f;
        slot.anchorMin = new Vector2(minX, 0f);
        slot.anchorMax = new Vector2(maxX, 1f);
        slot.offsetMin = new Vector2(4f, 0f);
        slot.offsetMax = new Vector2(-4f, 0f);

        Image hitArea = AddImage(slot, ThemeRole.Surface, false);
        hitArea.color = new Color(1f, 1f, 1f, 0f);

        Button button = slot.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        ConfigureButton(button);
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        RectTransform iconRect = CreateRect("Icon", slot);
        StretchTop(iconRect, 0f, 0f, 4f, 84f);
        if (labelKey == "erase")
        {
            BuildEraserGlyph(iconRect);
        }
        else
        {
            Text iconText = CreateText(iconRect, icon, 66, TextAnchor.MiddleCenter, ThemeRole.Accent, FontStyle.Normal);
            iconText.resizeTextForBestFit = true;
            iconText.resizeTextMinSize = 28;
            iconText.resizeTextMaxSize = 70;
        }

        RectTransform labelRect = CreateRect("Label", slot);
        StretchBottom(labelRect, 0f, 0f, 0f, 62f);
        Text labelText = CreateLocalizedText(labelRect, 23, TextAnchor.MiddleCenter, ThemeRole.Accent, FontStyle.Normal, delegate { return T(labelKey); });
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 12;
        labelText.resizeTextMaxSize = 23;

        if (labelKey == "notes")
        {
            RectTransform badge = CreateRect("Notes Badge", slot);
            AnchorCenterTop(badge, 58f, 8f, 88f, 48f);
            BindImage(AddImage(badge, ThemeRole.AccentSoft, true), ThemeRole.AccentSoft);
            notesBadgeText = CreateText(badge, GetToggleLabel(false), 23, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Normal);
            notesBadgeText.resizeTextForBestFit = true;
            notesBadgeText.resizeTextMinSize = 14;
            notesBadgeText.resizeTextMaxSize = 23;
        }

        if (labelKey == "hint")
        {
            RectTransform badge = CreateRect("Hint Badge", slot);
            AnchorCenterTop(badge, 58f, 8f, 48f, 48f);
            BindImage(AddImage(badge, ThemeRole.Accent, true), ThemeRole.Accent);
            hintBadgeText = CreateText(badge, hintRemaining.ToString(), 23, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Bold);
            hintBadgeText.resizeTextForBestFit = true;
            hintBadgeText.resizeTextMinSize = 14;
            hintBadgeText.resizeTextMaxSize = 24;
        }
    }

    private void BuildEraserGlyph(RectTransform parent)
    {
        RectTransform body = CreateRect("Eraser Body", parent);
        AnchorCenter(body, 66f, 38f);
        body.anchoredPosition = new Vector2(-4f, 6f);
        body.localEulerAngles = new Vector3(0f, 0f, -28f);
        BindImage(AddImage(body, ThemeRole.Accent, true), ThemeRole.Accent);

        RectTransform cut = CreateRect("Eraser Cut", body);
        AnchorLeft(cut, 42f, 0f, 6f, 38f);
        BindImage(AddImage(cut, ThemeRole.Surface, false), ThemeRole.Surface);

        RectTransform underline = CreateRect("Eraser Underline", parent);
        AnchorCenter(underline, 82f, 6f);
        underline.anchoredPosition = new Vector2(8f, -30f);
        BindImage(AddImage(underline, ThemeRole.Accent, true), ThemeRole.Accent);
    }

    private void BuildSettingsModal(RectTransform parent)
    {
        settingsOverlay = CreateRect("Settings Overlay", parent);
        Stretch(settingsOverlay);
        Image backdrop = AddImage(settingsOverlay, ThemeRole.ModalBackdrop, false);
        BindImage(backdrop, ThemeRole.ModalBackdrop, 0.62f);

        Button backdropButton = settingsOverlay.gameObject.AddComponent<Button>();
        backdropButton.targetGraphic = backdrop;
        ConfigureButton(backdropButton);
        backdropButton.onClick.AddListener(HideSettings);

        settingsPanel = CreateRect("Settings Panel", settingsOverlay);
        AnchorCenter(settingsPanel, 760f, 780f);
        BindImage(AddImage(settingsPanel, ThemeRole.Surface, true), ThemeRole.Surface);

        RectTransform title = CreateRect("Title", settingsPanel);
        StretchTop(title, 52f, 112f, 32f, 72f);
        CreateLocalizedText(title, 42, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Bold, delegate { return T("settings"); });

        CreateIconButton(settingsPanel, "Close", "X", 34, 82f, 72f, -38f, 32f, HideSettings);

        RectTransform subtitle = CreateRect("Subtitle", settingsPanel);
        StretchTop(subtitle, 52f, 52f, 112f, 66f);
        CreateLocalizedText(subtitle, 27, TextAnchor.MiddleLeft, ThemeRole.MutedText, FontStyle.Normal, delegate { return T("settingsSubtitle"); });

        RectTransform lightButton = CreateModeButton(settingsPanel, "White Mode", 52f, delegate { SetTheme(ThemeMode.Light); });
        lightModeButtonImage = lightButton.GetComponent<Image>();
        lightModeButtonText = lightButton.GetComponentInChildren<Text>();

        RectTransform darkButton = CreateModeButton(settingsPanel, "Dark Mode", 182f, delegate { SetTheme(ThemeMode.Dark); });
        darkModeButtonImage = darkButton.GetComponent<Image>();
        darkModeButtonText = darkButton.GetComponentInChildren<Text>();

        RectTransform languageButton = CreateModeButton(settingsPanel, "Language", 312f, ShowLanguageSelector);
        languageButtonImage = languageButton.GetComponent<Image>();
        languageButtonText = languageButton.GetComponentInChildren<Text>();

        RectTransform footer = CreateRect("Footer", settingsPanel);
        StretchBottom(footer, 52f, 52f, 34f, 74f);
        Text footerText = CreateLocalizedText(footer, 24, TextAnchor.MiddleCenter, ThemeRole.MutedText, FontStyle.Normal, delegate { return T("settingsFooter"); });
        footerText.resizeTextForBestFit = true;
        footerText.resizeTextMinSize = 16;
        footerText.resizeTextMaxSize = 24;

        settingsOverlay.gameObject.SetActive(false);
    }

    private void BuildRulesModal(RectTransform parent)
    {
        rulesOverlay = CreateRect("Game Rules Overlay", parent);
        Stretch(rulesOverlay);
        Image backdrop = AddImage(rulesOverlay, ThemeRole.ModalBackdrop, false);
        BindImage(backdrop, ThemeRole.ModalBackdrop, 0.62f);

        Button backdropButton = rulesOverlay.gameObject.AddComponent<Button>();
        backdropButton.targetGraphic = backdrop;
        ConfigureButton(backdropButton);
        backdropButton.onClick.AddListener(HideRules);

        RectTransform panel = CreateRect("Game Rules Panel", rulesOverlay);
        AnchorCenter(panel, 860f, 1260f);
        BindImage(AddImage(panel, ThemeRole.Surface, true), ThemeRole.Surface);

        RectTransform title = CreateRect("Game Rules Title", panel);
        StretchTop(title, 52f, 112f, 34f, 72f);
        Text titleText = CreateLocalizedText(title, 42, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Bold, delegate { return T("gameRules"); });
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 26;
        titleText.resizeTextMaxSize = 44;

        CreateIconButton(panel, "Game Rules Close", "X", 34, 82f, 72f, -38f, 32f, HideRules);

        RectTransform body = CreateRect("Game Rules Body", panel);
        StretchTop(body, 54f, 54f, 132f, 930f);
        Text bodyText = CreateLocalizedText(body, 25, TextAnchor.UpperLeft, ThemeRole.Text, FontStyle.Normal, delegate { return T("rulesBody"); });
        bodyText.resizeTextForBestFit = true;
        bodyText.resizeTextMinSize = 16;
        bodyText.resizeTextMaxSize = 25;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;

        CreateModalButton(panel, "Rules Close Button", delegate { return T("close"); }, 1120f, HideRules);
        rulesOverlay.gameObject.SetActive(false);
    }

    private void BuildLanguageModal(RectTransform parent)
    {
        languageOverlay = CreateRect("Language Overlay", parent);
        Stretch(languageOverlay);
        Image backdrop = AddImage(languageOverlay, ThemeRole.ModalBackdrop, false);
        BindImage(backdrop, ThemeRole.ModalBackdrop, 0.62f);

        Button backdropButton = languageOverlay.gameObject.AddComponent<Button>();
        backdropButton.targetGraphic = backdrop;
        ConfigureButton(backdropButton);
        backdropButton.onClick.AddListener(HideLanguageSelector);

        RectTransform panel = CreateRect("Language Panel", languageOverlay);
        AnchorCenter(panel, 760f, 800f);
        BindImage(AddImage(panel, ThemeRole.Surface, true), ThemeRole.Surface);

        RectTransform title = CreateRect("Language Title", panel);
        StretchTop(title, 52f, 112f, 32f, 64f);
        Text titleText = CreateLocalizedText(title, 40, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Bold, delegate { return T("language"); });
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 24;
        titleText.resizeTextMaxSize = 42;

        CreateIconButton(panel, "Language Close", "X", 34, 82f, 72f, -38f, 30f, HideLanguageSelector);

        RectTransform subtitle = CreateRect("Language Subtitle", panel);
        StretchTop(subtitle, 52f, 52f, 104f, 58f);
        CreateLocalizedText(subtitle, 25, TextAnchor.MiddleLeft, ThemeRole.MutedText, FontStyle.Normal, delegate { return T("languageSubtitle"); });

        CreateLanguageChoiceButton(panel, GameLanguage.English, 176f);
        CreateLanguageChoiceButton(panel, GameLanguage.Chinese, 264f);
        CreateLanguageChoiceButton(panel, GameLanguage.Russian, 352f);
        CreateLanguageChoiceButton(panel, GameLanguage.Hindi, 440f);
        CreateLanguageChoiceButton(panel, GameLanguage.Korean, 528f);
        CreateLanguageChoiceButton(panel, GameLanguage.French, 616f);

        languageOverlay.gameObject.SetActive(false);
    }

    private RectTransform CreateModalButton(RectTransform parent, string name, Func<string> getLabel, float top, UnityAction onClick)
    {
        RectTransform buttonRect = CreateRect(name, parent);
        StretchTop(buttonRect, 54f, 54f, top, 86f);
        Image background = AddImage(buttonRect, ThemeRole.Accent, true);
        BindImage(background, ThemeRole.Accent);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        button.onClick.AddListener(onClick);

        Text text = CreateLocalizedText(buttonRect, 30, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Bold, getLabel);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = 32;
        return buttonRect;
    }

    private void CreateLanguageChoiceButton(RectTransform parent, GameLanguage language, float top)
    {
        RectTransform buttonRect = CreateRect(language + " Button", parent);
        StretchTop(buttonRect, 52f, 52f, top, 76f);
        Image background = AddImage(buttonRect, ThemeRole.SurfaceAlt, true);
        BindImage(background, ThemeRole.SurfaceAlt);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        button.onClick.AddListener(delegate { SetLanguage(language); });

        RectTransform textRect = CreateRect("Text", buttonRect);
        Stretch(textRect, 34f, 34f, 0f, 0f);
        Text text = CreateText(textRect, GetLanguageOptionLabel(language), 28, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Normal);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = 30;

        languageButtonBindings.Add(new LanguageButtonBinding
        {
            Language = language,
            Background = background,
            Label = text
        });
    }

    private void BuildClearOverlay(RectTransform parent)
    {
        clearOverlay = CreateRect("Clear Overlay", parent);
        Stretch(clearOverlay);
        Image backdrop = AddImage(clearOverlay, ThemeRole.ModalBackdrop, false);
        BindImage(backdrop, ThemeRole.ModalBackdrop, 0.42f);

        RectTransform panel = CreateRect("Result Panel", clearOverlay);
        AnchorCenter(panel, 720f, 440f);
        BindImage(AddImage(panel, ThemeRole.Surface, true), ThemeRole.Surface);

        RectTransform title = CreateRect("Result Title", panel);
        StretchTop(title, 50f, 50f, 52f, 104f);
        clearMessageText = CreateText(title, T("roundClear"), 54, TextAnchor.MiddleCenter, ThemeRole.Text, FontStyle.Bold);
        clearMessageText.resizeTextForBestFit = true;
        clearMessageText.resizeTextMinSize = 30;
        clearMessageText.resizeTextMaxSize = 58;

        RectTransform body = CreateRect("Result Body", panel);
        StretchTop(body, 60f, 60f, 172f, 82f);
        clearBodyText = CreateText(body, string.Empty, 29, TextAnchor.MiddleCenter, ThemeRole.MutedText, FontStyle.Normal);
        clearBodyText.resizeTextForBestFit = true;
        clearBodyText.resizeTextMinSize = 18;
        clearBodyText.resizeTextMaxSize = 31;

        RectTransform primary = CreateResultButton(panel, "Result Main Button", "Main Screen", 54f, 304f, ThemeRole.SurfaceAlt, ThemeRole.Accent, GoToMainFromResult);
        resultPrimaryButtonText = primary.GetComponentInChildren<Text>();

        RectTransform secondary = CreateResultButton(panel, "Result Action Button", "Next Round", 378f, 304f, ThemeRole.Accent, ThemeRole.Text, NextRoundFromResult);
        resultSecondaryButtonText = secondary.GetComponentInChildren<Text>();

        clearOverlay.gameObject.SetActive(false);
    }

    private RectTransform CreateResultButton(
        RectTransform parent,
        string name,
        string label,
        float left,
        float top,
        ThemeRole backgroundRole,
        ThemeRole textRole,
        UnityAction onClick)
    {
        RectTransform buttonRect = CreateRect(name, parent);
        AnchorLeft(buttonRect, left, top, 288f, 82f);
        Image background = AddImage(buttonRect, backgroundRole, true);
        BindImage(background, backgroundRole);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        button.onClick.AddListener(onClick);

        Text text = CreateText(buttonRect, label, 30, TextAnchor.MiddleCenter, textRole, FontStyle.Bold);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = 32;

        return buttonRect;
    }

    private void ApplyResponsiveLayout(bool force)
    {
        if (safeRoot == null || phoneRoot == null || topBarRect == null || gameCardRect == null || boardRect == null || controlsRect == null)
        {
            return;
        }

        Vector2 safeSize = safeRoot.rect.size;
        if (safeSize.x < 100f || safeSize.y < 100f)
        {
            return;
        }

        if (!force && Mathf.Abs(safeSize.x - lastLayoutSize.x) < 0.5f && Mathf.Abs(safeSize.y - lastLayoutSize.y) < 0.5f)
        {
            return;
        }

        lastLayoutSize = safeSize;

        Vector2 size = ApplyPhoneViewport(safeSize);
        float margin = 10f;
        float gap = 18f;
        float inner = 16f;
        float bottomMargin = 22f;

        float topHeight = Mathf.Clamp(size.y * 0.074f, 118f, 146f);
        float controlsHeight = Mathf.Clamp(size.y * 0.245f, 390f, 470f);
        float statusHeight = Mathf.Clamp(size.y * 0.048f, 78f, 92f);

        float availableBoardHeight = size.y - topHeight - controlsHeight - bottomMargin - (gap * 2f) - statusHeight - (inner * 3f);
        if (availableBoardHeight < 430f)
        {
            topHeight = Mathf.Clamp(size.y * 0.066f, 96f, 126f);
            controlsHeight = Mathf.Clamp(size.y * 0.218f, 300f, 405f);
            statusHeight = Mathf.Clamp(size.y * 0.044f, 64f, 84f);
            gap = 10f;
            inner = 9f;
            bottomMargin = 12f;
            availableBoardHeight = size.y - topHeight - controlsHeight - bottomMargin - (gap * 2f) - statusHeight - (inner * 3f);
        }

        float maxBoardWidth = Mathf.Max(320f, size.x - (margin * 2f) - (inner * 1.5f));
        float boardSize = Mathf.Min(maxBoardWidth, availableBoardHeight);
        boardSize = Mathf.Clamp(boardSize, 300f, maxBoardWidth);

        float gameTop = topHeight + gap;
        float gameHeight = statusHeight + boardSize + (inner * 3f);
        float maxGameHeight = size.y - gameTop - controlsHeight - bottomMargin - gap;
        if (gameHeight > maxGameHeight)
        {
            boardSize = Mathf.Max(300f, maxGameHeight - statusHeight - (inner * 3f));
            gameHeight = statusHeight + boardSize + (inner * 3f);
        }

        StretchTop(topBarRect, 0f, 0f, 0f, topHeight);
        StretchTop(gameCardRect, margin, margin, gameTop, gameHeight);
        StretchTop(statusRowRect, inner, inner, inner * 0.8f, statusHeight);
        AnchorCenterTop(boardRect, 0f, statusHeight + (inner * 1.8f), boardSize, boardSize);
        StretchBottom(controlsRect, margin, margin, bottomMargin, controlsHeight);

        if (toolsRect != null)
        {
            float toolTop = Mathf.Clamp(controlsHeight * 0.08f, 20f, 38f);
            float toolHeight = Mathf.Clamp(controlsHeight * 0.38f, 116f, 164f);
            StretchTop(toolsRect, 34f, 34f, toolTop, toolHeight);
        }

        if (numbersRect != null)
        {
            float numberBottom = Mathf.Clamp(controlsHeight * 0.07f, 20f, 36f);
            float numberHeight = Mathf.Clamp(controlsHeight * 0.32f, 106f, 150f);
            StretchBottom(numbersRect, 28f, 28f, numberBottom, numberHeight);
        }
    }

    private Vector2 ApplyPhoneViewport(Vector2 safeSize)
    {
        float safeAspect = safeSize.x / safeSize.y;
        if (safeAspect > MaxPhoneAspect)
        {
            float phoneHeight = safeSize.y;
            float phoneWidth = phoneHeight * MaxPhoneAspect;

            phoneRoot.anchorMin = new Vector2(0.5f, 0.5f);
            phoneRoot.anchorMax = new Vector2(0.5f, 0.5f);
            phoneRoot.pivot = new Vector2(0.5f, 0.5f);
            phoneRoot.sizeDelta = new Vector2(phoneWidth, phoneHeight);
            phoneRoot.anchoredPosition = Vector2.zero;
            return new Vector2(phoneWidth, phoneHeight);
        }

        Stretch(phoneRoot);
        phoneRoot.pivot = new Vector2(0.5f, 0.5f);
        return safeSize;
    }

    private RectTransform CreateModeButton(RectTransform parent, string label, float top, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform buttonRect = CreateRect(label, parent);
        StretchTop(buttonRect, 52f, 52f, 190f + top, 94f);
        Image background = AddImage(buttonRect, ThemeRole.SurfaceAlt, true);
        BindImage(background, ThemeRole.SurfaceAlt);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        button.onClick.AddListener(onClick);

        RectTransform textRect = CreateRect("Text", buttonRect);
        Stretch(textRect, 34f, 34f, 0f, 0f);
        Text text = CreateText(textRect, label, 31, TextAnchor.MiddleLeft, ThemeRole.Text, FontStyle.Normal);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = 32;

        return buttonRect;
    }

    private RectTransform CreateIconButton(
        RectTransform parent,
        string name,
        string text,
        int fontSize,
        float width,
        float height,
        float x,
        float y,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform buttonRect = CreateRect(name + " Button", parent);

        if (x >= 0f)
        {
            AnchorLeft(buttonRect, x, y, width, height);
        }
        else
        {
            AnchorRight(buttonRect, -x, y, width, height);
        }

        Image hitArea = AddImage(buttonRect, ThemeRole.Surface, false);
        hitArea.color = new Color(1f, 1f, 1f, 0f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        ConfigureButton(button);
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        Text icon = CreateText(buttonRect, text, fontSize, TextAnchor.MiddleCenter, ThemeRole.Accent, FontStyle.Bold);
        icon.resizeTextForBestFit = true;
        icon.resizeTextMinSize = 16;
        icon.resizeTextMaxSize = fontSize;
        icon.lineSpacing = 0.74f;

        return buttonRect;
    }

    private RectTransform CreateRoundIconButton(
        RectTransform parent,
        string name,
        string text,
        int fontSize,
        float width,
        float height,
        float x,
        float y,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform buttonRect = CreateRect(name + " Button", parent);

        if (x >= 0f)
        {
            AnchorLeft(buttonRect, x, y, width, height);
        }
        else
        {
            AnchorRight(buttonRect, -x, y, width, height);
        }

        Image background = buttonRect.gameObject.AddComponent<Image>();
        background.sprite = circleSprite;
        background.type = Image.Type.Simple;
        background.raycastTarget = true;
        BindImage(background, ThemeRole.SurfaceAlt);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ConfigureButton(button);
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        Text icon = CreateText(buttonRect, text, fontSize, TextAnchor.MiddleCenter, ThemeRole.Accent, FontStyle.Bold);
        icon.resizeTextForBestFit = true;
        icon.resizeTextMinSize = 16;
        icon.resizeTextMaxSize = fontSize;

        return buttonRect;
    }

    private void SelectCell(int row, int col)
    {
        selectedRow = row;
        selectedCol = col;
        RefreshBoard();
    }

    private void ShowScreen(AppScreen screen)
    {
        currentScreen = screen;

        if (mainScreenRoot != null)
        {
            mainScreenRoot.gameObject.SetActive(screen == AppScreen.Main);
        }

        if (difficultyScreenRoot != null)
        {
            difficultyScreenRoot.gameObject.SetActive(screen == AppScreen.Difficulty);
        }

        if (gameScreenRoot != null)
        {
            gameScreenRoot.gameObject.SetActive(screen == AppScreen.Game);
        }

        if (screen != AppScreen.Game)
        {
            HideClearOverlay();
        }

        HideRules();
        HideLanguageSelector();

        timerRunning = screen == AppScreen.Game;
        ApplyTheme();
        RefreshBoard();
    }

    private void StartGame(SudokuDifficulty difficulty)
    {
        currentStageIndex = 0;
        puzzleRunSeed = CreatePuzzleRunSeed(difficulty);
        StartStage(difficulty, currentStageIndex, true);
        ShowScreen(AppScreen.Game);
    }

    private void StartStage(SudokuDifficulty difficulty, int stageIndex, bool resetTimer)
    {
        SetClearOverlayVisible(false);
        clearInProgress = false;
        resultIsGameOver = false;
        LoadPuzzle(difficulty, stageIndex);

        if (resetTimer)
        {
            elapsedSeconds = 0f;
            lastDisplayedSecond = -1;
        }

        notesMode = false;
        if (notesBadgeText != null)
        {
            notesBadgeText.text = GetToggleLabel(false);
        }

        UpdateTimerText(true);
    }

    private void LoadPuzzle(SudokuDifficulty difficulty, int stageIndex)
    {
        currentDifficulty = difficulty;
        currentStageIndex = Mathf.Clamp(stageIndex, 0, StageCount - 1);

        PuzzleConfig config = GetPuzzleConfig(difficulty, currentStageIndex);
        hintRemaining = config.HintCount;
        mistakeCount = 0;
        history.Clear();

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                int value = config.Puzzle[row, col];
                values[row, col] = value;
                notes[row, col] = 0;
                locked[row, col] = value != 0;
                currentSolution[row, col] = config.Solution[row, col];
            }
        }

        selectedRow = FindFirstOpenRow();
        selectedCol = FindFirstOpenCol(selectedRow);
        RefreshDifficultyLabels();
        RefreshMistakesLabel();
        RefreshBoard();
    }

    private int FindFirstOpenRow()
    {
        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                if (!locked[row, col])
                {
                    return row;
                }
            }
        }

        return 0;
    }

    private int FindFirstOpenCol(int row)
    {
        for (int col = 0; col < GridSize; col++)
        {
            if (!locked[row, col])
            {
                return col;
            }
        }

        return 0;
    }

    private void RefreshDifficultyLabels()
    {
        if (difficultyText != null)
        {
            difficultyText.text = GetDifficultyLabel(currentDifficulty) + " " + (currentStageIndex + 1) + "/" + StageCount;
        }

        if (hintBadgeText != null)
        {
            hintBadgeText.text = hintRemaining.ToString();
        }
    }

    private void RefreshMistakesLabel()
    {
        if (mistakesText != null)
        {
            mistakesText.text = T("mistakes") + ": " + mistakeCount + "/" + GetMistakeLimit(currentDifficulty);
        }
    }

    private void UseHint()
    {
        if (currentScreen != AppScreen.Game || hintRemaining <= 0 || clearInProgress)
        {
            return;
        }

        if (!TryRevealHintCell())
        {
            return;
        }

        hintRemaining--;
        RefreshDifficultyLabels();
        RefreshBoard();
        CheckForClear();
    }

    private void BackFromGame()
    {
        ShowScreen(AppScreen.Difficulty);
    }

    private bool TryRevealHintCell()
    {
        for (int pass = 0; pass < 2; pass++)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    bool empty = values[row, col] == 0;
                    bool wrong = values[row, col] != 0 && values[row, col] != currentSolution[row, col];
                    if ((pass == 0 && empty) || (pass == 1 && wrong))
                    {
                        values[row, col] = currentSolution[row, col];
                        notes[row, col] = 0;
                        locked[row, col] = true;
                        selectedRow = row;
                        selectedCol = col;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void CheckForClear()
    {
        if (clearInProgress)
        {
            return;
        }

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                if (values[row, col] != currentSolution[row, col])
                {
                    return;
                }
            }
        }

        clearInProgress = true;
        timerRunning = false;
        ShowRoundClearOverlay();
    }

    private void ShowRoundClearOverlay()
    {
        string label = GetDifficultyLabel(currentDifficulty);
        resultIsGameOver = false;
        ShowResultOverlay(
            T("roundClear"),
            label + " " + T("stage") + " " + (currentStageIndex + 1) + "/" + StageCount + " " + T("completed"),
            T("mainScreen"),
            T("nextRound"));
    }

    private void ShowGameOverOverlay()
    {
        string label = GetDifficultyLabel(currentDifficulty);
        resultIsGameOver = true;
        ShowResultOverlay(
            T("gameOver"),
            label + " " + T("mistakesFull"),
            T("mainScreen"),
            T("retry"));
    }

    private void ShowResultOverlay(string title, string body, string primaryLabel, string secondaryLabel)
    {
        if (clearOverlay == null)
        {
            return;
        }

        if (clearMessageText != null)
        {
            clearMessageText.text = title;
        }

        if (clearBodyText != null)
        {
            clearBodyText.text = body;
        }

        if (resultPrimaryButtonText != null)
        {
            resultPrimaryButtonText.text = primaryLabel;
        }

        if (resultSecondaryButtonText != null)
        {
            resultSecondaryButtonText.text = secondaryLabel;
        }

        PlayResultSound();
        clearOverlay.gameObject.SetActive(true);
        clearOverlay.SetAsLastSibling();
    }

    private void SetClearOverlayVisible(bool visible)
    {
        if (clearOverlay != null)
        {
            clearOverlay.gameObject.SetActive(visible);
        }
    }

    private void HideClearOverlay()
    {
        SetClearOverlayVisible(false);
    }

    private void GoToMainFromResult()
    {
        HideClearOverlay();
        clearInProgress = false;
        resultIsGameOver = false;
        ShowScreen(AppScreen.Main);
    }

    private void NextRoundFromResult()
    {
        HideClearOverlay();

        if (resultIsGameOver)
        {
            puzzleRunSeed = CreatePuzzleRunSeed(currentDifficulty);
            StartStage(currentDifficulty, currentStageIndex, true);
            ShowScreen(AppScreen.Game);
            return;
        }

        int nextStage = currentStageIndex + 1;
        if (nextStage >= StageCount)
        {
            nextStage = 0;
        }

        puzzleRunSeed = CreatePuzzleRunSeed(currentDifficulty);
        StartStage(currentDifficulty, nextStage, true);
        ShowScreen(AppScreen.Game);
    }

    private void EnterNumber(int number)
    {
        if (locked[selectedRow, selectedCol] || clearInProgress)
        {
            return;
        }

        if (notesMode)
        {
            EnterNote(number);
            return;
        }

        int previousValue = values[selectedRow, selectedCol];
        int previousNote = notes[selectedRow, selectedCol];
        if (previousValue == number && previousNote == 0)
        {
            return;
        }

        values[selectedRow, selectedCol] = number;
        notes[selectedRow, selectedCol] = 0;
        history.Push(new Move
        {
            Row = selectedRow,
            Col = selectedCol,
            BeforeValue = previousValue,
            AfterValue = number,
            BeforeNote = previousNote,
            AfterNote = 0
        });

        if (number != currentSolution[selectedRow, selectedCol])
        {
            mistakeCount = Mathf.Min(mistakeCount + 1, GetMistakeLimit(currentDifficulty));
            RefreshMistakesLabel();
            if (mistakeCount >= GetMistakeLimit(currentDifficulty))
            {
                clearInProgress = true;
                timerRunning = false;
                RefreshBoard();
                ShowGameOverOverlay();
                return;
            }
        }

        RefreshBoard();
        CheckForClear();
    }

    private void EnterNote(int number)
    {
        int previousValue = values[selectedRow, selectedCol];
        int previousNote = notes[selectedRow, selectedCol];
        if (previousValue == 0 && previousNote == number)
        {
            return;
        }

        values[selectedRow, selectedCol] = 0;
        notes[selectedRow, selectedCol] = number;
        history.Push(new Move
        {
            Row = selectedRow,
            Col = selectedCol,
            BeforeValue = previousValue,
            AfterValue = 0,
            BeforeNote = previousNote,
            AfterNote = number
        });

        RefreshBoard();
    }

    private void EraseSelected()
    {
        if (locked[selectedRow, selectedCol] || clearInProgress)
        {
            return;
        }

        int previousValue = values[selectedRow, selectedCol];
        int previousNote = notes[selectedRow, selectedCol];
        if (previousValue == 0 && previousNote == 0)
        {
            return;
        }

        values[selectedRow, selectedCol] = 0;
        notes[selectedRow, selectedCol] = 0;
        history.Push(new Move
        {
            Row = selectedRow,
            Col = selectedCol,
            BeforeValue = previousValue,
            AfterValue = 0,
            BeforeNote = previousNote,
            AfterNote = 0
        });
        RefreshBoard();
    }

    private void UndoMove()
    {
        if (history.Count == 0 || clearInProgress)
        {
            return;
        }

        Move move = history.Pop();
        values[move.Row, move.Col] = move.BeforeValue;
        notes[move.Row, move.Col] = move.BeforeNote;
        selectedRow = move.Row;
        selectedCol = move.Col;
        RefreshBoard();
    }

    private void ToggleNotes()
    {
        if (clearInProgress)
        {
            return;
        }

        notesMode = !notesMode;
        if (notesBadgeText != null)
        {
            notesBadgeText.text = GetToggleLabel(notesMode);
        }
    }

    private void TogglePause()
    {
        if (clearInProgress)
        {
            return;
        }

        timerRunning = !timerRunning;
        if (pauseText != null)
        {
            pauseText.text = timerRunning ? "II" : ">";
        }
    }

    private void ShowSettings()
    {
        settingsOverlay.gameObject.SetActive(true);
        settingsOverlay.SetAsLastSibling();
        RefreshSettingsButtons();
    }

    private void HideSettings()
    {
        settingsOverlay.gameObject.SetActive(false);
    }

    private void ShowRules()
    {
        if (rulesOverlay == null)
        {
            return;
        }

        rulesOverlay.gameObject.SetActive(true);
        rulesOverlay.SetAsLastSibling();
    }

    private void HideRules()
    {
        if (rulesOverlay != null)
        {
            rulesOverlay.gameObject.SetActive(false);
        }
    }

    private void ShowLanguageSelector()
    {
        if (languageOverlay == null)
        {
            return;
        }

        RefreshLanguageButtons();
        languageOverlay.gameObject.SetActive(true);
        languageOverlay.SetAsLastSibling();
    }

    private void HideLanguageSelector()
    {
        if (languageOverlay != null)
        {
            languageOverlay.gameObject.SetActive(false);
        }
    }

    private void SetLanguage(GameLanguage language)
    {
        currentLanguage = language;
        PlayerPrefs.SetString(LanguagePrefsKey, language.ToString());
        PlayerPrefs.Save();

        RefreshLocalizedTexts();
        RefreshDifficultyLabels();
        RefreshMistakesLabel();
        RefreshSettingsButtons();
        RefreshLanguageButtons();
        if (notesBadgeText != null)
        {
            notesBadgeText.text = GetToggleLabel(notesMode);
        }

        ApplyTheme();
        RefreshBoard();
        Canvas.ForceUpdateCanvases();
        ApplyResponsiveLayout(true);
    }

    private void RefreshLocalizedTexts()
    {
        for (int i = 0; i < localizedTextBindings.Count; i++)
        {
            LocalizedTextBinding binding = localizedTextBindings[i];
            if (binding.Target == null || binding.GetValue == null)
            {
                continue;
            }

            binding.Target.text = binding.GetValue();
        }
    }

    private void RefreshLanguageButtons()
    {
        ThemeColors theme = GetThemeColors();
        for (int i = 0; i < languageButtonBindings.Count; i++)
        {
            LanguageButtonBinding binding = languageButtonBindings[i];
            bool selected = binding.Language == currentLanguage;

            if (binding.Background != null)
            {
                binding.Background.color = selected ? theme.Pill : theme.SurfaceAlt;
            }

            if (binding.Label != null)
            {
                binding.Label.text = GetLanguageOptionLabel(binding.Language) + (selected ? "  " + T("selected") : string.Empty);
                binding.Label.color = selected ? theme.Text : theme.MutedText;
            }
        }
    }

    private string GetModeButtonLabel(ThemeMode mode, bool selected)
    {
        string label = mode == ThemeMode.Dark ? T("darkMode") : T("whiteMode");
        return selected ? label + "  " + T("selected") : label;
    }

    private string GetToggleLabel(bool enabled)
    {
        if (enabled)
        {
            return T("on");
        }

        return T("off");
    }

    private string GetDifficultyDetail(SudokuDifficulty difficulty)
    {
        return StageCount + " " + T("stages") + " · " + T("mistakes") + " " + GetMistakeLimit(difficulty);
    }

    private string GetLanguageDisplayName(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.Chinese:
                return "Chinese";
            case GameLanguage.Russian:
                return "Russian";
            case GameLanguage.Hindi:
                return "Indian";
            case GameLanguage.Korean:
                return "Korean";
            case GameLanguage.French:
                return "French";
            default:
                return "English";
        }
    }

    private string GetLanguageOptionLabel(GameLanguage language)
    {
        return GetLanguageDisplayName(language);
    }

    private GameLanguage ParseLanguage(string value)
    {
        GameLanguage language;
        if (Enum.TryParse(value, out language))
        {
            return language;
        }

        return GameLanguage.English;
    }

    private string T(string key)
    {
        switch (key)
        {
            case "appTitle":
                return L("Sudoku", "数独", "Судоку", "सुडोकू", "스도쿠", "Sudoku");
            case "mainBadge":
                return L("mobile puzzle", "移动益智", "мобильная головоломка", "मोबाइल पहेली", "모바일 퍼즐", "puzzle mobile");
            case "mainSubtitle":
                return L("Clean board. Quick rounds. Pick your level.", "清爽棋盘。快速回合。选择难度。", "Чистое поле. Быстрые раунды. Выберите уровень.", "साफ बोर्ड। तेज़ राउंड। अपना स्तर चुनें।", "깔끔한 보드. 빠른 라운드. 난이도를 선택하세요.", "Grille claire. Parties rapides. Choisissez le niveau.");
            case "start":
                return L("Start", "开始", "Старт", "शुरू करें", "시작", "Commencer");
            case "startHint":
                return L("choose difficulty next", "接着选择难度", "затем выберите сложность", "अगला चरण: कठिनाई चुनें", "다음에 난이도 선택", "choisissez ensuite la difficulté");
            case "settings":
                return L("Settings", "设置", "Настройки", "सेटिंग्स", "설정", "Paramètres");
            case "gameRules":
                return L("Game Rules", "游戏规则", "Правила игры", "खेल के नियम", "게임 규칙", "Règles du jeu");
            case "selectLevel":
                return L("Select Level", "选择难度", "Выберите уровень", "स्तर चुनें", "난이도 선택", "Choisir le niveau");
            case "back":
                return L("Back", "返回", "Назад", "वापस", "뒤로", "Retour");
            case "topSubtitle":
                return L("calm focus", "静心专注", "спокойный фокус", "शांत ध्यान", "차분한 집중", "concentration calme");
            case "easy":
                return L("Easy", "简单", "Легко", "आसान", "쉬움", "Facile");
            case "normal":
                return L("Normal", "普通", "Нормально", "सामान्य", "보통", "Normal");
            case "hard":
                return L("Hard", "困难", "Сложно", "कठिन", "어려움", "Difficile");
            case "hint":
                return L("Hint", "提示", "Совет", "संकेत", "힌트", "Indice");
            case "clues":
                return L("clues", "提示数", "чисел", "संकेत", "힌트 숫자", "indices");
            case "stages":
                return L("stages", "关卡", "этапов", "स्टेज", "스테이지", "niveaux");
            case "mistakes":
                return L("Mistakes", "错误", "Ошибки", "गलतियाँ", "실수", "Erreurs");
            case "undo":
                return L("Undo", "撤销", "Отмена", "पूर्ववत", "되돌리기", "Annuler");
            case "erase":
                return L("Erase", "擦除", "Стереть", "मिटाएँ", "지우기", "Effacer");
            case "notes":
                return L("Notes", "笔记", "Заметки", "नोट्स", "노트", "Notes");
            case "settingsSubtitle":
                return L("Choose screen mode", "选择屏幕模式", "Выберите режим экрана", "स्क्रीन मोड चुनें", "화면 모드 선택", "Choisissez le mode d'écran");
            case "whiteMode":
                return L("White Mode", "白色模式", "Светлый режим", "लाइट मोड", "화이트 모드", "Mode clair");
            case "darkMode":
                return L("Dark Mode", "深色模式", "Темный режим", "डार्क मोड", "다크 모드", "Mode sombre");
            case "settingsFooter":
                return L("The selected mode is saved automatically.", "所选模式会自动保存。", "Выбранный режим сохраняется автоматически.", "चुना गया मोड अपने आप सेव होता है।", "선택한 모드는 자동으로 저장됩니다.", "Le mode sélectionné est enregistré automatiquement.");
            case "language":
                return L("Language", "语言", "Язык", "भाषा", "언어", "Langue");
            case "languageSubtitle":
                return L("Choose game language", "选择游戏语言", "Выберите язык игры", "खेल की भाषा चुनें", "게임 언어 선택", "Choisissez la langue du jeu");
            case "selected":
                return L("SELECTED", "已选择", "ВЫБРАНО", "चयनित", "선택됨", "SÉLECTIONNÉ");
            case "close":
                return L("Close", "关闭", "Закрыть", "बंद करें", "닫기", "Fermer");
            case "roundClear":
                return L("Round Clear", "回合完成", "Раунд пройден", "राउंड पूरा", "라운드 클리어", "Manche réussie");
            case "gameOver":
                return L("Game Over", "游戏结束", "Игра окончена", "गेम ओवर", "게임 오버", "Partie terminée");
            case "stage":
                return L("Stage", "关卡", "Этап", "स्टेज", "스테이지", "Niveau");
            case "completed":
                return L("completed", "完成", "завершен", "पूरा", "완료", "terminé");
            case "mistakesFull":
                return L("mistakes are full", "错误次数已满", "лимит ошибок исчерпан", "गलतियों की सीमा पूरी हो गई", "실수 횟수가 가득 찼습니다", "limite d'erreurs atteinte");
            case "mainScreen":
                return L("Main Screen", "主界面", "Главный экран", "मुख्य स्क्रीन", "메인 화면", "Écran principal");
            case "nextRound":
                return L("Next Round", "下一回合", "Следующий раунд", "अगला राउंड", "다음 라운드", "Manche suivante");
            case "retry":
                return L("Retry", "重试", "Повторить", "फिर से", "다시 하기", "Réessayer");
            case "on":
                return L("ON", "开", "ВКЛ", "चालू", "켜짐", "ON");
            case "off":
                return L("OFF", "关", "ВЫКЛ", "बंद", "꺼짐", "OFF");
            case "rulesBody":
                return GetRulesBody();
            default:
                return key;
        }
    }

    private string L(string english, string chinese, string russian, string hindi, string korean, string french)
    {
        switch (currentLanguage)
        {
            case GameLanguage.Chinese:
                return chinese;
            case GameLanguage.Russian:
                return russian;
            case GameLanguage.Hindi:
                return hindi;
            case GameLanguage.Korean:
                return korean;
            case GameLanguage.French:
                return french;
            default:
                return english;
        }
    }

    private string GetRulesBody()
    {
        switch (currentLanguage)
        {
            case GameLanguage.Chinese:
                return "数独通常是在 9×9 方格上进行的数字谜题。\n\n"
                    + "目标是填满所有空格，并满足下面的规则。\n\n"
                    + "1. 每一行必须恰好包含数字 1 到 9 各一次。\n"
                    + "2. 每一列也必须恰好包含数字 1 到 9 各一次。\n"
                    + "3. 每一个 3×3 宫也必须恰好包含数字 1 到 9 各一次。\n"
                    + "4. 开始时给出的数字不能更改。\n\n"
                    + "例如，如果某一行已经有一个 5，你就不能在同一行的其他格子再放一个 5。同样，同一列或同一个 3×3 宫内也不能放重复数字。";
            case GameLanguage.Russian:
                return "Судоку — это числовая головоломка, обычно играемая на сетке 9×9.\n\n"
                    + "Цель — заполнить пустые клетки так, чтобы соблюдались все правила.\n\n"
                    + "1. В каждой строке должны быть числа от 1 до 9 ровно по одному разу.\n"
                    + "2. В каждом столбце также должны быть числа от 1 до 9 ровно по одному разу.\n"
                    + "3. В каждом блоке 3×3 также должны быть числа от 1 до 9 ровно по одному разу.\n"
                    + "4. Числа, данные в начале, нельзя изменять.\n\n"
                    + "Например, если в строке уже есть 5, нельзя поставить еще одну 5 в другую клетку этой строки. Точно так же нельзя ставить повторяющиеся числа в одном столбце или одном блоке 3×3.";
            case GameLanguage.Hindi:
                return "सुडोकू एक संख्या पहेली है, जिसे आमतौर पर 9×9 ग्रिड पर खेला जाता है।\n\n"
                    + "लक्ष्य खाली खानों को भरना है ताकि नीचे दिए गए सभी नियम पूरे हों।\n\n"
                    + "1. हर पंक्ति में 1 से 9 तक के अंक ठीक एक बार होने चाहिए।\n"
                    + "2. हर कॉलम में भी 1 से 9 तक के अंक ठीक एक बार होने चाहिए।\n"
                    + "3. हर 3×3 बॉक्स में भी 1 से 9 तक के अंक ठीक एक बार होने चाहिए।\n"
                    + "4. शुरुआत में दिए गए अंकों को बदला नहीं जा सकता।\n\n"
                    + "उदाहरण के लिए, अगर किसी पंक्ति में पहले से 5 है, तो उसी पंक्ति की किसी दूसरी खाने में 5 नहीं रखा जा सकता। इसी तरह, एक ही कॉलम या एक ही 3×3 बॉक्स में दोहराए गए अंक नहीं रखे जा सकते।";
            case GameLanguage.Korean:
                return "스도쿠는 보통 9×9 격자에서 플레이하는 숫자 퍼즐입니다.\n\n"
                    + "목표는 빈 칸을 채워 아래 규칙을 모두 만족시키는 것입니다.\n\n"
                    + "1. 각 가로줄에는 1부터 9까지의 숫자가 정확히 한 번씩 들어가야 합니다.\n"
                    + "2. 각 세로줄에도 1부터 9까지의 숫자가 정확히 한 번씩 들어가야 합니다.\n"
                    + "3. 각 3×3 박스에도 1부터 9까지의 숫자가 정확히 한 번씩 들어가야 합니다.\n"
                    + "4. 처음부터 주어진 숫자는 바꿀 수 없습니다.\n\n"
                    + "예를 들어, 어떤 가로줄에 이미 5가 있다면 같은 줄의 다른 칸에 5를 또 놓을 수 없습니다. 같은 방식으로 같은 세로줄이나 같은 3×3 박스에도 중복 숫자를 놓을 수 없습니다.";
            case GameLanguage.French:
                return "Le Sudoku est un puzzle de nombres généralement joué sur une grille 9×9.\n\n"
                    + "Le but est de remplir les cases vides en respectant toutes les règles suivantes.\n\n"
                    + "1. Chaque ligne doit contenir les nombres de 1 à 9 une seule fois.\n"
                    + "2. Chaque colonne doit aussi contenir les nombres de 1 à 9 une seule fois.\n"
                    + "3. Chaque bloc 3×3 doit aussi contenir les nombres de 1 à 9 une seule fois.\n"
                    + "4. Les nombres donnés au début ne peuvent pas être changés.\n\n"
                    + "Par exemple, si une ligne contient déjà un 5, vous ne pouvez pas placer un autre 5 dans cette même ligne. De la même manière, les nombres en double ne peuvent pas être placés dans la même colonne ou le même bloc 3×3.";
            default:
                return "Sudoku is a number puzzle usually played on a 9×9 grid.\n\n"
                    + "The goal is to fill in the empty cells so that all of the following rules are satisfied.\n\n"
                    + "1. Each row must contain the numbers 1 to 9 exactly once.\n"
                    + "2. Each column must also contain the numbers 1 to 9 exactly once.\n"
                    + "3. Each 3×3 box must also contain the numbers 1 to 9 exactly once.\n"
                    + "4. The numbers given at the start cannot be changed.\n\n"
                    + "For example, if a row already has a 5, you cannot place another 5 in any other cell in that row. In the same way, duplicate numbers cannot be placed in the same column or the same 3×3 box.";
        }
    }

    private void SetTheme(ThemeMode mode)
    {
        themeMode = mode;
        PlayerPrefs.SetString(ThemePrefsKey, mode == ThemeMode.Dark ? "Dark" : "Light");
        PlayerPrefs.Save();
        ApplyTheme();
        RefreshBoard();
        RefreshSettingsButtons();
    }

    private void ApplyTheme()
    {
        ThemeColors theme = GetThemeColors();

        for (int i = 0; i < imageBindings.Count; i++)
        {
            ImageBinding binding = imageBindings[i];
            if (binding.Target == null)
            {
                continue;
            }

            binding.Target.color = WithAlpha(GetThemeColor(theme, binding.Role), binding.Alpha);
        }

        for (int i = 0; i < textBindings.Count; i++)
        {
            TextBinding binding = textBindings[i];
            if (binding.Target == null)
            {
                continue;
            }

            binding.Target.color = WithAlpha(GetThemeColor(theme, binding.Role), binding.Alpha);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = theme.Background;
        }

        RefreshSettingsButtons();
    }

    private void RefreshBoard()
    {
        ThemeColors theme = GetThemeColors();
        int selectedValue = values[selectedRow, selectedCol];

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                CellView cell = cells[row, col];
                bool selected = row == selectedRow && col == selectedCol;
                bool peer = row == selectedRow || col == selectedCol || SameBox(row, col, selectedRow, selectedCol);
                bool sameNumber = selectedValue != 0 && values[row, col] == selectedValue;

                Color background = theme.Cell;
                if (selected)
                {
                    background = theme.SelectedCell;
                }
                else if (sameNumber)
                {
                    background = theme.SameNumberCell;
                }
                else if (peer)
                {
                    background = theme.PeerCell;
                }

                cell.Background.color = background;

                int value = values[row, col];
                int note = notes[row, col];
                bool hasNote = value == 0 && note != 0;
                cell.NumberText.text = value != 0 ? value.ToString() : (hasNote ? note.ToString() : string.Empty);

                bool userEntry = !locked[row, col];
                bool emphasized = value != 0 && value == selectedValue;
                if (hasNote)
                {
                    cell.NumberText.color = GetThemeColor(theme, ThemeRole.NoteText);
                }
                else if (userEntry || emphasized)
                {
                    cell.NumberText.color = theme.Accent;
                }
                else
                {
                    cell.NumberText.color = theme.Text;
                }
            }
        }
    }

    private void RefreshSettingsButtons()
    {
        ThemeColors theme = GetThemeColors();
        bool light = themeMode == ThemeMode.Light;

        if (lightModeButtonImage != null)
        {
            lightModeButtonImage.color = light ? theme.Pill : theme.SurfaceAlt;
        }

        if (darkModeButtonImage != null)
        {
            darkModeButtonImage.color = light ? theme.SurfaceAlt : theme.Pill;
        }

        if (languageButtonImage != null)
        {
            languageButtonImage.color = theme.SurfaceAlt;
        }

        if (lightModeButtonText != null)
        {
            lightModeButtonText.text = GetModeButtonLabel(ThemeMode.Light, light);
            lightModeButtonText.color = light ? theme.Text : theme.MutedText;
        }

        if (darkModeButtonText != null)
        {
            darkModeButtonText.text = GetModeButtonLabel(ThemeMode.Dark, !light);
            darkModeButtonText.color = light ? theme.MutedText : theme.Text;
        }

        if (languageButtonText != null)
        {
            languageButtonText.text = T("language") + ": " + GetLanguageDisplayName(currentLanguage);
            languageButtonText.color = theme.Text;
        }

        RefreshLanguageButtons();
    }

    private void UpdateTimerText(bool force)
    {
        int totalSeconds = Mathf.FloorToInt(elapsedSeconds);
        if (!force && totalSeconds == lastDisplayedSecond)
        {
            return;
        }

        lastDisplayedSecond = totalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timerText != null)
        {
            timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    private PuzzleConfig GetPuzzleConfig(SudokuDifficulty difficulty, int stageIndex)
    {
        int clueCount = GetClueCount(difficulty);
        int hintCount = GetHintCount(difficulty);
        int stageSeed = GetStageSeed(difficulty, stageIndex);
        int[,] stageSolution = BuildStageSolution(stageSeed);
        int[,] puzzle = BuildPuzzleFromSolution(stageSolution, clueCount, stageSeed);

        return new PuzzleConfig
        {
            Label = GetDifficultyLabel(difficulty),
            HintCount = hintCount,
            Puzzle = puzzle,
            Solution = stageSolution
        };
    }

    private int CreatePuzzleRunSeed(SudokuDifficulty difficulty)
    {
        unchecked
        {
            int seed = Environment.TickCount;
            seed = (seed * 397) ^ DateTime.Now.Millisecond;
            seed = (seed * 397) ^ UnityEngine.Random.Range(1, int.MaxValue);
            seed = (seed * 397) ^ (((int)difficulty + 1) * 1009);
            return seed == 0 ? 1 : seed;
        }
    }

    private int GetStageSeed(SudokuDifficulty difficulty, int stageIndex)
    {
        unchecked
        {
            int seed = puzzleRunSeed;
            seed = (seed * 397) ^ (((int)difficulty + 1) * 15485863);
            seed = (seed * 397) ^ ((stageIndex + 1) * 32452843);
            return seed == 0 ? 1 : seed;
        }
    }

    private int GetHintCount(SudokuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case SudokuDifficulty.Easy:
                return 3;
            case SudokuDifficulty.Hard:
                return 1;
            default:
                return 2;
        }
    }

    private int GetMistakeLimit(SudokuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case SudokuDifficulty.Easy:
                return 3;
            case SudokuDifficulty.Hard:
                return 1;
            default:
                return 2;
        }
    }

    private int GetClueCount(SudokuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case SudokuDifficulty.Easy:
                return 55;
            case SudokuDifficulty.Hard:
                return 22;
            default:
                return 30;
        }
    }

    private string GetDifficultyLabel(SudokuDifficulty difficulty)
    {
        switch (difficulty)
        {
            case SudokuDifficulty.Easy:
                return T("easy");
            case SudokuDifficulty.Hard:
                return T("hard");
            default:
                return T("normal");
        }
    }

    private int[,] BuildStageSolution(int seed)
    {
        int[] rowMap = BuildUnitMap(seed + 101);
        int[] colMap = BuildUnitMap(seed + 503);
        int[] numberMap = BuildNumberMap(seed + 907);
        int[,] result = new int[GridSize, GridSize];

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                int baseValue = baseSolution[rowMap[row], colMap[col]];
                result[row, col] = numberMap[baseValue];
            }
        }

        return result;
    }

    private int[,] BuildPuzzleFromSolution(int[,] stageSolution, int clueCount, int seed)
    {
        int[,] puzzle = new int[GridSize, GridSize];
        int[] cellOrder = BuildPermutation(GridSize * GridSize, seed + 1301);
        int placed = 0;

        for (int rank = 0; rank < GridSize * GridSize && placed < clueCount; rank++)
        {
            int index = cellOrder[rank];
            int row = index / GridSize;
            int col = index % GridSize;

            puzzle[row, col] = stageSolution[row, col];
            placed++;
        }

        return puzzle;
    }

    private int[] BuildUnitMap(int seed)
    {
        int[] bands = BuildPermutation(3, seed);
        int[] result = new int[GridSize];
        int write = 0;

        for (int bandIndex = 0; bandIndex < 3; bandIndex++)
        {
            int band = bands[bandIndex];
            int[] inner = BuildPermutation(3, seed + (bandIndex * 31) + 7);
            for (int i = 0; i < 3; i++)
            {
                result[write] = (band * 3) + inner[i];
                write++;
            }
        }

        return result;
    }

    private int[] BuildNumberMap(int seed)
    {
        int[] order = new int[GridSize + 1];
        int[] shuffled = BuildPermutation(GridSize, seed);

        for (int value = 1; value <= GridSize; value++)
        {
            order[value] = shuffled[value - 1] + 1;
        }

        return order;
    }

    private int[] BuildPermutation(int length, int seed)
    {
        int[] result = new int[length];
        for (int index = 0; index < length; index++)
        {
            result[index] = index;
        }

        int state = seed == 0 ? 1 : seed;
        for (int index = length - 1; index > 0; index--)
        {
            int swapIndex = NextPositive(ref state) % (index + 1);
            int temp = result[index];
            result[index] = result[swapIndex];
            result[swapIndex] = temp;
        }

        return result;
    }

    private int NextPositive(ref int state)
    {
        unchecked
        {
            state ^= state << 13;
            state ^= (int)((uint)state >> 17);
            state ^= state << 5;
            return (int)((uint)state & 0x7fffffff);
        }
    }

    private bool SameBox(int aRow, int aCol, int bRow, int bCol)
    {
        return aRow / 3 == bRow / 3 && aCol / 3 == bCol / 3;
    }

    private ThemeColors GetThemeColors()
    {
        if (themeMode == ThemeMode.Dark)
        {
            return new ThemeColors
            {
                Background = C("#101318"),
                TopBar = C("#151A21"),
                Surface = C("#1F2630"),
                SurfaceAlt = C("#28313D"),
                Cell = C("#171C22"),
                PeerCell = C("#213835"),
                SameNumberCell = C("#3A2B28"),
                SelectedCell = C("#D86F56"),
                GridLine = C("#536171"),
                ThickGridLine = C("#B3BDC8"),
                Text = C("#F5F7FA"),
                MutedText = C("#A5ADB8"),
                NoteText = C("#F59E0B"),
                Accent = C("#65D2C6"),
                AccentSoft = C("#355450"),
                Pill = C("#2F615B"),
                ModalBackdrop = C("#000000")
            };
        }

        return new ThemeColors
        {
            Background = C("#F7F8FA"),
            TopBar = C("#FFFFFF"),
            Surface = C("#FFFFFF"),
            SurfaceAlt = C("#EDF3F2"),
            Cell = C("#FCFCFD"),
            PeerCell = C("#E7F5F2"),
            SameNumberCell = C("#FFF0E8"),
            SelectedCell = C("#F7C7B6"),
            GridLine = C("#B7C2CE"),
            ThickGridLine = C("#17202A"),
            Text = C("#17202A"),
            MutedText = C("#6B7280"),
            NoteText = C("#F59E0B"),
            Accent = C("#2A8B7F"),
            AccentSoft = C("#CFE8E4"),
            Pill = C("#BFECE6"),
            ModalBackdrop = C("#000000")
        };
    }

    private Color GetThemeColor(ThemeColors theme, ThemeRole role)
    {
        switch (role)
        {
            case ThemeRole.Background:
                return theme.Background;
            case ThemeRole.TopBar:
                return theme.TopBar;
            case ThemeRole.Surface:
                return theme.Surface;
            case ThemeRole.SurfaceAlt:
                return theme.SurfaceAlt;
            case ThemeRole.Cell:
                return theme.Cell;
            case ThemeRole.PeerCell:
                return theme.PeerCell;
            case ThemeRole.SameNumberCell:
                return theme.SameNumberCell;
            case ThemeRole.SelectedCell:
                return theme.SelectedCell;
            case ThemeRole.GridLine:
                return theme.GridLine;
            case ThemeRole.ThickGridLine:
                return theme.ThickGridLine;
            case ThemeRole.Text:
                return theme.Text;
            case ThemeRole.MutedText:
                return theme.MutedText;
            case ThemeRole.NoteText:
                return theme.NoteText;
            case ThemeRole.Accent:
                return theme.Accent;
            case ThemeRole.AccentSoft:
                return theme.AccentSoft;
            case ThemeRole.Pill:
                return theme.Pill;
            case ThemeRole.ModalBackdrop:
                return theme.ModalBackdrop;
            default:
                return Color.white;
        }
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private Image AddImage(RectTransform rect, ThemeRole role, bool sliced)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sliced ? roundedSprite : squareSprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = true;
        return image;
    }

    private void BindImage(Image image, ThemeRole role)
    {
        BindImage(image, role, 1f);
    }

    private void BindImage(Image image, ThemeRole role, float alpha)
    {
        imageBindings.Add(new ImageBinding
        {
            Target = image,
            Role = role,
            Alpha = alpha
        });
    }

    private Text CreateText(RectTransform parent, string value, int fontSize, TextAnchor anchor, ThemeRole role, FontStyle style)
    {
        RectTransform textRect = CreateRect("Text", parent);
        Stretch(textRect);

        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.alignByGeometry = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        textBindings.Add(new TextBinding
        {
            Target = text,
            Role = role,
            Alpha = 1f
        });

        return text;
    }

    private Text CreateLocalizedText(RectTransform parent, int fontSize, TextAnchor anchor, ThemeRole role, FontStyle style, Func<string> getValue)
    {
        Text text = CreateText(parent, getValue(), fontSize, anchor, role, style);
        localizedTextBindings.Add(new LocalizedTextBinding
        {
            Target = text,
            GetValue = getValue
        });
        return text;
    }

    private void ConfigureButton(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.88f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.36f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void SetupResultAudio()
    {
        EnsureAudioListener();

        resultAudioSource = gameObject.AddComponent<AudioSource>();
        resultAudioSource.playOnAwake = false;
        resultAudioSource.loop = false;
        resultAudioSource.spatialBlend = 0f;
        resultAudioSource.volume = 1f;

        resultSound = Resources.Load<AudioClip>(ResultSoundResourcePath);
        if (resultSound == null)
        {
            Debug.LogWarning("Result sound was not found at Resources/" + ResultSoundResourcePath);
        }
    }

    private void EnsureAudioListener()
    {
        if (FindFirstObjectByType<AudioListener>() != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        GameObject listenerHost = mainCamera != null ? mainCamera.gameObject : gameObject;
        listenerHost.AddComponent<AudioListener>();
    }

    private void PlayResultSound()
    {
        if (resultAudioSource == null || resultSound == null)
        {
            return;
        }

        resultAudioSource.PlayOneShot(resultSound);
    }

    private void ConfigureMobileRuntime()
    {
        SudokuStartupOrientationLock.LockPortraitNow();
        Application.targetFrameRate = 60;
    }

    private Font LoadBuiltinFont()
    {
        Font osFont = TryCreateDynamicFont(new[]
        {
            "Malgun Gothic",
            "Microsoft YaHei UI",
            "Nirmala UI",
            "Segoe UI",
            "Arial Unicode MS",
            "Arial"
        });
        if (osFont != null)
        {
            return osFont;
        }

        Font font = TryLoadBuiltinFont("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        font = TryLoadBuiltinFont("Arial.ttf");
        if (font != null)
        {
            return font;
        }

        Debug.LogWarning("No built-in font was found. Text may not render until a font is assigned.");
        return null;
    }

    private Font TryCreateDynamicFont(string[] fontNames)
    {
        try
        {
            return Font.CreateDynamicFontFromOSFont(fontNames, 32);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private Font TryLoadBuiltinFont(string resourceName)
    {
        try
        {
            return Resources.GetBuiltinResource<Font>(resourceName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateRoundedSprite(int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float r = radius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, r, size - r);
                float cy = Mathf.Clamp(py, r, size - r);
                float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(r + 0.5f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Color C(string html)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(html, out color))
        {
            return color;
        }

        return Color.magenta;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static void Stretch(RectTransform rect)
    {
        Stretch(rect, 0f, 0f, 0f, 0f);
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void StretchTop(RectTransform rect, float left, float right, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void StretchBottom(RectTransform rect, float left, float right, float bottom, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, bottom + height);
    }

    private static void AnchorLeft(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(left, -top);
    }

    private static void AnchorRight(RectTransform rect, float right, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(-right, -top);
    }

    private static void AnchorCenter(RectTransform rect, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void AnchorCenterTop(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, -y);
    }

    private static void AnchorGridCell(RectTransform rect, int row, int col)
    {
        float minX = col / 9f;
        float maxX = (col + 1) / 9f;
        float minY = 1f - (row + 1) / 9f;
        float maxY = 1f - row / 9f;

        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Vector2 anchorMin = lastSafeArea.position;
            Vector2 anchorMax = lastSafeArea.position + lastSafeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
