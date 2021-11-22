using AlephVault.Unity.Binary.Wrappers;
using AlephVault.Unity.Meetgard.Protocols;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        public class SampleProtocolDefinition : ProtocolDefinition
        {
            protected override void DefineMessages()
            {
                DefineClientMessage<UInt>("GoTo:Default");
                DefineClientMessage("GoTo:Extra");
                DefineClientMessage("GoTo:Limbo");
                DefineServerMessage("OK");
            }
        }
    }
}