using UnityEngine;
using Normal.Realtime;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(UIManager))]
public class Player : RealtimeComponent<PlayerModel>
{
    [SerializeField] public CoinShape targetCoin = CoinShape.None;
    [SerializeField] private float walkingSpeed = 7.5f;
    [SerializeField] private float gravity = 20.0f;
    //[SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSpeed = 2.0f;
    [SerializeField] private float lookXLimit = 45.0f;
    [SerializeField] public Role currentRole = Role.None;
    [SerializeField] public TMP_Text roleText;
    [SerializeField] private GameObject batteryPrefab;
    [SerializeField] private Sprite crosshairSprite;
    [SerializeField] private float minWalkingSpeed = 1.0f;
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float coinLoad = 0.15f;
    [SerializeField] private float batteryLoad = 0.03f;
    [SerializeField] private float batteryRechargeTime = 2.0f;
    [SerializeField] public string layerToActivate = "Collector";

    private float batteryTimer = 0f;
    private RealtimeView realtimeView;
    private GameManager gameManager;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private bool canMove = true;
    private float currentEnergy;
    private bool isMoving = false;
    private bool isTacticalModeActive = false;

    public float CurrentEnergy => currentEnergy; // Expose current energy for the UI
    public float MaxEnergy => maxEnergy; // Expose max energy for the UI
    public int carryingBatteries { get; private set; } = 0; // Expose batteries for the UI
    public int carryingCoins { get; private set; } = 0;
    //[SerializeField] private int carryingBatteries = 3;
    [SerializeField] private const int MaxBatteries = 3;

    private UIManager uiManager; // Reference to the UIManager
    private Logger_new lg;

    //newly added variables for X Button
    private Player _player;
    private bool panelShow = true;
    private GameObject CanvasObj;
    public InputActionReference XButtonAction;

    void OnEnable()
    {
        XButtonAction.action.performed += OnXButtonPressed;
        XButtonAction.action.Enable();
    }

    void OnDisable()
    {
        XButtonAction.action.performed -= OnXButtonPressed;
        XButtonAction.action.Disable();
    }

    //changed the details of this function/method
    // HandleBatteryDrop
    private void OnXButtonPressed(InputAction.CallbackContext context)
    {
        if (currentRole == Role.Collector && carryingBatteries >= 1)
        {
            carryingBatteries--;
            lg.AddLine("Battery:drop");
            Vector3 spawnPosition = transform.position + transform.forward * 1.5f;
            spawnPosition.y += 0.8f;
            Realtime.Instantiate(batteryPrefab.name, spawnPosition, Quaternion.identity, new Realtime.InstantiateOptions { });

            GameObject locSystem = GameObject.Find("Locomotion System");
            TeleportationProvider tProvider = locSystem.GetComponent<TeleportationProvider>();
            ActionBasedSnapTurnProvider sProvider =locSystem.GetComponent<ActionBasedSnapTurnProvider>();

            float carryingLoad = (coinLoad * carryingCoins) + (batteryLoad * carryingBatteries);
            tProvider.delayTime = carryingLoad;
            sProvider.delayTime = carryingLoad;
            
            /* 
            OVRPlayerController playerCon = GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
            float carryingLoad = (0.1f * 0.15f * carryingCoins) + (0.1f * 0.03f * carryingBatteries); //10% a coin, 3% a battery
            playerCon.Acceleration = 0.1f - carryingLoad;

            //uiManager.UpdateBatteryRecharge();
            */
        }
    }

    private void panelActive(bool panelState)
    {
        CanvasObj.SetActive(panelState);
    }

    private void Start()
    {
        // roleText = GetComponentInChildren<TMP_Text>(); // 11/13
        realtimeView = GetComponent<RealtimeView>();
        characterController = GetComponent<CharacterController>();
        gameManager = FindObjectOfType<GameManager>();
        uiManager = GetComponent<UIManager>();
        roleText = GetComponentInChildren<TMP_Text>();
        lg = GetComponent<Logger_new>();

        if (realtimeView.isOwnedLocallyInHierarchy)
        {
            InitializePlayer();
            uiManager.Initialize(this, crosshairSprite, gameManager);
        }
        else
        {
            //playerCamera.gameObject.SetActive(false);
        }
    }

