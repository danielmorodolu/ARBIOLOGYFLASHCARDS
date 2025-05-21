using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI factText; // Reference to the Fact Text UI element (will be FactTextNew)
    public Button showFactButton; // Reference to Show Fact Button
    public Button newSessionButton; // Reference to New Session Button
    public Button takeQuizButton; // Reference to Take Quiz Button
    public GameObject quizPanel; // Reference to the Quiz Panel
    public TextMeshProUGUI questionText; // Reference to the Question Text UI element
    public Button[] answerButtons; // Array of 4 answer buttons
    public TextMeshProUGUI scoreText; // Reference to the Score Text UI element
    public Button nextQuestionButton; // Button to proceed to the next question
    public TextMeshProUGUI feedbackText; // Text to display feedback (Correct/Incorrect)
    public Button restartQuizButton; // Button to restart the quiz
    public Button returnButton; // Button to return to the main screen
    public AudioClip buttonClickSound; // Audio clip for button clicks

    private ImageTracking imageTracking;
    private string currentImageName = "";
    private int factIndex = 0;
    private bool isFactVisible = false;
    private bool isQuizActive = false;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private int totalQuestions = 0;
    private List<int> questionOrder;
    private List<ScoreData> allScores = new List<ScoreData>(); // Store all scores in memory
    private AudioSource audioSource; // AudioSource for playing sounds

    private readonly string[][] facts = new string[][]
    {
        new string[] { 
            "Animal cells lack a cell wall.", 
            "The nucleus controls all activities.", 
            "Mitochondria produce cell energy." 
        }, // animalcell.jpg
        new string[] { 
            "Fungi have a cell wall.", 
            "They reproduce using spores.", 
            "Can be multi- or unicellular organisms." 
        }, // fungicell.jpg
        new string[] { 
            "Plant cells have rigid cell walls.", 
            "Chloroplasts carry out photosynthesis.", 
            "Vacuoles store water and nutrients." 
        }, // plantcell.jpg
        new string[] { 
            "Bacteria are prokaryotic cells.", 
            "They lack a nucleus.", 
            "Some bacteria have protective capsules." 
        } // bacterialcell.jpg
    };

    private readonly string[][] incorrectAnswers = new string[][]
    {
        new string[] { "They have a rigid cell wall.", "They conduct photosynthesis.", "They lack mitochondria." }, // animalcell.jpg
        new string[] { "They lack a cell wall.", "They photosynthesize.", "They are always multicellular." }, // fungicell.jpg
        new string[] { "They lack a cell wall.", "They lack chloroplasts.", "They have a small vacuole." }, // plantcell.jpg
        new string[] { "They are eukaryotic.", "They have a true nucleus.", "They lack a capsule." } // bacterialcell.jpg
    };

    private const string SAVE_PATH = "/scores.json";

    private void Start()
    {
        Debug.Log("UIManager Start called");
        imageTracking = FindObjectOfType<ImageTracking>();
        if (imageTracking == null)
        {
            Debug.LogError("ImageTracking not found in the scene!");
        }

        // Initialize AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (buttonClickSound == null)
        {
            Debug.LogWarning("Button click sound not assigned in UIManager!");
        }

        // Validate Fact Text
        if (factText == null)
        {
            Debug.LogError("Fact Text is not assigned in the Inspector!");
            factText = GameObject.Find("FactTextNew")?.GetComponent<TextMeshProUGUI>();
            if (factText == null)
            {
                Debug.LogError("FactTextNew not found in the scene!");
                return;
            }
            else
            {
                Debug.Log($"Fact Text found: {factText.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"Fact Text assigned to: {factText.gameObject.name}");
        }

        // Check hierarchy and parent
        Debug.Log($"FactTextNew parent: {factText.transform.parent?.name}, Active in hierarchy: {factText.gameObject.activeInHierarchy}");
        if (!factText.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Fact Text GameObject is not active! Activating it.");
            factText.gameObject.SetActive(true);
        }

        if (!factText.enabled)
        {
            Debug.LogWarning("TextMeshProUGUI component on Fact Text is disabled! Enabling it.");
            factText.enabled = true;
        }

        // Validate Canvas
        Canvas canvas = factText.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (!canvas.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("Parent Canvas is not active! Activating it.");
                canvas.gameObject.SetActive(true);
            }
            if (!canvas.enabled)
            {
                Debug.LogWarning("Parent Canvas component is disabled! Enabling it.");
                canvas.enabled = true;
            }
            // Ensure Canvas is in Screen Space - Overlay
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning("Canvas Render Mode is not Screen Space - Overlay. Setting it.");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            // Set Canvas Scaler reference resolution
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                Debug.Log("Canvas Scaler set to 1080x1920, match 0.5");
            }
            // Set sorting order
            canvas.sortingOrder = 10;
            Debug.Log($"Canvas sorting order set to: {canvas.sortingOrder}");
        }

        // Validate Main Buttons
        if (showFactButton == null) Debug.LogError("Show Fact Button is not assigned in the Inspector!");
        if (newSessionButton == null) Debug.LogError("New Session Button is not assigned in the Inspector!");
        if (takeQuizButton == null) Debug.LogError("Take Quiz Button is not assigned in the Inspector!");

        // Validate Quiz UI Elements
        bool quizSetupValid = true;
        if (quizPanel == null) { Debug.LogError("Quiz Panel is not assigned in the Inspector!"); quizSetupValid = false; }
        if (questionText == null) { Debug.LogError("Question Text is not assigned in the Inspector!"); quizSetupValid = false; }
        if (answerButtons == null || answerButtons.Length != 4) { Debug.LogError("Answer Buttons array is not properly set (must have 4 buttons)!"); quizSetupValid = false; }
        else { foreach (var btn in answerButtons) { if (btn == null) { Debug.LogError("One of the Answer Buttons is not assigned!"); quizSetupValid = false; } } }
        if (scoreText == null) { Debug.LogError("Score Text is not assigned in the Inspector!"); quizSetupValid = false; }
        if (nextQuestionButton == null) { Debug.LogError("Next Question Button is not assigned in the Inspector!"); quizSetupValid = false; }
        if (feedbackText == null) { Debug.LogError("Feedback Text is not assigned in the Inspector!"); quizSetupValid = false; }
        if (restartQuizButton == null) { Debug.LogError("Restart Quiz Button is not assigned in the Inspector!"); quizSetupValid = false; }
        if (returnButton == null) { Debug.LogError("Return Button is not assigned in the Inspector!"); quizSetupValid = false; }

        if (quizSetupValid)
        {
            quizPanel.SetActive(false);
            nextQuestionButton.gameObject.SetActive(false);
            restartQuizButton.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(false);
            foreach (Button btn in answerButtons)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => { PlayButtonClickSound(); OnAnswerSelected(btn); });
            }
            nextQuestionButton.onClick.RemoveAllListeners();
            nextQuestionButton.onClick.AddListener(() => { PlayButtonClickSound(); NextQuestion(); });
            restartQuizButton.onClick.RemoveAllListeners();
            restartQuizButton.onClick.AddListener(() => { PlayButtonClickSound(); RestartQuiz(); });
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(() => { PlayButtonClickSound(); ReturnToMain(); });
            showFactButton.onClick.RemoveAllListeners();
            showFactButton.onClick.AddListener(() => { PlayButtonClickSound(); ToggleFact(); });
            newSessionButton.onClick.RemoveAllListeners();
            newSessionButton.onClick.AddListener(() => { PlayButtonClickSound(); ClearFacts(); });
            takeQuizButton.onClick.RemoveAllListeners();
            takeQuizButton.onClick.AddListener(() => { PlayButtonClickSound(); StartQuiz(); });
        }
        else
        {
            Debug.LogWarning("Quiz UI setup failed due to unassigned elements. Quiz functionality will be disabled until fixed.");
        }

        // Adjust Fact Text position and size only if not manually set in Editor
        RectTransform factTextRect = factText.GetComponent<RectTransform>();
        if (factTextRect.anchoredPosition == Vector2.zero) // Default position, assume not set manually
        {
            factTextRect.anchorMin = new Vector2(0.5f, 1f); // Anchor to top center
            factTextRect.anchorMax = new Vector2(0.5f, 1f);
            factTextRect.pivot = new Vector2(0.5f, 0.5f);
            factTextRect.anchoredPosition = new Vector2(0, -50); // 50 units below the top
            factTextRect.sizeDelta = new Vector2(600, 200);
            factTextRect.localScale = Vector3.one; // Ensure scale is 1 to avoid scaling issues
            Debug.Log("Fact Text position set by script to (0, -50) as it was default.");
        }
        else
        {
            Debug.Log($"Fact Text position preserved from Editor: {factTextRect.anchoredPosition}");
        }

        // Ensure TextMeshProUGUI settings are correct
        factText.alpha = 1f; // Ensure text is fully opaque
        factText.raycastTarget = false; // Disable raycast target to avoid interaction issues
        factText.maskable = true; // Ensure it respects canvas masking if any

        // Ensure fact text styling is applied
        StyleFactText(); // Call StyleFactText to apply background and styling
        StyleAllTextElements();

        // Ensure FactTextNew is on top
        factText.transform.SetAsLastSibling();

        factText.text = "";
        LoadScores();
        Debug.Log($"Fact Text position: {factTextRect.anchoredPosition}, World Position: {factTextRect.position}, Active: {factText.gameObject.activeInHierarchy}, Component Enabled: {factText.enabled}, Alpha: {factText.alpha}, Text: '{factText.text}'");
    }

    private void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    public void ToggleFact()
    {
        Debug.Log("ToggleFact method called from button click");
        if (imageTracking == null)
        {
            Debug.LogWarning("ImageTracking reference is null, cannot toggle fact.");
            factText.text = "";
            return;
        }

        string imageName = imageTracking.GetLastTrackedImageName();
        Debug.Log($"ToggleFact called with imageName: '{imageName}', currentImageName: '{currentImageName}', isFactVisible: {isFactVisible}, factIndex: {factIndex}");

        if (string.IsNullOrEmpty(imageName))
        {
            Debug.LogWarning("Image name is empty from ImageTracking. No image tracked.");
            factText.text = "";
            currentImageName = "";
            isFactVisible = false;
            return;
        }

        if (currentImageName != imageName)
        {
            Debug.Log($"New image detected: {imageName}");
            currentImageName = imageName;
            factIndex = 0;
            isFactVisible = true;
        }
        else
{
    factIndex = (factIndex + 1) % GetFactCount(imageName);
    isFactVisible = true; // Always visible
}

        UpdateFactText();
    }

    public void ClearFacts()
    {
        Debug.Log("ClearFacts method called from button click");
        currentImageName = "";
        factIndex = 0;
        isFactVisible = false;
        factText.text = "";
        Debug.Log("Cleared facts.");
    }

    private void UpdateFactText()
    {
        Debug.Log($"UpdateFactText called, currentImageName: '{currentImageName}', isFactVisible: {isFactVisible}, factIndex: {factIndex}");
        
        GameObject backgroundObj = factText.transform.Find("Background")?.gameObject;

        if (string.IsNullOrEmpty(currentImageName) || !isFactVisible)
        {
            factText.text = "";
            if (backgroundObj != null) backgroundObj.SetActive(false); // Hide background
            Debug.Log("No image tracked or fact hidden, clearing Fact Text.");
            return;
        }

        int imageIndex = GetImageIndex(currentImageName);
        if (imageIndex >= 0 && factIndex < facts[imageIndex].Length)
        {
            factText.text = facts[imageIndex][factIndex];
            if (backgroundObj != null) 
            {
                backgroundObj.SetActive(true); // Show background
                Image backgroundImage = backgroundObj.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    backgroundImage.color = new Color(0f, 0f, 0f, 0.7f); // Ensure background is visible
                }
            }
            Debug.Log($"Updated Fact Text to: '{factText.text}'");
        }
        else
        {
            factText.text = "";
            if (backgroundObj != null) backgroundObj.SetActive(false); // Hide background
            Debug.LogWarning($"No facts available for image: {currentImageName}, index: {imageIndex}");
        }

        if (!factText.gameObject.activeInHierarchy)
            factText.gameObject.SetActive(true);
        if (!factText.enabled)
            factText.enabled = true;

        // Ensure visibility settings are correct
        factText.alpha = 1f;
        factText.transform.SetAsLastSibling(); // Ensure it's on top
        Debug.Log($"Fact Text visibility check - Alpha: {factText.alpha}, Position: {factText.GetComponent<RectTransform>().position}, Active: {factText.gameObject.activeInHierarchy}");
    }

    private void StyleFactText()
    {
        // Reset any existing components that might cause issues
        var existingOutline = factText.GetComponent<Outline>();
        if (existingOutline != null)
        {
            Destroy(existingOutline);
        }

        factText.color = Color.white;
        factText.fontSize = 40; // Reduced font size for better fit
        factText.fontStyle = FontStyles.Bold;
        factText.alignment = TextAlignmentOptions.Center; // Ensure text is centered
        factText.enableWordWrapping = true; // Ensure wrapping is enabled
        factText.overflowMode = TextOverflowModes.Overflow; // Ensure overflow is handled

        var outline = factText.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4, -4);

        // Remove any existing background to avoid conflicts
        var oldBackground = factText.transform.Find("Background");
        if (oldBackground != null)
        {
            Destroy(oldBackground.gameObject);
        }

        // Create a child GameObject for the background
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(factText.transform, false);
        RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.offsetMin = new Vector2(-10, -10);
        bgRect.offsetMax = new Vector2(10, 10);

        Image background = backgroundObj.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.7f);
        background.raycastTarget = false;

        backgroundObj.transform.SetAsFirstSibling();
        Debug.Log("Background created for FactTextNew");
    }

    private void StyleAllTextElements()
    {
        // Ensure consistent font and style across all TextMeshProUGUI elements
        TextMeshProUGUI[] allTextElements = new TextMeshProUGUI[]
        {
            factText, questionText, scoreText, feedbackText,
        };
        foreach (var btn in answerButtons)
        {
            if (btn != null)
            {
                var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) StyleTextElement(btnText);
            }
        }
        foreach (var text in allTextElements)
        {
            if (text != null) StyleTextElement(text);
        }
    }

    private void StyleTextElement(TextMeshProUGUI text)
    {
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;

        var outline = text.gameObject.GetComponent<Outline>();
        if (outline == null) outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
    }

    public void StartQuiz()
    {
        Debug.Log("StartQuiz method called from button click");
        if (imageTracking == null)
        {
            Debug.LogWarning("ImageTracking reference is null, cannot start quiz.");
            return;
        }

        string imageName = imageTracking.GetLastTrackedImageName();
        if (string.IsNullOrEmpty(imageName))
        {
            Debug.LogWarning("No image tracked, cannot start quiz.");
            return;
        }

        currentImageName = imageName;
        int imageIndex = GetImageIndex(imageName);
        if (imageIndex < 0)
        {
            Debug.LogWarning($"Invalid image name: {imageName}, cannot start quiz.");
            return;
        }

        isQuizActive = true;
        factText.text = "";
        quizPanel.SetActive(true);
        score = 0;
        currentQuestionIndex = 0;
        totalQuestions = facts[imageIndex].Length;

        questionOrder = new List<int>();
        for (int i = 0; i < totalQuestions; i++)
        {
            questionOrder.Add(i);
        }
        for (int i = 0; i < questionOrder.Count; i++)
        {
            int temp = questionOrder[i];
            int randomIndex = Random.Range(i, questionOrder.Count);
            questionOrder[i] = questionOrder[randomIndex];
            questionOrder[randomIndex] = temp;
        }

        UpdateScoreText();
        DisplayQuestion();
    }

    private void DisplayQuestion()
    {
        if (!isQuizActive) return;

        int imageIndex = GetImageIndex(currentImageName);
        if (currentQuestionIndex >= totalQuestions)
        {
            EndQuiz();
            return;
        }

        int factIdx = questionOrder[currentQuestionIndex];
        string correctAnswer = facts[imageIndex][factIdx];
        questionText.text = GenerateQuestion(correctAnswer);

        List<string> answerOptions = new List<string> { correctAnswer };
        for (int i = 0; i < 3; i++)
        {
            answerOptions.Add(incorrectAnswers[imageIndex][i]);
        }

        for (int i = 0; i < answerOptions.Count; i++)
        {
            string temp = answerOptions[i];
            int randomIndex = Random.Range(i, answerOptions.Count);
            answerOptions[i] = answerOptions[randomIndex];
            answerOptions[randomIndex] = temp;
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = answerOptions[i];
            answerButtons[i].interactable = true;
        }

        feedbackText.text = "";
        nextQuestionButton.gameObject.SetActive(false);
    }

    private string GenerateQuestion(string fact)
    {
        if (fact.Contains("lack a cell wall")) return "What do animal cells lack?";
        if (fact.Contains("nucleus controls")) return "What does the nucleus control?";
        if (fact.Contains("Mitochondria")) return "What do mitochondria produce?";
        if (fact.Contains("Fungi have")) return "What do fungi cells have?";
        if (fact.Contains("reproduce using")) return "How do fungi reproduce?";
        if (fact.Contains("multi- or unicellular")) return "What forms can fungi take?";
        if (fact.Contains("rigid cell walls")) return "What do plant cells have?";
        if (fact.Contains("Chloroplasts")) return "What do chloroplasts do?";
        if (fact.Contains("Vacuoles")) return "What do vacuoles store?";
        if (fact.Contains("prokaryotic")) return "What type of cells are bacteria?";
        if (fact.Contains("lack a nucleus")) return "What do bacteria lack?";
        if (fact.Contains("protective capsules")) return "What do some bacteria have?";
        return "What is true about this cell?";
    }

    private void OnAnswerSelected(Button selectedButton)
    {
        string selectedAnswer = selectedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        int imageIndex = GetImageIndex(currentImageName);
        int factIdx = questionOrder[currentQuestionIndex];
        string correctAnswer = facts[imageIndex][factIdx];

        if (selectedAnswer == correctAnswer)
        {
            score++;
            feedbackText.text = "Correct!";
            feedbackText.color = Color.green;
        }
        else
        {
            feedbackText.text = $"Incorrect! The correct answer is: {correctAnswer}";
            feedbackText.color = Color.red;
        }

        UpdateScoreText();
        foreach (Button btn in answerButtons)
        {
            btn.interactable = false;
        }
        nextQuestionButton.gameObject.SetActive(true);
    }

    private void NextQuestion()
    {
        currentQuestionIndex++;
        DisplayQuestion();
    }

    private void EndQuiz()
    {
        questionText.text = "Quiz Complete!";
        feedbackText.text = $"Final Score: {score}/{totalQuestions}";
        feedbackText.color = Color.white;
        foreach (Button btn in answerButtons)
        {
            btn.gameObject.SetActive(false);
        }
        nextQuestionButton.gameObject.SetActive(false);
        restartQuizButton.gameObject.SetActive(true);
        returnButton.gameObject.SetActive(true);
        SaveScore();
    }

    private void UpdateScoreText()
    {
        scoreText.text = $"Score: {score}/{totalQuestions}";
    }

    private void RestartQuiz()
    {
        StartQuiz();
        foreach (Button btn in answerButtons)
        {
            btn.gameObject.SetActive(true);
        }
        restartQuizButton.gameObject.SetActive(false);
        returnButton.gameObject.SetActive(false);
    }

    private void ReturnToMain()
    {
        isQuizActive = false;
        quizPanel.SetActive(false);
        foreach (Button btn in answerButtons)
        {
            btn.gameObject.SetActive(true);
        }
        restartQuizButton.gameObject.SetActive(false);
        returnButton.gameObject.SetActive(false);
        ClearFacts();
    }

    private int GetImageIndex(string imageName)
    {
        Debug.Log($"Mapping image name: '{imageName}'");
        return imageName switch
        {
            "animalcell.jpg" => 0,
            "fungicell.jpg" => 1,
            "plantcell.jpg" => 2,
            "bacterialcell.jpg" => 3,
            _ => -1
        };
    }

    private int GetFactCount(string imageName)
    {
        int imageIndex = GetImageIndex(imageName);
        return imageIndex >= 0 ? facts[imageIndex].Length : 0;
    }

    [System.Serializable]
    private class ScoreDataWrapper
    {
        public List<ScoreData> scores;
    }

    private void SaveScore()
    {
        if (string.IsNullOrEmpty(currentImageName)) return;

        string path = Application.persistentDataPath + SAVE_PATH;
        ScoreData newScore = new ScoreData(currentImageName, score, totalQuestions);

        // Update or add the score
        bool scoreUpdated = false;
        for (int i = 0; i < allScores.Count; i++)
        {
            if (allScores[i].imageName == currentImageName)
            {
                if (score > allScores[i].highScore)
                {
                    allScores[i].highScore = score;
                    allScores[i].totalQuestions = totalQuestions;
                }
                scoreUpdated = true;
                break;
            }
        }

        if (!scoreUpdated)
        {
            allScores.Add(newScore);
        }

        // Serialize the entire list of scores
        ScoreDataWrapper wrapper = new ScoreDataWrapper { scores = allScores };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(path, json);
        Debug.Log($"Score saved to {path}: {json}");
    }

    private void LoadScores()
    {
        string path = Application.persistentDataPath + SAVE_PATH;

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ScoreDataWrapper wrapper = JsonUtility.FromJson<ScoreDataWrapper>(json);
            if (wrapper != null && wrapper.scores != null)
            {
                allScores = wrapper.scores;
                Debug.Log($"Scores loaded from {path}: {json}");
            }
            else
            {
                Debug.LogWarning($"Failed to deserialize scores from {path}, initializing empty scores.");
                allScores = new List<ScoreData>();
            }
        }
        else
        {
            Debug.LogWarning($"No score file found at {path}, initializing empty scores.");
            allScores = new List<ScoreData>();
        }
    }
}