using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(SampleProtocolClientSide))]
        public class SampleClientStarter : MonoBehaviour
        {
            [SerializeField]
            private KeyCode startKey = KeyCode.A;

            [SerializeField]
            private KeyCode stopKey = KeyCode.S;

            [SerializeField]
            private KeyCode[] gotoDefaultKeys;

            [SerializeField]
            private KeyCode gotoLimboKey = KeyCode.D;

            [SerializeField]
            private KeyCode gotoExtraKey = KeyCode.F;

            private NetworkClient client;
            private SampleProtocolClientSide protocol;

            private void Awake()
            {
                client = GetComponent<NetworkClient>();
                protocol = GetComponent<SampleProtocolClientSide>();
            }

            void Update()
            {
                if (Input.GetKeyDown(startKey) && !client.IsRunning && !client.IsConnected)
                {
                    Debug.Log("Client::Connecting...");
                    client.Connect("localhost", 9999);
                    Debug.Log("Client::Connected.");
                }

                if (client.IsRunning && client.IsConnected)
                {
                    if (Input.GetKeyDown(stopKey))
                    {
                        Debug.Log("Client::Disconnecting...");
                        client.Close();
                        Debug.Log("Client::Disconnected.");
                    }
                    else if (Input.GetKeyDown(gotoLimboKey))
                    {
                        Debug.Log("Client::Requiring Limbo...");
                        protocol.DoSendGoToLimbo();
                        Debug.Log("Client::Limbo required.");
                    }
                    else if (Input.GetKeyDown(gotoExtraKey))
                    {
                        Debug.Log("Client::Requiring Extra...");
                        protocol.DoSendGoToExtra();
                        Debug.Log("Client::Extra required.");
                    }
                    else
                    {
                        for(uint index = 0; index < gotoDefaultKeys.Length; index++)
                        {
                            if (Input.GetKeyDown(gotoDefaultKeys[index]))
                            {
                                Debug.Log($"Client::Requiring Default {index + 1}...");
                                protocol.DoSendGoToDefault(index + 1);
                                Debug.Log($"Client::Default {index + 1} required.");
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}