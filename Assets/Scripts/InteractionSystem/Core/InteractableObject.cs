using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public Transform peepholeCameraPoint; // ponto onde a câmera vai se mover
    public Camera mainCamera;

    private bool isPeeking = false;
    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;

    public void Interact()
    {
        if (!isPeeking)
        {
            StartPeeking();
        }
        else
        {
            StopPeeking();
        }
    }

    void StartPeeking()
    {
        isPeeking = true;

        // guarda posição original
        originalCamPosition = mainCamera.transform.position;
        originalCamRotation = mainCamera.transform.rotation;

        // move a câmera pro ponto do olho mágico
        mainCamera.transform.position = peepholeCameraPoint.position;
        mainCamera.transform.rotation = peepholeCameraPoint.rotation;
    }

    void StopPeeking()
    {
        isPeeking = false;

        // volta a câmera pra posição original
        mainCamera.transform.position = originalCamPosition;
        mainCamera.transform.rotation = originalCamRotation;
    }
}
