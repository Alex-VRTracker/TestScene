using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine;

public class EnableOnLoad : MonoBehaviour
{
    /* VR Tracker
     * This script will enable the local player camera 
     * And disable the mesh for the local player
     */

    public bool enableOnLoad = false;

    public Renderer controllerMesh;

    protected virtual void Start()
    {

        if (transform.GetComponent<NetworkIdentity>() != null && !transform.GetComponent<NetworkIdentity>().isLocalPlayer)
        {
            Camera[] cams = GetComponentsInChildren<Camera>();
            //Desactivate camera of other player
            foreach (Camera cam in cams)
            {
                cam.enabled = false;
            }
            //Disable audio listener of other player
            if (this.GetComponentInChildren<AudioListener>() != null)
                this.GetComponentInChildren<AudioListener>().enabled = false;

            foreach (Transform child in transform)
            {
                //display all the component with tag buddy
                if (child.tag == "Body")
                {
                    child.gameObject.layer = 9; //See layer for the number
                    SetLayerToChildren(child.gameObject, 9);
                }

            }
        }
        else
        {
            transform.eulerAngles.Set(transform.eulerAngles.x, 0, transform.eulerAngles.z);

            Camera[] cams = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cams)
            {
                cam.enabled = true;
                cam.transform.eulerAngles.Set(cam.transform.eulerAngles.x, 0, cam.transform.eulerAngles.z);
            }
            if (this.GetComponentInChildren<AudioListener>() != null)
            {
                this.GetComponentInChildren<AudioListener>().enabled = true;
            }
            foreach (Transform child in transform)
            {
                //hide all the component with for the player buddy
                if (child.tag == "Body")
                {
                    child.gameObject.layer = 8; //See layer for the number
                    SetLayerToChildren(child.gameObject, 8);
                }

            }
        }

        //Disable mesh
        if (controllerMesh)
        {
			NetworkIdentity netId = transform.GetComponent<NetworkIdentity>();
            if ((netId != null && netId.isLocalPlayer))
            {
                controllerMesh.enabled = false;
            }
            else
            {
                controllerMesh.enabled = true;
            }
        }
    }

    /// <summary>
    /// Set the layer to the children object, this can be used to set head and body on different layer
    /// </summary>
    /// <param name="gObject">node where we want to start changing the layer number</param>
    /// <param name="layerNumber">The layer number</param>
    public void SetLayerToChildren(GameObject gObject, int layerNumber)
    {
        foreach (Transform trans in gObject.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = layerNumber;
        }
    }

    private void DisableUI()
    {
        if (transform.parent.GetComponent<NetworkIdentity>() != null && !transform.parent.GetComponent<NetworkIdentity>().isLocalPlayer)
        {
            foreach (Transform trans in gameObject.GetComponentsInChildren<Transform>())
            {
                if(trans.gameObject.layer == 5)
                {
                    trans.gameObject.SetActive(false);
                }
            }
        }
    }

}