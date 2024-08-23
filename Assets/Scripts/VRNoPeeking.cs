using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;

public class VRNoPeeking : MonoBehaviour
{
    [SerializeField] LayerMask collisionLayer;
    [SerializeField] float fadeSpeed;
    [SerializeField] float sphereCheckSize = .15f;
    [SerializeField] float teleportSphereCheckSize = .55f;
    [SerializeField] float threshold = 0.15f;
    //[SerializeField] OVRPlayerController avatarController;

    private Material CameraFadeMat;
    private bool isCameraFadedOut = false;
    private Vector3 peekingPosition;
    private TeleportationProvider teleportationProvider;
    private Transform _xrOrigin;
    private Vector3 startPosition;
    private Vector3 endPosition;

    private void Awake()
    {
        CameraFadeMat = GetComponent<Renderer>().material;
        _xrOrigin = transform.root;
        teleportationProvider = FindObjectOfType<TeleportationProvider>();
        if (teleportationProvider != null)
        {
            teleportationProvider.beginLocomotion += OnTeleportStart;
            teleportationProvider.endLocomotion += OnTeleportEnd;
        }
    }

    // Update is called once per frame
    void Update()
    { 
        GameObject locSystem = GameObject.Find("Locomotion System");
        TeleportationProvider tProvider = locSystem.GetComponent<TeleportationProvider>();
        ActionBasedSnapTurnProvider sProvider =locSystem.GetComponent<ActionBasedSnapTurnProvider>();

        if(Physics.CheckSphere(transform.position, sphereCheckSize, collisionLayer, QueryTriggerInteraction.Ignore)) 
        {
            if(!isCameraFadedOut)
            {
                isCameraFadedOut = true;
                //avatarController.EnableLinearMovement = false;
                //avatarController.EnableRotation = false;
                tProvider.enabled = false;
                //sProvider.enabled = false;

                StartCoroutine(FadeCamera(true, 1f));
                //CameraFade(1f);
                peekingPosition = transform.position;
            }
        }
        else
        {
            if(!isCameraFadedOut)
                return;

            float dist = Vector3.Distance(peekingPosition, transform.position);
            if(dist < threshold)
            {
                isCameraFadedOut = false;
                tProvider.enabled = true;
                //sProvider.enabled = true;
                //avatarController.EnableLinearMovement = true;
                //avatarController.EnableRotation = true;
                StartCoroutine(FadeCamera(false, 0f));
                //CameraFade(0f);
            }
                
        }
    }

    private void OnTeleportStart(LocomotionSystem locomotionSystem)
    {
        startPosition = _xrOrigin.position;
    }

    private void OnTeleportEnd(LocomotionSystem locomotionSystem)
    {
        endPosition = _xrOrigin.position;
        Vector3 direction = (endPosition - startPosition).normalized;
        while (Physics.CheckSphere(transform.position, teleportSphereCheckSize, collisionLayer, QueryTriggerInteraction.Ignore)) 
        {
            GameObject[] avatars = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject avatar in avatars)
            {
                UIManager uiManager = avatar.GetComponent<UIManager>();
                uiManager.ShowMessage($"{direction.ToString()}");
            }
            _xrOrigin.position = _xrOrigin.position - direction;
        }
    }

    public void FadeForTeleport(float delay)
    {
        //StartCoroutine(FadeCamera(false, 0f));

    }

    IEnumerator FadeCamera(bool FadedOut, float targetAlpha)
    {
        var fadeValue = Mathf.MoveTowards(CameraFadeMat.GetFloat("_AlphaValue"), targetAlpha, Time.deltaTime * fadeSpeed);
        while ((isCameraFadedOut && CameraFadeMat.GetFloat("_AlphaValue") <= targetAlpha) ||
                    (!isCameraFadedOut && CameraFadeMat.GetFloat("_AlphaValue") >= targetAlpha))
        {
            CameraFadeMat.SetFloat("_AlphaValue", fadeValue);
            fadeValue = Mathf.MoveTowards(CameraFadeMat.GetFloat("_AlphaValue"), targetAlpha, Time.deltaTime * fadeSpeed);

            yield return null;
        }
    }

    public void CameraFade(float targetAlpha)
    {
        var fadeValue = Mathf.MoveTowards(CameraFadeMat.GetFloat("_AlphaValue"), targetAlpha, Time.deltaTime * fadeSpeed);
        CameraFadeMat.SetFloat("_AlphaValue", fadeValue);

        if(fadeValue <= 0.01f)
            isCameraFadedOut = false;
    }
}
