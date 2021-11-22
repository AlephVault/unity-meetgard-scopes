using AlephVault.Unity.Binary.Wrappers;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(ScopesProtocolServerSide))]
        public class SampleProtocolServerSide : ProtocolServerSide<SampleProtocolDefinition>
        {
            private ScopesProtocolServerSide scopesProtocol;
            private Func<ulong, Task> SendOK;

            protected override void Initialize()
            {
                scopesProtocol = GetComponent<ScopesProtocolServerSide>();
                SendOK = MakeSender("OK");
            }

            protected override void SetIncomingMessageHandlers()
            {
                AddIncomingMessageHandler("GoTo:Extra", async (proto, connectionId) => {
                    Debug.Log("Sample Protocol Server Side::Attending GoTo:Extra...");
                    ScopeServerSide sss = await scopesProtocol.LoadExtraScope("sample-extra");
                    await scopesProtocol.SendTo(connectionId, sss.Id);
                    await SendOK(connectionId);
                    Debug.Log("Sample Protocol Server Side::GoTo:Extra attended.");
                });

                AddIncomingMessageHandler("GoTo:Limbo", async (proto, connectionId) => {
                    Debug.Log("Sample Protocol Server Side::Attending GoTo:Limbo...");
                    await scopesProtocol.SendToLimbo(connectionId);
                    await SendOK(connectionId);
                    Debug.Log("Sample Protocol Server Side::GoTo:Limbo attended.");
                });

                AddIncomingMessageHandler<UInt>("GoTo:Default", async (proto, connectionId, message) => {
                    Debug.Log($"Sample Protocol Server Side::Attending GoTo:Default {(uint)message}...");
                    await scopesProtocol.SendTo(connectionId, message);
                    await SendOK(connectionId);
                    Debug.Log($"Sample Protocol Server Side::GoTo:Default {(uint)message} attended.");
                });
            }
        }
    }
}