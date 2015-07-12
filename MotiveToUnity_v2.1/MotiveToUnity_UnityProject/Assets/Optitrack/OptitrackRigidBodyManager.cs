using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

//=============================================================================----
// Original Code Extended By Author: Bradley Newman - USC Worldbuilding Media Lab - worldbuilding.usc.edu
//
// Original Author: Copyright © NaturalPoint, Inc. All Rights Reserved.
// 
// This software is provided by the copyright holders and contributors "as is" and
// any express or implied warranties, including, but not limited to, the implied
// warranties of merchantability and fitness for a particular purpose are disclaimed.
// In no event shall NaturalPoint, Inc. or contributors be liable for any direct,
// indirect, incidental, special, exemplary, or consequential damages
// (including, but not limited to, procurement of substitute goods or services;
// loss of use, data, or profits; or business interruption) however caused
// and on any theory of liability, whether in contract, strict liability,
// or tort (including negligence or otherwise) arising in any way out of
// the use of this software, even if advised of the possibility of such damage.
//=============================================================================----

// This script will parse the rigid body tracking data coming from SlipStream.cs 
// and store the data in an array for OptiTrackRigidBody.cs to read from.
// Usage: 
// 1. Attach OptitrackRigidBodyManager.cs to an empty Game Object.
// 2. Optional: Create an empty game object to represent the origin and drag it to the "Origin" transform field in the inspector.
// This origin will allow you to offset the center of the tracking volume to be located somewhere other than the world origin at 0,0,0.
// Requirements:
// 1. Motive 1.7 Beta2
// 2. UnitySample.exe: this is a custom modified build of the NatNet SDK Sample that enables streaming up multiple rigid bodies instead of just one.
// 3. SlipStream.cs: script attached to another game object.

public class OptitrackRigidBodyManager : MonoBehaviour {
	public Transform origin;
	private static OptitrackRigidBodyManager _instance;
	private SlipStream SlipStreamComponent;
	private XmlDocument xmlDoc;
	[HideInInspector] public XmlNodeList rigidBodyList;
	[HideInInspector] public bool receivedFirstRigidBodyPacket = false;
	[HideInInspector] public int[] rigidBodyIDs;
	[HideInInspector] public Vector3[] rigidBodyPositions;
	[HideInInspector] public Quaternion[] rigidBodyQuaternions;

	#region Singleton Creation
	public static OptitrackRigidBodyManager instance
	{
		get
		{
			if(_instance == null)
			{
				_instance = GameObject.FindObjectOfType<OptitrackRigidBodyManager>();

				//Tell unity not to destroy this object when loading a new scene.
				DontDestroyOnLoad(_instance.gameObject);
			}

			return _instance;
		}
	}

	void Awake() 
	{
		if(_instance == null)
		{
			//If I am the first instance, make me the Singleton.
			_instance = this;
			DontDestroyOnLoad(this);
		}
		else
		{
			//If a Singleton already exists and you find another reference in scene, destroy it.
			if(this != _instance)
				Destroy(this.gameObject);
		}
	}
	#endregion

	void Start () 
	{
		xmlDoc= new XmlDocument();
		SlipStreamComponent = GameObject.FindObjectOfType<SlipStream>();
		SlipStreamComponent.PacketNotification += new PacketReceivedHandler(OnPacketReceived);

		if(origin == null) {
			origin = new GameObject("Origin").transform;
		}
	}

	// packet received
	void OnPacketReceived(object sender, string Packet)
	{
		xmlDoc.LoadXml(Packet);
		rigidBodyList = xmlDoc.GetElementsByTagName("Bone");

		//Initialize the rigid body data arrays once
		if(receivedFirstRigidBodyPacket == false) {
			for(int index=0; index<rigidBodyList.Count; index++)
			{
				rigidBodyIDs 			= new int[rigidBodyList.Count];
				rigidBodyPositions 		= new Vector3[rigidBodyList.Count];
				rigidBodyQuaternions 	= new Quaternion[rigidBodyList.Count];
			}
			receivedFirstRigidBodyPacket = true;
		}

		//Loop through the packet for the rigid body data
		for(int index=0; index<rigidBodyList.Count; index++)
		{
			rigidBodyIDs[index] = System.Convert.ToInt32(rigidBodyList[index].Attributes["ID"].InnerText);
			
			//Rigid Body Position
			rigidBodyPositions[index].x = -((float) System.Convert.ToDouble(rigidBodyList[index].Attributes["x"].InnerText)) 	+ origin.position.x;
			rigidBodyPositions[index].y = (float) System.Convert.ToDouble(rigidBodyList[index].Attributes["y"].InnerText) 		+ origin.position.y;
			rigidBodyPositions[index].z = (float) System.Convert.ToDouble(rigidBodyList[index].Attributes["z"].InnerText) 		+ origin.position.z;
			
			//Rigid Body Quaternion Orientation
            //Convert coordinates systems from Motive to Unity
			rigidBodyQuaternions[index].x = (float) System.Convert.ToDouble(rigidBodyList[index].Attributes["qx"].InnerText);
            rigidBodyQuaternions[index].y = -((float)System.Convert.ToDouble(rigidBodyList[index].Attributes["qy"].InnerText));
            rigidBodyQuaternions[index].z = -((float)System.Convert.ToDouble(rigidBodyList[index].Attributes["qz"].InnerText));
            rigidBodyQuaternions[index].w = (float)System.Convert.ToDouble(rigidBodyList[index].Attributes["qw"].InnerText);
            
            //Add the origin roatation
            rigidBodyQuaternions[index] = origin.rotation * rigidBodyQuaternions[index];      
		}
	}
}