    protected override void OnRealtimeModelReplaced(PlayerModel previousModel, PlayerModel currentModel)
    {
        if (previousModel != null){
            previousModel.roleDidChange -= RoleDidChange;
        }

        if (currentModel != null){
            currentModel.roleDidChange += RoleDidChange;
            if (currentModel.isFreshModel) {
                currentModel.role = 0;
            }
        }
    }

    public int GetRole() {
        return (int)model.role;
    }
    
    public void EndTrial()
    {
        if (uiManager == null)
        {
            Debug.LogError("UIManager is null in EndTrial");
            return;
        }
        canMove = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        uiManager.DisplayTrialOverScreen();
    }


    private void InitializePlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetRole(Role.None);
        GetComponent<RealtimeTransform>().RequestOwnership();
        currentEnergy = maxEnergy;
    }

    void Update()
    {
        if (!realtimeView.isOwnedLocallyInHierarchy) return;

        UpdatePlayerModels();

        HandleInput();
        //HandleMovement();
        //HandleRotation();
        uiManager.UpdateUI();

        switch (currentRole)
        {
            case Role.Collector:
                //UpdateBatteryRecharge();
                SetCollectorVisibility();
                // HandleBatteryDrop();
                break;
            case Role.Explorer:
                //HandleEnergyConsumption();
                HideCollectorLayer();
                break;
            case Role.Tactical:
                HideCollectorLayer();
                break;
        }
        
    }

    private void UpdatePlayerModels()
    {
        if (model.role != (int)currentRole)
        {
            SetRole((Role)model.role);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!realtimeView.isOwnedLocallyInHierarchy) return;

        switch (currentRole)
        {
            case Role.Tactical:
                TriggerTactical(other);
                break;
            case Role.Explorer:
                PickUpBattery(other);
                break;
            case Role.Collector:
                boxEvents(other);
                break;
            default:
                Debug.Log("collide player");
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("TacticalControlTrigger"))
        {
            other.GetComponent<TacticalControlTrigger>().tacticalControl.IsTacticalModeActive = false;
            isTacticalModeActive = false;
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isTacticalModeActive)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
            //uiManager.SetCrosshairVisibility(Cursor.lockState == CursorLockMode.Locked);
        }
    }

    private void HandleMovement()
    {
        if (!canMove) return;

        float curSpeedX, curSpeedY;

        if (currentRole.Equals(Role.Explorer))
        {
            //float energyRatio = currentEnergy / maxEnergy;

            // does not decrease walking speed as long as energy is not gone.
            float energyRatio = 1;
            if (currentEnergy <= 1)
            {
                energyRatio = 1.0f / maxEnergy;
            }

            float scaledSpeed = Mathf.Lerp(minWalkingSpeed, walkingSpeed, energyRatio);

            curSpeedX = scaledSpeed * Input.GetAxis("Vertical");
            curSpeedY = scaledSpeed * Input.GetAxis("Horizontal");
        } else
        {
            curSpeedX = walkingSpeed * Input.GetAxis("Vertical");
            curSpeedY = walkingSpeed * Input.GetAxis("Horizontal");
        }
        
        if (characterController.isGrounded)
        {
            moveDirection = transform.TransformDirection(Vector3.forward * curSpeedX + Vector3.right * curSpeedY);
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (canMove)
        {
            rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            //playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.Rotate(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }


    private void TriggerTactical(Collider other)
    {
        if (other.CompareTag("TacticalControlTrigger") && currentRole.Equals(Role.Tactical))
        {
            //other.GetComponent<TacticalControlTrigger>().tacticalControl.AssignPlayerComponents(this, playerCamera);
            other.GetComponent<TacticalControlTrigger>().tacticalControl.IsTacticalModeActive = true;
            isTacticalModeActive = true;
            uiManager.UpdateRoleDependentUI();
        }
    }

    private void PickUpBattery(Collider other)
    {
        if (other.CompareTag("Battery") || (other.gameObject.name == "batteryBox"))
        {
            // Assuming batteries restore a fixed amount of energy
            float energyRestored = 10f*maxEnergy/20f; // Adds 10 secs. Adjust this value as needed
            currentEnergy = Mathf.Min(currentEnergy + energyRestored, maxEnergy);
            lg.AddLine("Battery:pickUp");
            
            if (other.CompareTag("Battery"))
            {
                // Assuming the battery should be destroyed after being picked up
                Destroy(other.gameObject);
            }

            Debug.Log("Picked up a battery. Energy restored.");
        }
    }

    public void HandleEnergyConsumption(Player player)
    {
        /*
        // Determine if the player is moving
        isMoving = characterController.velocity.magnitude > 0;
        
        // If moving, deplete energy
        if (isMoving)
        {
            // energy change based on time instead of movement
            currentEnergy = Mathf.Max(currentEnergy - Time.deltaTime*maxEnergy/45.0f, 1);
        
            //float energyRatio = currentEnergy / maxEnergy;
            //float scaledSpeed = Mathf.Lerp(minWalkingSpeed, walkingSpeed, energyRatio);

            //currentEnergy = Mathf.Max(currentEnergy - Time.deltaTime * scaledSpeed, 1); // Keep energy above 0 to avoid division by zero
        }

        // Handle energy reaching zero if needed

        if (currentEnergy <= 5)
        {
            // Perform any logic for when energy depletes (like disabling movement)
            GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>().Acceleration = 0.01f;
        }
        else if (currentEnergy <= 50)
        {
            OVRPlayerController playerCon = GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
            float currentAcceleration = playerCon.Acceleration;
            playerCon.Acceleration = currentEnergy/500f;
        }
        */
        player.currentEnergy -= 33;

    }

    private void boxEvents(Collider other)
    {
        if (other.gameObject.name == "coinBox")
        {
            carryingCoins = 0;

            GameObject locSystem = GameObject.Find("Locomotion System");
            TeleportationProvider tProvider = locSystem.GetComponent<TeleportationProvider>();
            ActionBasedSnapTurnProvider sProvider =locSystem.GetComponent<ActionBasedSnapTurnProvider>();

            float carryingLoad = (coinLoad * carryingCoins) + (batteryLoad * carryingBatteries);
            tProvider.delayTime = carryingLoad;
            sProvider.delayTime = carryingLoad;
            
/* 
            OVRPlayerController playerCon = GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
            float carryingLoad = (0.1f * 0.15f * carryingCoins) + (0.1f * 0.03f * carryingBatteries); //10% a coin, 3% a battery
            playerCon.Acceleration = 0.1f - carryingLoad;

 */
            lg.AddLine("dropCoins");
            lg.AddLine("carryingCoins:0");
        }
        else if (other.gameObject.name == "batteryBox")
        {
            if (carryingBatteries < MaxBatteries)
            {
                carryingBatteries ++;

                GameObject locSystem = GameObject.Find("Locomotion System");
                TeleportationProvider tProvider = locSystem.GetComponent<TeleportationProvider>();
                ActionBasedSnapTurnProvider sProvider =locSystem.GetComponent<ActionBasedSnapTurnProvider>();

                float carryingLoad = (coinLoad * carryingCoins) + (batteryLoad * carryingBatteries);
                tProvider.delayTime = carryingLoad;
                sProvider.delayTime = carryingLoad;
/* 
                OVRPlayerController playerCon = GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
                float carryingLoad = (0.1f * 0.15f * carryingCoins) + (0.1f * 0.03f * carryingBatteries); //10% a coin, 3% a battery
                playerCon.Acceleration = 0.1f - carryingLoad;
 */
                lg.AddLine("pickUpBattery");
                string line = $"carryingBatteries:{carryingBatteries}";
                lg.AddLine(line);
            }
        }
    }

    // now this is assigned to onXButton function
    
    private void HandleBatteryDrop()
    {
        if (currentRole == Role.Collector && (Input.GetKey(KeyCode.B) || OVRInput.GetUp(OVRInput.RawButton.X)) && carryingBatteries >= 1)
        {
            carryingBatteries--;
            lg.AddLine("Battery:drop");
            Vector3 spawnPosition = transform.position + transform.forward * 1.5f;
            spawnPosition.y += 0.8f;
            Realtime.Instantiate(batteryPrefab.name, spawnPosition, Quaternion.identity, new Realtime.InstantiateOptions { });

            GameObject locSystem = GameObject.Find("Locomotion System");
            TeleportationProvider tProvider = locSystem.GetComponent<TeleportationProvider>();
            ActionBasedSnapTurnProvider sProvider =locSystem.GetComponent<ActionBasedSnapTurnProvider>();

            float carryingLoad = (coinLoad * carryingCoins) + (batteryLoad * carryingBatteries);
            tProvider.delayTime = carryingLoad;
            sProvider.delayTime = carryingLoad;
/* 
            OVRPlayerController playerCon = GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
            float carryingLoad = (0.1f * 0.15f * carryingCoins) + (0.1f * 0.03f * carryingBatteries); //10% a coin, 3% a battery
            playerCon.Acceleration = 0.1f - carryingLoad;

           //uiManager.UpdateBatteryRecharge();
*/
        }
    }
    

    public void SetRole(Role newRole)
    {
        if (currentRole != newRole)
        {
            RoleDidChange(model, (int)newRole);
            currentRole = newRole;
            uiManager.UpdateRoleUI(currentRole);
            uiManager.SetEnergyBarVisibility(newRole == Role.Explorer);
            if (newRole == Role.Explorer)
            {
                currentEnergy = maxEnergy;
            }
        }
    }

    private void RoleDidChange(PlayerModel model, int value) {
        model.role = value;
        if (value == 0) {
            roleText.text = "No Role";
        } else if (value == 1) {
            roleText.text = "Collector";
        } else if (value == 2) {
            roleText.text = "Tactical";
        } else {
            roleText.text = "Explorer";
        }
        
    }

    public void collectCoin()
    {
        carryingCoins ++;

        GameObject locSystem = GameObject.Find("Locomotion System");
        TeleportationProvider tProvider = locSystem.GetComponent<TeleportationProvider>();
        ActionBasedSnapTurnProvider sProvider =locSystem.GetComponent<ActionBasedSnapTurnProvider>();

        float carryingLoad = (coinLoad * carryingCoins) + (batteryLoad * carryingBatteries);
        tProvider.delayTime = carryingLoad;
        sProvider.delayTime = carryingLoad;
/*         
        OVRPlayerController playerCon = GameObject.Find("OVRPlayerController").GetComponent<OVRPlayerController>();
        float carryingLoad = (0.1f * 0.15f * carryingCoins) + (0.1f * 0.03f * carryingBatteries); //10% a coin, 3% a battery
        playerCon.Acceleration = 0.1f - carryingLoad;
 */
    }
    
    public string GetFormattedGameTime()
    {
        return gameManager != null ? gameManager.GetFormattedGameTime() : "00:00";
    }


    public int GetCoinsCollected()
    {
        return 0;
    }

    public void SetCollectorVisibility()
    {
        /*
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                return;
        }
        int layerNumber = LayerMask.NameToLayer(layerToActivate);
        if (layerNumber == -1)
        {
            Debug.LogError($"Layer '{layerToActivate}' not found.");
            return;
        }
        int layerMask = 1 << layerNumber;
        playerCamera.cullingMask |= layerMask;
        */
    }

    public void HideCollectorLayer()
    {
        /*
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                return;
        }

        int layerNumber = LayerMask.NameToLayer(layerToActivate);
        if (layerNumber == -1)
            return;
        int layerMask = 1 << layerNumber;
        playerCamera.cullingMask &= ~layerMask;
        */
    }


    private void UpdateBatteryRecharge()
    {
        if (carryingBatteries < MaxBatteries)
        {
            batteryTimer += Time.deltaTime;
            if (batteryTimer >= batteryRechargeTime)
            {
                carryingBatteries++;
                batteryTimer = 0;
                uiManager.UpdateBatteryNumber();
            }
        }
    }

}
