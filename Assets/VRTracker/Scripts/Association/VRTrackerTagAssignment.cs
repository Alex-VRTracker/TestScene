using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.IO;

public class VRTrackerTagAssignment : MonoBehaviour
{
    /* VR Tracker
     * VR Tracker Tag Association will handle the assingnment step
     * It will also enable the auto assignation phase to skip the assingnment
     */

    public static VRTrackerTagAssignment instance;
    public string JsonFilePath = "Player_Data.json";
    [System.NonSerialized] public bool isWaitingForAssociation;
    public float assignationDelay = 10f; // Delay during which the User can press the red button on the Tag to assign it to one of its object in the game
    private JSONNode playerAssociation;
    [System.NonSerialized] public bool isAssociationLoaded;
    private Dictionary<string, bool> associatedMap; //Contains the association tag/object in the file
    private List<string> objectToAssign; // List of all object to assign from the player prefab
    private int numberOfAssociatedTag;
    private bool canSave;
    private List<string> availableTagMac;
    private float currentTime;


    //Call when script is loaded
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one VRTrackerTagAssignment in the scene");
        }
        else
        {
            instance = this;
            associatedMap = new Dictionary<string, bool>();
            objectToAssign = new List<string>();
            availableTagMac = new List<string>();
            canSave = false;
            assignationDelay = 10f;
            numberOfAssociatedTag = 0;
            isAssociationLoaded = false;
        }

    }

    // Use this for initialization
    void Start()
    {
        DontDestroyOnLoad(this);
    }


    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Update available tags
    /// </summary>
    public void AddAvailableTag(string uid)
    {
        availableTagMac.Add(uid);
    }


    private void Save()
    {
        string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);
        if (playerAssociation != null)
        {
            if (canSave)
            {
                string content = playerAssociation.ToString();
                System.IO.File.WriteAllText(filePath, content);
            }
        }
        else
        {
            Debug.Log("No Association to save");
        }
    }

    // Check if Tag association to User is saved in a file
    public bool LoadAssociation()
    {

        string filePath = Path.Combine(Application.persistentDataPath, JsonFilePath);

        if (File.Exists(filePath))
        {
            // Read the json from the file into a string
            string jsonDataString = File.ReadAllText(filePath);
            playerAssociation = JSON.Parse(jsonDataString);
            //updateAssociation ();
            return true;
        }
        else
        {
            Debug.LogWarning("Cannot load json file!");
        }
        return false;
    }

    // Try to assign the Tag UID from the Player file
    public bool TryAutoAssignTag()
    {

        Dictionary<VRTrackerTag, string> tagToAssign = new Dictionary<VRTrackerTag, string>();
        bool allTagAreInJSONList = true;

        // Check if the Tags to assign are all in the JSON file
        foreach (VRTrackerTag tag in VRTracker.instance.tags)
        {
            if (!tag.IDisAssigned)
            {

                bool tagFoundinJson = false;
                //foreach(KeyValuePair<string,string> jsonTag in playerAssociation){
                for (short i = 0; i < playerAssociation.Count; i++)
                {
                    if (playerAssociation.KeyAtIndex(i) == tag.tagType.ToString())
                    {
                        tagToAssign.Add(tag, playerAssociation[playerAssociation.KeyAtIndex(i)]);
                        tagFoundinJson = true;
                    }
                }
                if (!tagFoundinJson)
                    allTagAreInJSONList = false;; // If one of the tag is not present, false
            }
        }

        if (!allTagAreInJSONList)
        {
            Debug.LogWarning("Tag Association Error : Could not find all Tag in the JSON file");
            return false;
        }

        // Check if the Tags to assign are available in the Gateway
        bool allLinkFound = true;
        foreach (KeyValuePair<VRTrackerTag, string> tagUID in tagToAssign)
        {
            bool tagLinkFound = false;
            foreach (string mac in availableTagMac)
            {
                if (mac == tagUID.Value)
                    tagLinkFound = true;
            }
            if (!tagLinkFound)
                allLinkFound = false;
        }

        if (!allLinkFound)
        {
            Debug.LogWarning("Tag Association Error : Could not find all Tag on the Gateway");
            return false;
        }

        foreach (KeyValuePair<VRTrackerTag, string> tagUID in tagToAssign)
        {
            tagUID.Key.AssignTag(tagUID.Value);
        }
        return true;
    }


    /// <summary>
    /// Saves the tag association for the game for this device
    /// </summary>
    public void SaveAssociation()
    {

        foreach (VRTrackerTag tag in VRTracker.instance.tags)
        {
            if (tag.UID != "" && tag.UID != "Enter Your Tag UID")
            {
                //Store every tag association
                if (playerAssociation == null)
                {
                    playerAssociation = new JSONObject();
                }
                playerAssociation[tag.tagType.ToString()] = tag.UID;
                canSave = true;
            }
        }
        Save();
    }

}
