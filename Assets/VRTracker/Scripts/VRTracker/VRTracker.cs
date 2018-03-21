using UnityEngine;
using System.Collections.Generic;
using System;
using WebSocketSharp;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

public class VRTracker : MonoBehaviour {
    /* VR Tracker
     * This script is the main component of VR Tracker
     * It holds the websocket, and handle the communication and the treatment of the messages from the gateway
     */

    public static VRTracker instance;

	private WebSocket myws;
	private Vector3 position;
	private Vector3 orientation;
    private int timestamp = 0;
	protected Quaternion orientation_quat;

	[System.NonSerialized]public List<VRTrackerTag> tags;
	private List<string> messagesToSend;
	private bool connected = false;

	public string UserUID = "";
	public bool autoAssignation = true;
	public event Action OnConnected;
	public event Action OnDisconnected;
	[System.NonSerialized]public bool assignationComplete = false;
	[System.NonSerialized]public string serverIp = "";
	private bool serverIpReceived = false;
    //[System.NonSerialized]
    public bool isSpectator = false;

	public float RoomNorthOffset;

    public event Action OnAddTag;  // Called when a player is added with a tagtype head
    public event Action OnNewBoundaries;  // Called when new boundaries are set from dashboard
    [System.NonSerialized]
    public float xmin, xmax, ymin, ymax;

    private GameObject LocalPlayerReference;

    public enum TagType
    {
        Head, Gun, LeftController, RightController, LeftFoot, RightFoot
    }

    private void Awake()
	{
		if (instance != null)
		{
			Debug.LogError("More than one VRTracker in the scene");
		}
		else
		{
			instance = this;
		}

		// Initialize Unique User ID
		UserUID = SystemInfo.deviceUniqueIdentifier.ToLower();

		// Connect to Gateway
		openWebsocket ();
        tags = new List<VRTrackerTag>();

    }

    // Use this for initialization
    void Start () {

		DontDestroyOnLoad(this.gameObject);

	}

	// Update is called once per frame
	void Update () {

	}

	// Called when connection to Gateway is successfull
	private void OnOpenHandler(object sender, System.EventArgs e) {
		if(OnConnected != null)
			OnConnected ();

		connected = true;
		Debug.Log("VR Tracker : connection established");

		myws.SendAsync ("cmd=mac&uid="+UserUID, OnSendComplete);
        myws.SendAsync("cmd=allavailabletag", OnSendComplete);

        //Ask the server IP
        AskServerIP();

        foreach (VRTrackerTag tag in tags) {
			if(tag.UID != "Enter Your Tag UID")
				AssignTag(tag.UID);
		}

		getMagneticOffset ();
	}

    private void OnErrorHandler(object sender, System.EventArgs e)
    {
    
    }

