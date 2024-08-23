using System;
using Normal.Realtime;
using UnityEngine;

public class StartTrigger : RealtimeComponent<StartTriggerModel>
{
    [SerializeField] private MazeStateSync mazeStateSync; // Reference to MazeSelect script
    [SerializeField] private RoleSelect roleSelect; // Reference to RoleSelect script
    [SerializeField] private GameObject door;       // The door GameObject to deactivate
    [SerializeField] public bool started = false;
    public event Action OnGameStarted;

    [SerializeField] private int totalPlayers = 0;
    [SerializeField] private int playersInTrigger = 0;

    //*total players to start the game:*//
    [SerializeField] private int requiredPlayers = 3; 

    private void Start()
    {
        door = transform.parent.gameObject;
        started = false;
    }

    protected override void OnRealtimeModelReplaced(StartTriggerModel previousModel, StartTriggerModel currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events on the previous model
            previousModel.startedDidChange -= StartedDidChange;
        }

        if (currentModel != null)
        {
            // If this is a model that has no data set on it, populate it with the current started value.
            if (currentModel.isFreshModel)
                currentModel.started = false;

            // Register for events so we'll know if the started property changes later
            currentModel.startedDidChange += StartedDidChange;
        }
    }

    private void StartedDidChange(StartTriggerModel model, bool started)
    {
        // Update the door based on the started value
        if (door != null)
        {
            //door.SetActive(!started);
            door.GetComponent<AutomaticDoor>().distanceChange(1.85f);
        }
        started = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInTrigger++;
            CheckStartConditions();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInTrigger--;
        }
    }

    void CheckStartConditions()
    {
        totalPlayers = GameObject.FindGameObjectsWithTag("Player").Length;
        // Check if the game is not started and all conditions are met to start the game
        if (!model.started && IsEveryoneReady())
        {
            StartGame();
        }
    }

    private bool IsEveryoneReady()
    {
        // Check if a maze has been selected
        //if (!mazeStateSync.IsMazeSelected())
        //    return false;

        // Check if all players have roles
        if (!roleSelect.AreAllRolesAssigned())
            return false;

        // Check if all players are within the trigger
        if (playersInTrigger < totalPlayers)
            return false;

        //*The number of players should be equal to the 3*//
        //if (playersInTrigger != requiredPlayers)
        //    return false;

        // All conditions are met
        return true;
    }

    private void StartGame()
    {
        model.started = true;
        started = true;
        door.GetComponent<AutomaticDoor>().distanceChange(1.85f);
        OnGameStarted?.Invoke();
    }

}
