using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScoringSystem : MonoBehaviour
    {
        public GameObject grid1;
        public GameObject grid2;
        public float detectionRadius = 0.5f;
        public TextMeshProUGUI scoreText;
        public GameObject scorePanel;
        private int score = 0;
        private int previousScore = 0;

        private GameObject objectToRelocate;
        //Game Over
        public GameObject gameOver;
        public TextMeshProUGUI gameOverText;
        private int GameOverMinutes;
        private int GameOverSeconds;

        public TextMeshProUGUI timerText;

        [SerializeField] private int initialMinutes = 1;
        [SerializeField] private int initialSeconds = 0;
    int busCount = 3; // Default value is 3
    private int remainingBuses = 0; // Track buses still to come

        private int minutes;
        private int seconds;
        private Coroutine countdownCoroutine;

        public GameObject Bus;
        private Animator busAnimator;

        private bool isBusLeaving = false;
        private bool isBusPresent = true;
        public NPCSpawner npcSpawner;

        public GameObject notificationPanel; // Reference to the NotificationPanel
        public GameObject notificationPrefab; // Prefab for the notification message
        public TextMeshProUGUI NotificationText;

        public ScoreRawImageController scoreRawImageController; // Reference to the ScoreRawImageController script


        private Placement placementSystem;

        private List<(ObjectData SeatedObject, GameObject ObjectGameObject, Vector3Int Position)> seatedObjects = new List<(ObjectData, GameObject, Vector3Int)>();

        public TextMeshProUGUI TotalBusStopsText;
        public TextMeshProUGUI CurrentStationText;

        public Button SkipStationButton;

    
    public void OnSkipStation()
        {
        seconds = 1;
        Debug.Log("Skipped station, seconds reset to 1");
    }

    public void AddNotification(string message)
        {
            if (notificationPanel == null || notificationPrefab == null)
            {
                Debug.LogError("NotificationPanel or NotificationPrefab is not assigned.");
                return;
            }
            NotificationText.text = message.ToString();

        }

        // New method to set bus count from OpenAI
        public void SetBusCount(int count)
        {
            busCount = count;
            remainingBuses = busCount;
            initialSeconds = 30; // Set the initial seconds to 10
        Debug.Log($"Bus count set to {busCount}. Total time: {initialMinutes}:{initialSeconds}");
        }

        public void Start()
        {
        if (SkipStationButton != null)
        {
            SkipStationButton.onClick.AddListener(OnSkipStation);
        }
        score = 0;

        // Initialize the first bus route
        SharedGameData.SelectRandomBusRoute();
        SetBusCount(SharedGameData.BusCount);

        // Update UI with bus number and current stop
        TotalBusStopsText.text = "0/" + SharedGameData.CurrentRoute.Count;
        CurrentStationText.text = $"Bus {SharedGameData.CurrentBusNumber}: {SharedGameData.CurrentRoute[0]}";

        if (Bus != null)
            {
                busAnimator = Bus.GetComponent<Animator>();
            }


            // Initialize timer values
            minutes = initialMinutes;
            seconds = initialSeconds;

            countdownCoroutine = StartCoroutine(CountdownTimer());

            placementSystem = FindFirstObjectByType<Placement>();
        }

        private IEnumerator CountdownTimer()
        {
            while (true)
            {
                // Update the timer text
                timerText.text = $"Shift ends in: {minutes:00}:{seconds:00}";

                yield return new WaitForSeconds(1);

                // Countdown the time
                seconds--;
                if (seconds < 0)
                {
                    seconds = 59;
                    minutes--;
                    if (minutes < 0)
                    {
                        if (remainingBuses > 0)
                        {
                            remainingBuses--;
                            minutes = initialMinutes;
                            seconds = 20; // Add 20 seconds for the next bus
                            
                            if (busAnimator != null && isBusPresent && !isBusLeaving)
                            {
                                SharedGameData.CurrentStopIndex++;
                                if (SharedGameData.CurrentStopIndex >= SharedGameData.CurrentRoute.Count)
                                {
                                    // Select a new random bus route when current route is complete
                                    SharedGameData.SelectRandomBusRoute();
                                }
                                
                                StartCoroutine(HandleBusDeparture());
                                TotalBusStopsText.text = $"{SharedGameData.CurrentStopIndex}/{SharedGameData.CurrentRoute.Count}";
                                CurrentStationText.text = $"Bus {SharedGameData.CurrentBusNumber}: {SharedGameData.CurrentRoute[SharedGameData.CurrentStopIndex]}";
                            }
                        }
                        else
                        {
                            // No more buses, trigger Game Over
                            HandleGameOver();
                            yield break;
                        }
                    }
                }

                if (npcSpawner != null)
                {
                    npcSpawner.RestoreNPCCount();
                }
            }
        }


        private void HandleGameOver()
        {
            // Stop all game activity
            Time.timeScale = 0;

            // Display the Game Over panel
            if (gameOver != null)
            {
                gameOver.SetActive(true);
            }

            // Set the Game Over text
            if (gameOverText != null)
            {
                gameOverText.text = "High score is " + score;
            }

            Debug.Log("Game Over triggered. High score is 100.");
        }

        private IEnumerator HandleBusDeparture()
        {
            isBusLeaving = true;
            isBusPresent = false;

            // Hide preview when bus is leaving
            if (placementSystem != null)
            {
                placementSystem.StopPlacement();
                placementSystem.enabled = false;
            }

            busAnimator.ResetTrigger("IsComing");
            busAnimator.SetTrigger("IsLeaving");

            float leavingAnimationDuration = 1f;
            yield return new WaitForSeconds(leavingAnimationDuration);

            busAnimator.ResetTrigger("IsLeaving");
            isBusLeaving = false;

            float delayBetweenBuses = 0f;
            yield return new WaitForSeconds(delayBetweenBuses);

            ClearAllObjects();

            // Respawn NPCs after bus arrives back
            busAnimator.SetTrigger("IsComing");

            float comingAnimationDuration = 1f;
            yield return new WaitForSeconds(comingAnimationDuration);

            busAnimator.ResetTrigger("IsComing");
            isBusPresent = true;

            // Re-enable placement system when bus arrives
            if (placementSystem != null)
            {
                placementSystem.enabled = true;
            }
        }



        private void ClearAllObjects()
        {

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            minutes = initialMinutes;
            seconds = initialSeconds;

            countdownCoroutine = StartCoroutine(CountdownTimer());

            if (placementSystem != null)
            {
                placementSystem.ClearAllPlacedObjects();
            }

            if (Bus != null)
            {
                foreach (Transform child in Bus.transform)
                {
                    if (child.CompareTag("Character"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        public void OnCharacterSeated(ObjectData seatedObject, GameObject objectGameObject, Vector3 position)
        {
            Vector3Int objectPositionInt = GetIntegerPosition(position);
            // Pass `seatedObject` directly instead of trying to retrieve it later
            seatedObjects.Add((seatedObject, objectGameObject, objectPositionInt));

            CheckPosition(seatedObject, objectPositionInt);
            CheckForNeighbors(seatedObject, objectGameObject, objectPositionInt);

            score = Mathf.Max(score, 0);
        }


        private void CheckPosition(ObjectData newObject, Vector3Int newPosition)
        {
            // Store the previous score before changes
            int oldScore = score;
            // Position preference handling
            // Adult seat preferences
            if (newObject.type == "adult" &&
                ((newPosition.x == 7 || newPosition.x == 12) && (newPosition.y == 10 || newPosition.y == 8) ||
                 newPosition.z <= 26))
            {
                score += 10;
                scoreRawImageController.ShowScoreUpImage();

            }

            // Elder seat preferences
            if (newObject.type == "elder" &&
                (newPosition.z >= 29 || (newPosition.x == 9 && (newPosition.y == 10 || newPosition.y == 8))))
            {
                score += 10;
                scoreRawImageController.ShowScoreUpImage();

            }

            // Kid or student preferences
            if ((newObject.type == "kid" || newObject.type == "student") &&
                ((newPosition.y == 10 || newPosition.y == 8) && newPosition.z == 28 || newPosition.z <= 26))
            {
                score += 10;
                scoreRawImageController.ShowScoreUpImage();

            }

            // Police preferences
            if (newObject.type == "police" && newPosition.z <= 26)
            {
                score += 10;
                scoreRawImageController.ShowScoreUpImage();

            }


            DisplayScorePanel(score);

            // Update the previous score
            previousScore = score;
        }

        private void CheckForNeighbors(ObjectData newObject, GameObject newObjectGameObject, Vector3Int newPosition)
        {
            Vector3Int[] directions = {
        Vector3Int.left, Vector3Int.right, Vector3Int.back, Vector3Int.forward
    };

            List<(ObjectData, GameObject, Vector3Int)> neighborsToRemove = new List<(ObjectData, GameObject, Vector3Int)>();

            foreach (var direction in directions)
            {
                Vector3Int neighborPosition = newPosition + direction;

                foreach (var existingObject in seatedObjects)
                {
                    if (existingObject.Position == neighborPosition)
                    {
                        switch (existingObject.SeatedObject.type)
                        {
                            // Elder seated next to elder
                            case "elder" when newObject.type == "elder":
                                score += 5;
                                AddNotification("Granma Reunion! +5!");
                                break;

                            // Police seated next to elder
                            case "police" when newObject.type == "elder":
                                score += 20;
                                AddNotification("Order restored! Police love elders. +20!");
                                break;

                            // Kid seated next to elder (relocate the kid)
                            case "kid" when newObject.type == "elder":
                                StartTimer(existingObject.ObjectGameObject, existingObject.SeatedObject, Random.Range(0, 1));
                                Destroy(existingObject.ObjectGameObject);
                                neighborsToRemove.Add((newObject, newObjectGameObject, newPosition));
                                score -= 20;
                                AddNotification("Kid annoyed by elder stories. -20!");
                                break;

                            // Student seated next to elder
                            case "student" when newObject.type == "elder":
                                int delayChance = Random.Range(0, 5);
                                if (delayChance >= 2)
                                {
                                    Destroy(existingObject.ObjectGameObject);
                                    neighborsToRemove.Add((newObject, newObjectGameObject, newPosition));
                                    AddNotification("Elder told the student to find another seat. -15!");
                                }
                                else
                                {
                                    StartTimer(existingObject.ObjectGameObject, existingObject.SeatedObject, Random.Range(2, 3));
                                    AddNotification("Elder scolded the student! -15!");
                                }
                                score -= 15;
                                break;

                            // Elder seated next to adult
                            case "elder" when newObject.type == "adult":
                                StartTimer(existingObject.ObjectGameObject, existingObject.SeatedObject, Random.Range(0, 1));
                                score += 15;
                                AddNotification("Adult is charmed by elder wisdom! +15!");
                                break;

                            // Police seated next to kid
                            case "police" when newObject.type == "kid":
                                StartTimer(existingObject.ObjectGameObject, existingObject.SeatedObject, Random.Range(0, 1));
                                score -= 5;
                                AddNotification("Kid feels under surveillance! -5!");
                                break;

                            // Student seated next to police
                            case "student" when newObject.type == "police":
                                StartTimer(existingObject.ObjectGameObject, existingObject.SeatedObject, Random.Range(2, 3));
                                Destroy(existingObject.ObjectGameObject);
                                neighborsToRemove.Add((newObject, newObjectGameObject, newPosition));
                                score -= 20;
                                AddNotification("Police busted the student! -20!");
                                break;

                                // Add more specific cases here as needed
                        }
                    }
                }
            }

            // Check if the score increased or decreased
            if (score > previousScore)
            {
                scoreRawImageController.ShowScoreUpImage();
            }
            else if (score < previousScore)
            {
                scoreRawImageController.ShowScoreDownImage();
            }

            foreach (var neighbor in neighborsToRemove)
            {
                seatedObjects.Remove(neighbor);
            }

            DisplayScorePanel(score);
            previousScore = score;

        }



        private Vector3 GetRandomAdjacentPosition(Vector3 position)
        {
            // Define possible directions to find adjacent positions (left, right, up, down, forward, back)
            Vector3[] directions = {
        Vector3.left, Vector3.right, Vector3.forward, Vector3.back, Vector3.up, Vector3.down
    };

            // Pick a random direction
            Vector3 randomDirection = directions[Random.Range(0, directions.Length)];

            // Return the new adjacent position
            return position + randomDirection;
        }


        private Vector3Int GetIntegerPosition(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x),
                Mathf.RoundToInt(position.y),
                Mathf.RoundToInt(position.z)
            );
        }

        private void StartTimer(GameObject existingObject, ObjectData objectData, int timer)
        {
            objectToRelocate = existingObject;
            Invoke(nameof(RelocateStoredObject), timer);
        }

        private void RelocateStoredObject()
        {
            if (objectToRelocate != null)
            {
                // Ensure grids are properly configured
                if (grid1 == null || grid2 == null)
                {
                    Debug.LogWarning("Grids are not assigned. Cannot relocate object.");
                    return;
                }

                // Find the matching ObjectData in seatedObjects
                var objectDataEntry = seatedObjects.Find(item => item.ObjectGameObject == objectToRelocate);
                if (objectDataEntry.Equals(default((ObjectData, GameObject, Vector3Int))))
                {
                    Debug.LogError("Failed to find ObjectData for relocation.");
                    return;
                }

                GameObject selectedGrid = Random.value > 0.5f ? grid1 : grid2;
                Vector3 newPosition = GetRandomPositionWithinGridBounds(selectedGrid);
                seatedObjects.RemoveAll(item => item.ObjectGameObject == objectToRelocate);
                Destroy(objectToRelocate);
                GameObject newObject = Instantiate(objectToRelocate, newPosition, Quaternion.identity);

                newObject.transform.SetParent(Bus.transform);
                seatedObjects.Add((objectDataEntry.SeatedObject, newObject, GetIntegerPosition(newPosition)));

                Debug.Log($"Object relocated to {newPosition}");
            }
        }




        private Vector3 GetRandomPositionWithinGridBounds(GameObject grid)
        {
            Renderer gridRenderer = grid.GetComponent<Renderer>();

            // Ensure the grid has a renderer to determine its bounds
            if (gridRenderer == null)
            {
                Debug.LogError("Grid does not have a Renderer component.");
                return Vector3.zero;
            }

            Bounds bounds = gridRenderer.bounds;

            // Generate a random position within the grid's bounds
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = bounds.min.y; // Assuming the grid is flat on Y-axis
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            return new Vector3(randomX, randomY, randomZ);
        }


        private void DisplayScorePanel(int score)
        {
            scoreText.text = score.ToString();
        }

        public int RemainingBuses => remainingBuses;

        public void AddScore(int points)
        {
            score += points;
            DisplayScorePanel(score);
            if (points > 0)
                scoreRawImageController.ShowScoreUpImage();
            else
                scoreRawImageController.ShowScoreDownImage();
        }
    }
