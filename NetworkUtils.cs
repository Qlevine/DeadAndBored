using System;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;
using GameNetcodeStuff;

namespace DeadAndBored
{
	public struct NetworkMessage
	{
		public string MessageTag;

		public ulong TargetId;

		public Hash128 Checksum;

		public byte[] Data;
	}
	public class NetworkUtils : MonoBehaviour
	{
		public static NetworkUtils instance;

		public Action<string, byte[]> OnNetworkData;

		public Action OnDisconnect;

		public Action OnConnect;

		private bool _initialized;

		private bool _connected;

		public bool IsConnected => _connected;

		public static string HostRelayID = "HOST_RELAY";

		public static void Init()
        {
			if(instance == null)
            {
				GameObject networkUtilObject = new GameObject();
				networkUtilObject.name = "DeadAndBored Network Util";
				NetworkUtils networkUtils = networkUtilObject.AddComponent<NetworkUtils>();
				instance = networkUtils;
				DontDestroyOnLoad(networkUtilObject);
				DeadAndBoredObject.DABLogging("Init Network Utils");
            }
        }

		private void Initialize()
		{
			if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
			{
				_initialized = true;
				DeadAndBoredObject.DABLogging("[Network Transport] Initialized");
			}
		}

		private void UnInitialize()
		{
			if (_connected)
			{
				Disconnected();
			}
			_initialized = false;
		}

		private void Connected()
		{
			NetworkManager.Singleton.CustomMessagingManager.OnUnnamedMessage += new UnnamedMessageDelegate(OnMessageEvent);
			OnConnect?.Invoke();
			_connected = true;
			DeadAndBoredObject.DABLogging("[Network Transport] Connected");
		}

		private void Disconnected()
		{
			NetworkManager singleton = NetworkManager.Singleton;
			if (((singleton != null) ? singleton.CustomMessagingManager : null) != null)
			{
				NetworkManager.Singleton.CustomMessagingManager.OnUnnamedMessage -= new UnnamedMessageDelegate(OnMessageEvent);
			}
			OnDisconnect?.Invoke();
			_connected = false;
			DeadAndBoredObject.DABLogging("[Network Transport] Disconnected");
		}

		public void Update()
		{
			if (!_initialized)
			{
				Initialize();
			}
			else if (NetworkManager.Singleton == null)
			{
				UnInitialize();
			}
			else if (!_connected && NetworkManager.Singleton.IsConnectedClient)
			{
				Connected();
			}
			else if (_connected && !NetworkManager.Singleton.IsConnectedClient)
			{
				Disconnected();
			}
		}

		private void OnMessageEvent(ulong clientId, FastBufferReader reader)
		{
			Hash128 val = new Hash128();
			string text = "";
			NetworkMessage networkMessage = new NetworkMessage();
			try
			{
				reader.ReadValueSafe(out text, false);
				DeadAndBoredObject.DABLogging($"[Network Transport] Incoming message from {clientId} {text}");
				networkMessage = JsonUtility.FromJson<NetworkMessage>(text);
				val = Hash128.Compute<byte>(networkMessage.Data);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			if (val != default(Hash128) && val.CompareTo(val) == 0)
			{
				OnNetworkData?.Invoke(networkMessage.MessageTag, networkMessage.Data);
			}
		}


		public class RelayObject
        {
			public string tag;
			public byte[] data;
        }
		public void RelayToHost(string tag, byte[] data)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
				ulong hostID = 0uL;
				foreach(PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player.IsHost)
                    {
						hostID = player.actualClientId;
                    }
                }

				RelayObject relayObject = new RelayObject();
				relayObject.tag = tag;
				relayObject.data = data;
				string json = JsonUtility.ToJson(relayObject);
				byte[] relayByte = Encoding.UTF8.GetBytes(json);
				DeadAndBoredObject.DABLogging($"Relay message from to host {hostID}");
				SendTo(hostID, HostRelayID, relayByte);
			}
            else
            {
				DeadAndBoredObject.DABLogging("Error: Trying to relay when already host");
            }

		}

		public void SendToAll(string tag, byte[] data)
		{
			if (!_initialized)
			{
				return;
			}

            if (NetworkManager.Singleton.IsHost)
            {
				foreach (var (num2, val2) in NetworkManager.Singleton.ConnectedClients)
				{
					SendTo(val2.ClientId, tag, data);
				}
			}
            else
            {
				RelayToHost(tag, data);
            }
		}

		public void SendTo(ulong clientId, string tag, byte[] data)
		{
			if (!_initialized)
			{
				return;
			}
			NetworkMessage networkMessage = new NetworkMessage();
			networkMessage.MessageTag = tag;
			networkMessage.TargetId = clientId;
			networkMessage.Data = data;
			networkMessage.Checksum = Hash128.Compute<byte>(data);
			string text = JsonUtility.ToJson(networkMessage);
			int writeSize = FastBufferWriter.GetWriteSize(text);
			using FastBufferWriter writer = new FastBufferWriter(writeSize, Unity.Collections.Allocator.Temp);
			DeadAndBoredObject.DABLogging($"Send To: Write Size{writeSize}");
			writer.WriteValue(text);
			NetworkManager.Singleton.CustomMessagingManager.SendUnnamedMessage(clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
		}
	}
}
