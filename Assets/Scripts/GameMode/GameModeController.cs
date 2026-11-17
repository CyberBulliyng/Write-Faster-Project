using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Игровой режим: враги-шарики со словами едут к игроку в центре экрана.
/// Игрок набирает слово врага, чтобы его уничтожить. Уровни бесконечные,
/// каждый пятый уровень — босс со словами по очереди.
/// Статистика набора считается скрыто и пишется в общий файл статистики.
/// </summary>
public class GameModeController : MonoBehaviour
{
    [Header("Мир")]
    public Transform player;
    public Transform enemyRoot;

    [Header("HUD")]
    public TMP_Text scoreText;
    public Image scoreBar;
    public TMP_Text progressText;
    public Image progressBar;
    public TMP_Text timerText;
    public TMP_Text levelText;
    public TMP_Text healthText;
    public Image healthBar;
    public TMP_Text toastText;
    public Image damageFlash;

    [Header("Настройки режима")]
    public int maxHealth = 3;
    public int scorePerRank = 500;
    [Tooltip("Если включено, ошибка в слове обнуляет прогресс набора этого слова")]
    public bool resetProgressOnMistake = true;
    public float intermissionSeconds = 2.2f;

    [Header("Шрифт слов (можно оставить пустым)")]
    public TMP_FontAsset wordFont;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly Dictionary<char, int> attemptsByKey = new Dictionary<char, int>();
    private readonly Dictionary<char, int> errorsByKey = new Dictionary<char, int>();
    private static readonly System.Random random = new System.Random();

    private LevelInfo info;
    private Enemy activeEnemy;

    private int level = 1;
    private int score;
    private int health;
    private int spawnedCount;
    private int killedCount;
    private int reachedCount;
    private int bossWordsDone;

    private float spawnTimer;
    private float intermissionTimer;
    private float levelStartedAt;
    private float flashTimer;
    private float hudTimer;

    private bool gameOver;
    private bool statsRecorded;
    private bool sessionStarted;

    private double correctKeystrokes;
    private double errorKeystrokes;
    private double sessionStartedAt;
    private double sessionEndedAt;

    public int Level { get { return level; } }
    public int Score { get { return score; } }
    public int Health { get { return health; } }
    public bool IsGameOver { get { return gameOver; } }
    public bool IsBossLevel { get { return info != null && info.isBoss; } }
    public Enemy ActiveEnemy { get { return activeEnemy; } }
    public int EnemyCount { get { return enemies.Count; } }
    public LevelInfo CurrentInfo { get { return info; } }

    // ---------------------------------------------------------------- жизненный цикл

    private void Start()
    {
        if (CheckEnterkeyboard.Get() == null)
        {
            Debug.LogError("GameModeController: нет CheckEnterkeyboard на сцене.");
            return;
        }

        if (player == null)
        {
            GameObject foundPlayer = GameObject.Find("Player");
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        if (enemyRoot == null)
        {
            GameObject foundEnemies = GameObject.Find("Enemies");
            if (foundEnemies != null)
                enemyRoot = foundEnemies.transform;
        }

        // Круг игрока генерируем кодом при старте сцены
        if (player != null)
        {
            SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
            if (playerSprite != null && playerSprite.sprite == null)
                playerSprite.sprite = CircleSpriteFactory.Get();
        }

        CheckEnterkeyboard.Get().enterNewCharacter += HandleCharacter;

        if (PauseMenu.Find() != null)
            PauseMenu.Find().restartRequested += Restart;

        if (wordFont == null)
        {
            wordFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (wordFont == null)
                wordFont = TMP_Settings.defaultFontAsset;
        }

        Restart();
    }

    private void OnDestroy()
    {
        if (CheckEnterkeyboard.Get() != null)
            CheckEnterkeyboard.Get().enterNewCharacter -= HandleCharacter;

        if (PauseMenu.Find() != null)
            PauseMenu.Find().restartRequested -= Restart;

        RecordSession();
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime);
            if (damageFlash != null)
            {
                Color flash = damageFlash.color;
                flash.a = flashTimer * 1.6f;
                damageFlash.color = flash;
            }
        }

