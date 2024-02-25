/*==============================================================================
Copyright (c) 2021 PTC Inc. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other 
countries.
==============================================================================*/

using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Vuforia;
using UnityEngine.SceneManagement;

/// <summary>
/// A custom handler that implements the ITrackableEventHandler interface.
///
/// Changes made to this file could be overwritten when upgrading the Vuforia version.
/// When implementing custom event handler behavior, consider inheriting from this class instead.
/// </summary>
public class CustomObserverEventHandler : MonoBehaviour
{
    public enum TrackingStatusFilter
    {
        Tracked,
        Tracked_ExtendedTracked,
        Tracked_ExtendedTracked_Limited
    }

    /// <summary>
    /// A filter that can be set to either:
    /// - Only consider a target if it's in view (TRACKED)
    /// - Also consider the target if's outside of the view, but the environment is tracked (EXTENDED_TRACKED)
    /// - Even consider the target if tracking is in LIMITED mode, e.g. the environment is just 3dof tracked.
    /// </summary>
    public TrackingStatusFilter StatusFilter = TrackingStatusFilter.Tracked_ExtendedTracked_Limited;
    public UnityEvent OnTargetFound;
    public UnityEvent OnTargetLost;


    protected ObserverBehaviour mObserverBehaviour;
    protected TargetStatus mPreviousTargetStatus = TargetStatus.NotObserved;
    protected bool mCallbackReceivedOnce;

    public int Detection = 0; // Detected Means = 1; Not Detected = 0;
    public GameObject itemFX;
    public GameObject scanCanvas;
    public GameObject spawnPoint;

    private string currentScene;
    public string sceneName;
    public string trackname;

    protected virtual void Start()
    {
        mObserverBehaviour = GetComponent<ObserverBehaviour>();

        if (mObserverBehaviour)
        {
            mObserverBehaviour.OnTargetStatusChanged += OnObserverStatusChanged;
            mObserverBehaviour.OnBehaviourDestroyed += OnObserverDestroyed;

            OnObserverStatusChanged(mObserverBehaviour, mObserverBehaviour.TargetStatus);
        }

    }

    protected virtual void OnDestroy()
    {
        if (mObserverBehaviour)
            OnObserverDestroyed(mObserverBehaviour);
    }

    void OnObserverDestroyed(ObserverBehaviour observer)
    {
        mObserverBehaviour.OnTargetStatusChanged -= OnObserverStatusChanged;
        mObserverBehaviour.OnBehaviourDestroyed -= OnObserverDestroyed;
        mObserverBehaviour = null;
    }

    void OnObserverStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        var name = mObserverBehaviour.TargetName;
        if (mObserverBehaviour is VuMarkBehaviour vuMarkBehaviour && vuMarkBehaviour.InstanceId != null)
        {
            name += " (" + vuMarkBehaviour.InstanceId + ")";
        }

        Debug.Log($"Target status: { name } { targetStatus.Status } -- { targetStatus.StatusInfo }");

        HandleTargetStatusChanged(mPreviousTargetStatus.Status, targetStatus.Status);
        HandleTargetStatusInfoChanged(targetStatus.StatusInfo);

        mPreviousTargetStatus = targetStatus;
    }

    protected virtual void HandleTargetStatusChanged(Status previousStatus, Status newStatus)
    {
        var shouldBeRendererBefore = ShouldBeRendered(previousStatus);
        var shouldBeRendererNow = ShouldBeRendered(newStatus);
        if (shouldBeRendererBefore != shouldBeRendererNow)
        {
            if (shouldBeRendererNow)
            {
                OnTrackingFound();
            }
            else
            {
                OnTrackingLost();
            }
        }
        else
        {
            if (!mCallbackReceivedOnce && !shouldBeRendererNow)
            {
                // This is the first time we are receiving this callback, and the target is not visible yet.
                // --> Hide the augmentation.
                OnTrackingLost();
            }
        }

        mCallbackReceivedOnce = true;
    }

    protected virtual void HandleTargetStatusInfoChanged(StatusInfo newStatusInfo)
    {
        if (newStatusInfo == StatusInfo.WRONG_SCALE)
        {
            Debug.LogErrorFormat("The target {0} appears to be scaled incorrectly. " +
                                 "This might result in tracking issues. " +
                                 "Please make sure that the target size corresponds to the size of the " +
                                 "physical object in meters and regenerate the target or set the correct " +
                                 "size in the target's inspector.", mObserverBehaviour.TargetName);
        }
    }

    protected bool ShouldBeRendered(Status status)
    {
        if (status == Status.TRACKED)
        {
            // always render the augmentation when status is TRACKED, regardless of filter
            return true;
        }

        if (StatusFilter == TrackingStatusFilter.Tracked_ExtendedTracked && status == Status.EXTENDED_TRACKED)
        {
            // also return true if the target is extended tracked
            return true;
        }

        if (StatusFilter == TrackingStatusFilter.Tracked_ExtendedTracked_Limited &&
            (status == Status.EXTENDED_TRACKED || status == Status.LIMITED))
        {
            // in this mode, render the augmentation even if the target's tracking status is LIMITED.
            // this is mainly recommended for Anchors.
            return true;
        }

        return false;
    }

    protected virtual void OnTrackingFound()
    {

        StartCoroutine(ShowTrackedObject());

        /*if (mObserverBehaviour)
        {
            var rendererComponents = mObserverBehaviour.GetComponentsInChildren<Renderer>(true);
            var colliderComponents = mObserverBehaviour.GetComponentsInChildren<Collider>(true);
            var canvasComponents = mObserverBehaviour.GetComponentsInChildren<Canvas>(true);

            // Enable rendering:
            foreach (var component in rendererComponents)
                component.enabled = true;

            // Enable colliders:
            foreach (var component in colliderComponents)
                component.enabled = true;

            // Enable canvas':
            foreach (var component in canvasComponents)
                component.enabled = true;
        }

        OnTargetFound?.Invoke();*/
    }

    [Obsolete]
    protected virtual void OnTrackingLost()
    {
        Detection = 0;
        StopCoroutine(ShowTrackedObject());
        scanCanvas.SetActive(true);
        UnLoad(currentScene);

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        /* if (mObserverBehaviour)
         {
             var rendererComponents = mObserverBehaviour.GetComponentsInChildren<Renderer>(true);
             var colliderComponents = mObserverBehaviour.GetComponentsInChildren<Collider>(true);
             var canvasComponents = mObserverBehaviour.GetComponentsInChildren<Canvas>(true);

             // Disable rendering:
             foreach (var component in rendererComponents)
                 component.enabled = false;

             // Disable colliders:
             foreach (var component in colliderComponents)
                 component.enabled = false;

             // Disable canvas':
             foreach (var component in canvasComponents)
                 component.enabled = false;
         }

         OnTargetLost?.Invoke();*/
    }

    public void Load(string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    [Obsolete]
    public void UnLoad(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
            SceneManager.UnloadScene(sceneName);
    }

    IEnumerator ShowTrackedObject()
    {
        Detection = 1;
        
        GameObject clone = Instantiate(itemFX);
        clone.transform.SetParent(this.transform, false);
        scanCanvas.SetActive(false);
        yield return new WaitForSeconds(2f);

        if (Detection == 1)
        {
            Destroy(clone);            

            if (mObserverBehaviour.TargetName == trackname)
            {                
                currentScene = sceneName;
                Load(currentScene);
            }
        }        
    }    

}