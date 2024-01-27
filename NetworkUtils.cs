using System;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using static Unity.Netcode.CustomMessagingManager;

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

		public void SendToAll(string tag, byte[] data)
		{
			if (!_initialized)
			{
				return;
			}
			foreach (var (num2, val2) in NetworkManager.Singleton.ConnectedClients)
			{
				SendTo(val2.ClientId, tag, data);
			}
		}

		public void SendTo(ulong clientId, string tag, byte[] data)
		{
			if (!_initialized)
			{
				return;
			}
			NetworkMessage networkMessage = default(NetworkMessage);
			networkMessage.MessageTag = tag;
			networkMessage.TargetId = clientId;
			networkMessage.Data = data;
			networkMessage.Checksum = Hash128.Compute<byte>(data);
			NetworkMessage networkMessage2 = networkMessage;
			string text = JsonUtility.ToJson((object)networkMessage2);
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			int writeSize = FastBufferWriter.GetWriteSize(text, false);
			FastBufferWriter val = new FastBufferWriter(writeSize + 1, Unity.Collections.Allocator.Temp, -1);
			FastBufferWriter val2 = val;
			try
			{
				val.WriteValueSafe(text, false);
				Debug.Log((object)$"[Network Transport] Sending message to {clientId} {text}");
				NetworkManager.Singleton.CustomMessagingManager.SendUnnamedMessage(clientId, val, (NetworkDelivery)3);
			}
			finally
			{
				val2.Dispose();
			}
		}
	}
}
