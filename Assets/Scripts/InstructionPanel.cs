using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InstructionPanel : MonoBehaviour
{
    //[SerializeField] private Material mat;
    private bool showInstruction = false;
    public InputActionReference BButtonAction;
    private Renderer objRenderer;

     void Awake()
    {
        BButtonAction.action.performed += OnBButtonPressed;
        BButtonAction.action.Enable();
    }

    void onDestroy()
    {
        BButtonAction.action.performed -= OnBButtonPressed;
        BButtonAction.action.Disable();
    }

    private void OnBButtonPressed(InputAction.CallbackContext context)
    {
        showInstruction = !showInstruction;
        objRenderer.enabled = showInstruction;
        if (showInstruction)
        {
            Transform player = GameObject.Find("XR Origin (XR Rig) teleport").transform;
            Vector3 plPosition = player.position;
            transform.position = plPosition + new Vector3(0f, 1.5f, 0f) + player.forward * 1.5f;
            transform.rotation = Quaternion.LookRotation(player.forward, Vector3.up);
        }        
    }
    // Start is called before the first frame update
    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        objRenderer.enabled = showInstruction;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
