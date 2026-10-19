
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public TurnManager TurnManager { get; private set; }

    [Header("Scene References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerController playerController;

    [Header("Food Settings")]
    [SerializeField, Range(1, 200)]
    private int startingFood = 30;

    [SerializeField] private int currentFood;

    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;

    private Label foodLabel;
    private Label levelLabel;

    private VisualElement gameOverPanel;
    private VisualElement mainMenuPanel;
    private VisualElement pauseMenuPanel;

    private Button playButton;
    private Button mainQuitButton;
    private Button resumeButton;
    private Button pauseRestartButton;
    private Button pauseQuitButton;
    private Button restartButton;

    private bool gameOver;
    private bool isPaused;
    private bool isInMainMenu;

    public bool CanPlayerAct =>
        !gameOver && !isPaused && !isInMainMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (Instance != this)
            return;

        if (boardManager == null || playerController == null)
        {
            Debug.LogError("Missing GameManager references!");
            return;
        }

        TurnManager = new TurnManager();
        TurnManager.OnTick += HandleTurn;

        SetupUI();
        ShowMainMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isInMainMenu && !gameOver)
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }

        if (gameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    private void SetupUI()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UI Document is not assigned!");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        foodLabel = root.Q<Label>("FoodLabel");
        levelLabel = root.Q<Label>("LevelLabel");

        gameOverPanel =
            root.Q<VisualElement>("GameOverPanel");

        mainMenuPanel =
            root.Q<VisualElement>("MainMenuPanel");

        pauseMenuPanel =
            root.Q<VisualElement>("PauseMenuPanel");

        playButton = root.Q<Button>("PlayButton");
        mainQuitButton = root.Q<Button>("MainQuitButton");

        resumeButton = root.Q<Button>("ResumeButton");
        pauseRestartButton =
            root.Q<Button>("PauseRestartButton");

        pauseQuitButton = root.Q<Button>("PauseQuitButton");

        restartButton = root.Q<Button>("RestartButton");

        if (playButton != null)
            playButton.clicked += StartGame;

        if (mainQuitButton != null)
            mainQuitButton.clicked += QuitGame;

        if (resumeButton != null)
            resumeButton.clicked += ResumeGame;

        if (pauseRestartButton != null)
            pauseRestartButton.clicked += RestartGame;

        if (pauseQuitButton != null)
            pauseQuitButton.clicked += QuitGame;

        if (restartButton != null)
            restartButton.clicked += RestartGame;

        if (mainMenuPanel == null)
            Debug.LogError("MainMenuPanel not found!");

        if (pauseMenuPanel == null)
            Debug.LogError("PauseMenuPanel not found!");

        if (gameOverPanel == null)
            Debug.LogError("GameOverPanel not found!");
    }

    private void ShowMainMenu()
    {
        isInMainMenu = true;
        isPaused = false;
        gameOver = false;

        Time.timeScale = 0f;

        SetVisible(mainMenuPanel, true);
        SetVisible(pauseMenuPanel, false);
        SetVisible(gameOverPanel, false);

        Debug.Log("Main Menu opened.");
    }

    public void StartGame()
    {
        isInMainMenu = false;
        RestartGame();
    }

    public void PauseGame()
    {
        if (gameOver || isInMainMenu || isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        SetVisible(pauseMenuPanel, true);

        Debug.Log("Game paused.");
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        SetVisible(pauseMenuPanel, false);

        Debug.Log("Game resumed.");
    }

    public void RestartGame()
    {
        if (boardManager == null ||
            playerController == null ||
            TurnManager == null)
        {
            Debug.LogError("Cannot restart game!");
            return;
        }

        Time.timeScale = 1f;

        gameOver = false;
        isPaused = false;
        isInMainMenu = false;

        currentLevel = 1;
        currentFood = startingFood;

        SetVisible(mainMenuPanel, false);
        SetVisible(pauseMenuPanel, false);
        SetVisible(gameOverPanel, false);

        boardManager.Init();

        playerController.Spawn(new Vector2Int(1, 1));

        boardManager.SpawnEnemies(
            currentLevel,
            playerController.transform,
            TurnManager
        );

        UpdateUI();

        Debug.Log("Game started/restarted!");
    }

    public void NextLevel()
    {
        if (!CanPlayerAct)
            return;

        currentLevel++;

        boardManager.Init();

        playerController.Spawn(new Vector2Int(1, 1));

        boardManager.SpawnEnemies(
            currentLevel,
            playerController.transform,
            TurnManager
        );

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLevelComplete();

        UpdateUI();

        Debug.Log("Level: " + currentLevel);
    }

    private void HandleTurn()
    {
        if (!CanPlayerAct)
            return;

        currentFood = Mathf.Max(0, currentFood - 1);

        UpdateUI();

        Debug.Log("Food remaining: " + currentFood);

        if (currentFood <= 0)
            GameOver();
    }

    public void AddFood(int amount)
    {
        if (!CanPlayerAct || amount <= 0)
            return;

        currentFood += amount;
        UpdateUI();

        Debug.Log("Food added: +" + amount);
    }

    public void TakeDamage(int damage)
    {
        if (!CanPlayerAct || damage <= 0)
            return;

        currentFood = Mathf.Max(0, currentFood - damage);

        UpdateUI();

        Debug.Log("Player took " + damage + " damage!");

        if (currentFood <= 0)
            GameOver();
    }

    private void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;
        isPaused = false;

        SetVisible(pauseMenuPanel, false);
        SetVisible(gameOverPanel, true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameOver();

        Debug.Log("GAME OVER!");
    }

    private void UpdateUI()
    {
        if (foodLabel != null)
            foodLabel.text = "Food: " + currentFood;

        if (levelLabel != null)
            levelLabel.text = "Level: " + currentLevel;
    }

    private void SetVisible(
        VisualElement element,
        bool visible)
    {
        if (element != null)
        {
            element.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game requested.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (TurnManager != null)
            TurnManager.OnTick -= HandleTurn;

        if (playButton != null)
            playButton.clicked -= StartGame;

        if (mainQuitButton != null)
            mainQuitButton.clicked -= QuitGame;

        if (resumeButton != null)
            resumeButton.clicked -= ResumeGame;

        if (pauseRestartButton != null)
            pauseRestartButton.clicked -= RestartGame;

        if (pauseQuitButton != null)
            pauseQuitButton.clicked -= QuitGame;

        if (restartButton != null)
            restartButton.clicked -= RestartGame;

        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }
}
