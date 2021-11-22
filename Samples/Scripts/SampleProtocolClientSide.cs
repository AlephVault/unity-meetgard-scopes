using AlephVault.Unity.Binary.Wrappers;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        public class SampleProtocolClientSide : ProtocolClientSide<SampleProtocolDefinition>
        {
            private Func<Task> SendGoToExtra;
            private Func<Task> SendGoToLimbo;
            private Func<UInt, Task> SendGoToDefault;

            protected override void Initialize()
            {
                SendGoToExtra = MakeSender("GoTo:Extra");
                SendGoToLimbo = MakeSender("GoTo:Limbo");
                SendGoToDefault = MakeSender<UInt>("GoTo:Default");
            }

            public async void DoSendGoToLimbo()
            {
                Debug.Log("Sample Protocol Client Side::[Async] Requiring Limbo...");
                _ = SendGoToLimbo();
                Debug.Log("Sample Protocol Client Side::[Async] Limbo required.");
            }

            public async void DoSendGoToExtra()
            {
                Debug.Log("Sample Protocol Client Side::[Async] Requiring Extra...");
                _ = SendGoToExtra();
                Debug.Log("Sample Protocol Client Side::[Async] Extra required.");
            }

            public async void DoSendGoToDefault(uint index)
            {
                Debug.Log($"Sample Protocol Client Side::[Async] Requiring Default {index}...");
                _ = SendGoToDefault((UInt)index);
                Debug.Log($"Sample Protocol Client Side::[Async] Default {index} required.");
            }

            protected override void SetIncomingMessageHandlers()
            {
                AddIncomingMessageHandler("OK", async (proto) => {
                    Debug.Log("Success on sample request");
                });
            }
        }
    }
}