    // Handler for all messages from the Gateway
    private void OnMessageHandler(object sender, MessageEventArgs e) {

//		Debug.Log (e.Data);
		if (e.Data.Contains ("cmd=position")) {
//			Debug.Log (System.DateTime.Now.Millisecond + ", " + e.Data);

			string[] datasbytag = e.Data.Split (new string[] { "&uid=" }, System.StringSplitOptions.RemoveEmptyEntries);
			for (int i = 1; i < datasbytag.Length; i++) {
				bool positionUpdated = false;
				bool orientationUpdated = false;
				bool orientationQuaternion = false;
				bool timestampUpdated = false;
				string[] datas = datasbytag [i].Split ('&');
				string uid = datas [0];
				foreach (string data in datas) {
					string[] datasplit = data.Split ('=');
					// Position
					if (datasplit [0] == "x") {
						positionUpdated = true;
						position.x = float.Parse (datasplit [1]);
					} else if (datasplit [0] == "z") {
						position.y = float.Parse (datasplit [1]);
					} else if (datasplit [0] == "y") {
						position.z = float.Parse (datasplit [1]);
					} else if (datasplit [0] == "ts") {
						timestamp = int.Parse (datasplit [1]);
						timestampUpdated = true;
					}

                    // Orientation
                    else if (datasplit [0] == "ox") {
						orientationUpdated = true;
						orientation.y = -float.Parse (datasplit [1]);
						orientation_quat.x = -orientation.y;
					} else if (datasplit [0] == "oy") {
						orientation.x = -float.Parse (datasplit [1]);
						orientation_quat.y = -orientation.x;
					} else if (datasplit [0] == "oz") {
						orientation.z = float.Parse (datasplit [1]);
						orientation_quat.z = orientation.z;
					} else if (datasplit [0] == "ow") {
						orientationQuaternion = true;
						orientation_quat.w = -float.Parse (datasplit [1]);
					}
				}
				foreach (VRTrackerTag tag in tags) {
					if (tag.UID == uid) {
						if (orientationUpdated) {
                            if (!tag.isOldTag)
                                tag.isOldTag = true;
							if (orientationQuaternion)
								tag.UpdateOrientationQuat (orientation_quat);
							else
								tag.UpdateOrientation (orientation);
						}
						if (positionUpdated) {
							if (!timestampUpdated)
								tag.UpdatePosition (position);
							else
								tag.UpdatePosition (position, timestamp);

						}
					}
				}
			}
		} else if (e.Data.Contains ("cmd=specialcmd")) {
			//	Debug.Log (e.Data);
			string[] datas = e.Data.Split ('&');
			string uid = null;
			string command = null;
			foreach (string data in datas) {
				string[] datasplit = data.Split ('=');

				// Tag UID sending the special command
				if (datasplit [0] == "uid") {
					uid = datasplit [1];
				}

				// Special Command name
				else if (datasplit [0] == "data") {
					command = datasplit [1];
				}
			}
			if (uid != null && command != null)
				ReceiveSpecialCommand (uid, command);

		} else if (e.Data.Contains ("cmd=tag")) { // Tag V2 data 
			string[] datas = e.Data.Split ('&');
			string uid = null;
			foreach (string data in datas) {
				string[] datasplit = data.Split ('=');

				// Tag UID sending the special command
				if (datasplit [0] == "uid") {
					uid = datasplit [1];
				}

			}
			if (uid != null)
				ReceiveSpecialData(uid, e.Data);
		}
		else if (e.Data.Contains ("cmd=taginfos")) {

			string[] datas = e.Data.Split ('&');

			string uid = null;
			string status = null;
			int battery = 0;

			foreach (string data in datas) {
				string[] datasplit = data.Split ('=');

				// Tag UID sending its informations
				if (datasplit [0] == "uid") {
					uid = datasplit [1];
				}
				// Tag status (“lost”, “tracking”, “unassigned”)
				else if (datasplit [0] == "status") {
					status = datasplit [1];
				}
				// Tag battery
				else if (datasplit [0] == "battery") {
					battery = int.Parse (datasplit [1]);
				}
			}
			if (uid != null && status != null) {
				foreach (VRTrackerTag tag in tags) {
					if (tag.UID == uid) {
						tag.status = status;
						tag.battery = battery;
					}
				}
			}

		} else if (e.Data.Contains ("cmd=error")) {
			// TODO Parse differnt kinds of errors
			Debug.LogWarning ("VR Tracker : " + e.Data);
			if (e.Data.Contains ("needmacadress")) {
				myws.SendAsync ("cmd=mac&uid=" + UserUID, OnSendComplete);
				foreach (VRTrackerTag tag in tags) {
					if (tag.UID != "Enter Your Tag UID")
						AssignTag (tag.UID);
				}
			}
		} else if (e.Data.Contains ("function=needaddress")) {
			ReceiveAskServerIP ();
		}
		//if the message gives us an IP address, try to connect as a client to it
		else if (e.Data.Contains ("function=address")) {

			string[] datas = e.Data.Split ('&');
			foreach (string data in datas) {
				string[] infos = data.Split ('=');
				if (infos [0] == "ip") {
					ReceiveServerIP (infos [1]);
				}
			}
		} else if (e.Data.Contains ("cmd=availabletag")) {
			Debug.Log ("Available tag message : " + e.Data);
			string[] datas = e.Data.Split ('&');


			// Verify if Tags connected to the system can be assoicated to the User from association File
			foreach (string data in datas) {
				string[] datasplit = data.Split ('=');
				if (datasplit [0].Contains ("tag")) {
					VRTrackerTagAssignment.instance.AddAvailableTag (datasplit [1]);
				}
			}
		} else if (e.Data.Contains ("cmd=reoriente")) {
			string uid = null;
			string[] datas = e.Data.Split ('&');

			foreach (string data in datas) {
				string[] datasplit = data.Split ('=');
				// Tag UID sending the special command
				if (datasplit [0] == "uid") {
					uid = datasplit [1];
				}
			}
			foreach (VRTrackerTag tag in tags) {
				if (tag.UID == uid) {
					Debug.Log ("Resetting orientation after receiving message");
					tag.ResetOrientation ();
				}
			}
		} else if (e.Data.Contains ("cmd=offset")) {
			Debug.LogWarning (e.Data);
			string[] datas = e.Data.Split ('&');

			foreach (string data in datas) {
				string[] datasplit = data.Split ('=');
				// Tag UID sending the special command
				if (datasplit [0] == "oy") {
					float f;

                    // Update rotation offset only if not null
                    if (float.TryParse(datasplit[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out f))
                        RoomNorthOffset = f+90;
				}
			}
		}
        else if (e.Data.Contains("cmd=boundaries"))
        {
            Debug.LogWarning(e.Data);


            string[] datas = e.Data.Split('&');

            foreach (string data in datas)
            {
                string[] datasplit = data.Split('=');
                // Tag UID sending the special command
                if (datasplit[0] == "xmin")
                {
                   xmin = float.Parse(datasplit[1]);
                }else if (datasplit[0] == "xmax")
                {
                    xmax = float.Parse(datasplit[1]);
                }else if (datasplit[0] == "ymin")
                {
                    ymin = float.Parse(datasplit[1]);
                }else if (datasplit[0] == "ymax")
                {
                   ymax = float.Parse(datasplit[1]);
                }
            }

            //Handle boundaries
            OnUpdateBoundaries();
        }

        else
		{
		//	Debug.Log("VR Tracker : Unknown data received : " + e.Data);
		}
	}

	// Called when connection to Gateway is closed
	private void OnCloseHandler(object sender, CloseEventArgs e) {
		connected = false;
		if(OnDisconnected != null)
			OnDisconnected ();

        Debug.Log("VR Tracker : connection closed for this reason: " + e.Reason);
	}

	private void OnSendComplete(bool success) {

	}

	/*
	 * Opens the websocket connection with the Gateway
	 */
	private void openWebsocket(){
		//Debug.Log("VR Tracker : opening websocket connection");
		myws = new WebSocket("ws://192.168.42.1:7777/user/");
		myws.OnOpen += OnOpenHandler;
		myws.OnMessage += OnMessageHandler;
		myws.OnClose += OnCloseHandler;
        myws.OnError += OnErrorHandler;
		myws.ConnectAsync();
	}

	/*
	 * Close the ebsocket connection to the Gateway
	 */
	private void CloseWebsocket(){
		connected = false;
		Debug.Log("VR Tracker : closing websocket connection");

		this.myws.Close();
	}


	/* 
	 * Send your Unique ID, it can be your MAC address for 
	 * example but avoid the IP. It will be used by the Gateway
	 * to identify you over the network. It is necessary on multi-gateway
	 * configuration 
	 */
	private void SendMyUID(string uid){
		myws.SendAsync (uid, OnSendComplete);

	}

	/* 
	 * Asks the gateway to assign a specific Tag to this device.  
	 * Assigned Tags will then send their position to this device.
	 */
	public void AssignTag(string TagID){
		myws.SendAsync ("cmd=tagassign&uid=" + TagID, OnSendComplete);
    }

	/* 
	 * Asks the gateway to assign a Tag to this device.  
	 * Assigned Tags will then send their position to this device.
	 */
	public void AssignATag(){
		myws.SendAsync ("cmd=AssignATag", OnSendComplete);
	}

	/* 
	 * Asks the gateway to UNassign a specific Tag from this device.  
	 * You will stop receiving updates from this Tag.
	 */
	public void UnassignTag(string TagID){
		myws.SendAsync("cmd=tagunassign&uid=" + TagID, OnSendComplete);
	}

	/* 
	 * Asks the gateway to UNassign all Tags from this device.  
	 * You will stop receiving updates from any Tag.
	 */
	public void UnassignAllTags(){
		myws.SendAsync("cmd=tagunassignall", OnSendComplete);
	}

	/* 
	 * Ask for informations on a specific Tag
	 */
	public void GetTagInformations(string TagID){
		myws.SendAsync("cmd=taginfos&uid=" + TagID, OnSendComplete);
	}

	/*
	 * Enable or Disable orientation detection for a Tag
	 */
	public void TagOrientation(string TagID, bool enable){
		string en = "";
		if (enable) {
			en = "true";
		} else {
			en = "false";
		}

		myws.SendAsync("cmd=orientation&orientation=" + en + "&uid=" + TagID, OnSendComplete);
	}

	/*
	 * Set a specific color on the Tag
	 * R (0-255)
	 * G (0-255)
	 * B (0-255)
	 */
	public void SetTagColor(string TagID, int red, int green, int blue){
		myws.SendAsync("cmd= color&r=" + red + "&g=" + green + "&b=" + blue + "&uid=" + TagID, OnSendComplete);
	}


	/* 
	 * Send special command to a Tag
	 */
	public void SendTagCommand(string TagID, string command){
		Debug.Log("VR Tracker : " + command);
		myws.SendAsync("cmd=specialcmd&uid=" + TagID + "&data=" + command, OnSendComplete);
	}

	/* 
	 * Send special command to the gateway that will be broadcast to all others users
	 */
	public void SendSpecialData(string command)
	{
		Debug.Log("VR Tracker : " + command);
		myws.SendAsync("cmd=specialdata&data="+ command, OnSendComplete);
	}


	/* 
	 * Send User device battery level to the Gateway
	 * battery (0-100)
	 */
	public void SendUserBattery(int battery){
		myws.SendAsync("cmd=usrbattery&battery=" + battery, OnSendComplete);
	}

	// For Multiplayer, we ask all other user if the know the Server IP
	public void AskServerIP(){
        myws.SendAsync("cmd=specialdata&function=needaddress", OnSendComplete);

	}

	public void SendServerIP(string ip){
		myws.SendAsync("cmd=specialdata&function=address&ip=" + ip, OnSendComplete);
	}

	// The server IP was sent to us by another user (typically the server)
	private void ReceiveServerIP(string ip){
		if(!serverIpReceived){
			serverIp = ip;
			serverIpReceived = true;
		}
	}

	// Another user is looking for the Server and asks if we know the IP
	private void ReceiveAskServerIP(){
		if (serverIp != "") {
			SendServerIP (serverIp);
		}
	}

	public void SendMessageToGateway(string message)
	{
		myws.SendAsync(message, OnSendComplete);
	}

	public static string LookForFunction(string message)
	{
		string[] datas = message.Split('&');
		foreach (string data in datas)
		{
			string[] infos = data.Split('=');
			if(infos[0] == "function")
			{
				return infos[1];
			}
		}
		return null;
	}

	/*
	 * Executed on reception of a special command 
	 */
	public void ReceiveSpecialCommand(string TagID, string data){
        // TODO: You can do whatever you wants with the special command, have fun !
		bool tagFound = false;
		// Search for the Tag the special command is sent to
		foreach (VRTrackerTag tag in tags)
		{
			if (tag.UID == TagID)
			{
				tagFound = true;
				tag.OnSpecialCommand(data);
			}
		}
		// If the Tag was not found, the command is sent to all Tags
		if (!tagFound) {
			foreach (VRTrackerTag tag in tags) {
				tag.OnSpecialCommandToAll (TagID, data);
			}
		}
		
	}



	/*
	 * Executed on reception of a special data 
	 */
	public void ReceiveSpecialData(string TagID, string data){
		// TODO: You can do whatever you wants with the special command, have fun !

		bool tagFound = false;
		// Search for the Tag the special command is sent to
		foreach (VRTrackerTag tag in tags)
		{
			if (tag.UID == TagID)
			{
				tagFound = true;
				tag.OnTagData(data);
			}
		}
	}

	/*
	 * Executed on reception of  tag informations
	 */
	public void ReceiveTagInformations(string TagID, string status, int battery){
		// TODO: You can do whatever you wants with the Tag informations
	}

	/* 
	 * Ensure the Websocket is correctly closed on application quit
	 */
	void OnApplicationQuit() {
		CloseWebsocket ();
        if (Network.connections.Length == 1)
        {
            //Disconnection to the server
            Debug.Log("Disconnecting: " + Network.connections[0].ipAddress + ":" + Network.connections[0].port);
            Network.CloseConnection(Network.connections[0], true);
        }
        else
        {
            if (Network.connections.Length == 0)
            {
                Debug.Log("No one is connected");
            }
            else
            {
                if (Network.connections.Length > 1)
                    Debug.Log("I'm a server, there is multiple connection");
            }

        }
    }

	public void AddTag(VRTrackerTag tag)
	{
		tags.Add(tag);
        if (OnAddTag != null)
            OnAddTag();
	}

    public VRTrackerTag GetTag(TagType type)
    {
        foreach (VRTrackerTag tag in tags)
            if (tag.tagType == type)
                return tag;
        Debug.LogWarning("Could not find a VR Tracker Tag with type " + type.ToString() + " in current Scene");
        return null;
    }

    public VRTrackerTag GetHeadsetTag()
    {
        foreach (VRTrackerTag tag in tags)
            if (tag.tagType == VRTracker.TagType.Head)
                return tag;
        Debug.LogWarning("Could not find a VR Tracker Tag with type " + VRTracker.TagType.Head.ToString() + " in current Scene");
        return null;
    }

    public VRTrackerTag GetLeftControllerTag()
    {
        foreach (VRTrackerTag tag in tags)
            if (tag.tagType == VRTracker.TagType.LeftController)
                return tag;
        Debug.LogWarning("Could not find a VR Tracker Tag with type " + VRTracker.TagType.LeftController.ToString() + " in current Scene");
        return null;
    }

    public VRTrackerTag GetRightControllerTag()
    {
        foreach (VRTrackerTag tag in tags)
            if (tag.tagType == VRTracker.TagType.RightController)
                return tag;
        Debug.LogWarning("Could not find a VR Tracker Tag with type " + VRTracker.TagType.RightController.ToString() + " in current Scene");
        return null;
    }

    public void RemoveTag(VRTrackerTag tag)
	{
		tags.Remove(tag);
	}

	public GameObject GetTagObject(string id)
	{
		foreach(VRTrackerTag tag in tags)
		{
			if(tag.UID == id)
			{
				return tag.gameObject;
			}
		}
		return null;
	}

	public bool IsAssigned()
	{
		return assignationComplete;
	}

	// Save the association between the Tag and each object for this user in a file on the PC/Phone
	public void SaveAssociationTagUser()
	{
		VRTrackerTagAssignment.instance.SaveAssociation ();
	}

	public void AskForServer(){
		AskServerIP ();
	}

	// Ask the gateway for the rotation offset between true magnetic North and room forward axis (Y in VR Tracker coordinates, Z in Unity coordinates)
	public void getMagneticOffset(){
		myws.SendAsync("cmd=getoffset", OnSendComplete);
	}
		
	public void SetLocalPlayer(GameObject player){
		LocalPlayerReference = player;
    }

	public GameObject GetLocalPlayer(){
		return LocalPlayerReference;
	}
    
    private void OnUpdateBoundaries()
    {
        if(OnNewBoundaries != null)
            OnNewBoundaries();
    }
}