        if (gameOver)
            return;

        if (intermissionTimer > 0f)
        {
            intermissionTimer -= Time.deltaTime;
            if (intermissionTimer <= 0f)
                BeginLevel(level + 1);

            return;
        }

        UpdateSpawning();
        CheckLevelFinished();

        hudTimer += Time.unscaledDeltaTime;
        if (hudTimer >= 0.1f)
        {
            hudTimer = 0f;
            UpdateHud();
        }
    }

    // ---------------------------------------------------------------- уровень

    private void Restart()
    {
        ClearEnemies();

        level = 1;
        score = 0;
        health = maxHealth;
        gameOver = false;
        statsRecorded = false;
        sessionStarted = false;
        correctKeystrokes = 0;
        errorKeystrokes = 0;
        attemptsByKey.Clear();
        errorsByKey.Clear();

        BeginLevel(1);
    }

    /// <summary>Убрать всех врагов (остатки прошлого уровня не должны долетать до игрока).</summary>
    private void ClearEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] != null)
                enemies[i].Despawn();
        }

        enemies.Clear();
        activeEnemy = null;
    }

    private void BeginLevel(int newLevel)
    {
        ClearEnemies();

        level = newLevel;
        info = LevelRules.Get(level);

        spawnedCount = 0;
        killedCount = 0;
        reachedCount = 0;
        bossWordsDone = 0;
        spawnTimer = 0.4f;
        intermissionTimer = 0f;
        levelStartedAt = Time.time;
        activeEnemy = null;

        if (levelText != null)
            levelText.text = info.isBoss ? ("Уровень " + level + " — БОСС") : ("Уровень " + level);

        if (info.isBoss)
            SetToast("Босс! Набери " + info.bossWordCount + " слов подряд");
        else
            SetToast("Уровень " + level + ": " + info.enemyCount + " врагов, слова " +
                     info.minWordLength + "–" + info.maxWordLength + " букв");

        UpdateHud();
    }

    private void UpdateSpawning()
    {
        if (spawnedCount >= info.enemyCount)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        if (CountAlive() >= info.maxAliveEnemies)
        {
            spawnTimer = 0.3f;
            return;
        }

        SpawnEnemy();
        spawnTimer = info.spawnInterval;
    }

    private int CountAlive()
    {
        int alive = 0;
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] != null && enemies[i].Alive)
                alive++;

        return alive;
    }

    private void SpawnEnemy()
    {
        if (enemyRoot == null)
        {
            Debug.LogError("GameModeController: не назначен enemyRoot.");
            return;
        }

        Vector3 position = RandomEdgePoint();

        List<string> words = new List<string>();

        if (info.isBoss)
        {
            for (int i = 0; i < info.bossWordCount; i++)
                words.Add(WordLibrary.GetRandom(info.minWordLength, info.maxWordLength, random));

            Enemy boss = Enemy.Create(enemyRoot, position, true, info.bossSpeed, words, wordFont, player);
            Register(boss);
        }
        else
        {
            words.Add(WordLibrary.GetRandom(info.minWordLength, info.maxWordLength, random));

            Enemy enemy = Enemy.Create(enemyRoot, position, false, info.enemySpeed, words, wordFont, player);
            Register(enemy);
        }

        spawnedCount++;
    }

    private void Register(Enemy enemy)
    {
        enemy.wordCompleted += OnWordCompleted;
        enemy.killed += OnEnemyKilled;
        enemy.reachedPlayer += OnReachedPlayer;
        enemies.Add(enemy);
    }

    /// <summary>Точка у края экрана, чуть за пределами видимой области.</summary>
    private Vector3 RandomEdgePoint()
    {
        Camera camera = Camera.main;
        float halfHeight = 5f;
        float halfWidth = 8.9f;

        if (camera != null && camera.orthographic)
        {
            halfHeight = camera.orthographicSize;
            halfWidth = halfHeight * camera.aspect;
        }

        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float x = Mathf.Cos(angle) * (halfWidth + 0.7f);
        float y = Mathf.Sin(angle) * (halfHeight + 0.6f);

        return new Vector3(x, y, 0f);
    }

    private void CheckLevelFinished()
    {
        // Учитываем и убитых, и дошедших: иначе уровень мог зависнуть,
        // если все враги долетели до игрока, но ни один не был набран
        if (killedCount + reachedCount < info.enemyCount)
            return;

        intermissionTimer = intermissionSeconds;
        SetToast(info.isBoss ? "Босс уничтожен! Дальше будет сложнее." : "Уровень пройден!");
    }

    // ---------------------------------------------------------------- события врагов

    private void OnWordCompleted(Enemy enemy, int points)
    {
        score += points;

        if (enemy.IsBoss)
        {
            bossWordsDone++;
            int left = enemy.TotalWords - bossWordsDone;
            SetToast(left > 0 ? ("Слово принято! Осталось " + left) : "Босс добит!");
        }

        UpdateHud();
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        killedCount++;
        Remove(enemy);

        if (activeEnemy == enemy)
            activeEnemy = null;

        UpdateHud();
    }

    private void OnReachedPlayer(Enemy enemy)
    {
        health = Mathf.Max(0, health - 1);
        flashTimer = 0.4f;
        reachedCount++;

        Remove(enemy);
        enemy.Despawn();

        if (activeEnemy == enemy)
            activeEnemy = null;

        if (health <= 0)
        {
            GameOver();
            return;
        }

        SetToast("Враг дошёл до игрока! Жизней осталось: " + health);
        UpdateHud();
    }

    private void Remove(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    private void GameOver()
    {
        gameOver = true;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] != null)
                enemies[i].Frozen = true;
        }

        SetToast("<b>Игра окончена.</b> Счёт: " + score + "   Уровень: " + level +
                 "   Esc — меню паузы");

        RecordSession();
        UpdateHud();
    }

    // ---------------------------------------------------------------- ввод

    /// <summary>Публичный вход для символа (используется и клавиатурой, и тестами).</summary>
    public void HandleCharacter(char c)
    {
        if (gameOver || info == null)
            return;

        if (c == TypingTextModel.EnterChar || c == TypingTextModel.BackspaceChar)
            return;

        if (!sessionStarted)
        {
            sessionStarted = true;
            sessionStartedAt = Time.time;
        }

        if (activeEnemy == null)
            TrySelectTarget(c);
        else
            ProcessAgainstTarget(c);
    }

    private void TrySelectTarget(char c)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.Alive)
                continue;

            if (!enemy.CanBeSelected(c))
                continue;

            activeEnemy = enemy;
            enemy.SetHighlight(true);
            ProcessAgainstTarget(c);
            return;
        }

        // Ни одно слово не начинается с этой буквы — это ошибка набора
        CountKey(c, false);
    }

    private void ProcessAgainstTarget(char c)
    {
        Enemy enemy = activeEnemy;
        if (enemy == null || !enemy.Alive)
            return;

        char expected = enemy.Expected;
        EnemyStep step = enemy.Process(c, resetProgressOnMistake);

        if (step == EnemyStep.Correct)
            CountKey(expected, true);
        else if (step == EnemyStep.Wrong)
        {
            CountKey(expected, false);

            if (resetProgressOnMistake)
            {
                // Ошибка в порядке букв — цель сброшена, нужно начинать заново
                enemy.SetHighlight(false);
                activeEnemy = null;
            }
        }
        else if (step == EnemyStep.EnemyKilled)
        {
            activeEnemy = null;
        }
        else if (step == EnemyStep.WordFinished)
        {
            // Босс: следующее слово набираем сразу, цель остаётся прежней
            activeEnemy = enemy;
        }
    }

    private void CountKey(char expected, bool correct)
    {
        char key = char.IsLetter(expected) ? char.ToUpperInvariant(expected) : char.ToLowerInvariant(expected);

        attemptsByKey[key] = attemptsByKey.TryGetValue(key, out int attempts) ? attempts + 1 : 1;

        if (correct)
            correctKeystrokes++;
        else
        {
            errorsByKey[key] = errorsByKey.TryGetValue(key, out int errors) ? errors + 1 : 1;
            errorKeystrokes++;
        }
    }

    // ---------------------------------------------------------------- показатели

    private double SessionSeconds
    {
        get
        {
            if (!sessionStarted)
                return 0;

            double end = statsRecorded ? sessionEndedAt : Time.time;
            return Math.Max(1.0, end - sessionStartedAt);
        }
    }

    private double Cpm
    {
        get { return (correctKeystrokes + errorKeystrokes) / (SessionSeconds / 60.0); }
    }

    private double NetCpm
    {
        get { return Math.Max(0, (correctKeystrokes - errorKeystrokes) / (SessionSeconds / 60.0)); }
    }

    private double Accuracy
    {
        get
        {
            double total = correctKeystrokes + errorKeystrokes;
            return total <= 0 ? 1 : correctKeystrokes / total;
        }
    }

    private void RecordSession()
    {
        if (statsRecorded || !sessionStarted || correctKeystrokes + errorKeystrokes <= 0)
            return;

        sessionEndedAt = Time.time;

        var result = new TypingSessionResult
        {
            mode = "game",
            dateIso = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            seconds = SessionSeconds,
            correctKeystrokes = (int)correctKeystrokes,
            errorKeystrokes = (int)errorKeystrokes,
            textLength = (int)(correctKeystrokes + errorKeystrokes),
            cpm = Cpm,
            netCpm = NetCpm,
            wpm = Cpm / 5.0,
            netWpm = NetCpm / 5.0,
            accuracy = Accuracy
        };

        foreach (KeyValuePair<char, int> pair in attemptsByKey)
        {
            int errors = 0;
            errorsByKey.TryGetValue(pair.Key, out errors);

            result.keys.Add(new KeyStat
            {
                key = pair.Key.ToString(),
                attempts = pair.Value,
                errors = errors
            });
        }

        StatsStore.AddSession(result);
        statsRecorded = true;
    }

    private void UpdateHud()
    {
        if (scoreText != null)
            scoreText.text = "Очки: " + score;

        if (scoreBar != null)
            scoreBar.fillAmount = Mathf.Clamp01((score % scorePerRank) / (float)scorePerRank);

        float progress = 0f;
        string remaining = string.Empty;

        if (info != null)
        {
            if (info.isBoss)
            {
                int totalWords = info.bossWordCount;
                progress = totalWords > 0 ? bossWordsDone / (float)totalWords : 0f;
                remaining = "Слов осталось: " + Mathf.Max(0, totalWords - bossWordsDone);
            }
            else
            {
                int resolved = killedCount + reachedCount;
                progress = info.enemyCount > 0 ? resolved / (float)info.enemyCount : 0f;
                remaining = "Врагов осталось: " + Mathf.Max(0, info.enemyCount - resolved);
            }
        }

        if (progressBar != null)
            progressBar.fillAmount = Mathf.Clamp01(progress);

        if (progressText != null)
            progressText.text = remaining;

        if (timerText != null)
        {
            float elapsed = Mathf.Max(0f, Time.time - levelStartedAt);
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        if (healthBar != null)
            healthBar.fillAmount = maxHealth > 0 ? health / (float)maxHealth : 0f;

        if (healthText != null)
            healthText.text = "Жизни: " + health + "/" + maxHealth;
    }

    private void SetToast(string message)
    {
        if (toastText != null)
            toastText.text = message;
    }
}
