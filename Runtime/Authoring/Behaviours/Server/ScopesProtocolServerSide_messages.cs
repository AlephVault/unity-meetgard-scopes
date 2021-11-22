using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Server
            {
                public partial class ScopesProtocolServerSide : ProtocolServerSide<ScopesProtocolDefinition>
                {
                    // A sender for the Welcome message.
                    private Func<ulong, Task> SendWelcome;

                    // A sender for the MovedToScope message.
                    private Func<ulong, MovedToScope, Task> SendMovedToScope;

                    // A sender for the ObjectSpawned message.
                    // Use case: when a new connection arrives, for each object.
                    internal Func<ulong, ObjectSpawned, Task> SendObjectSpawned;

                    // A broadcaster for the ObjectSpawned message.
                    // Use case: when a new object spawns, for each connection.
                    internal Func<IEnumerable<ulong>, ObjectSpawned, Dictionary<ulong, Task>> BroadcastObjectSpawned;

                    // A broadcaster for the ObjectRefreshed message.
                    // Use case: when a new connection requests refresh, for each object.
                    internal Func<ulong, ObjectRefreshed, Task> SendObjectRefreshed;

                    // A broadcaster for the ObjectDespawned message.
                    // Use case: when an object despawns, for each connection.
                    internal Func<IEnumerable<ulong>, ObjectDespawned, Dictionary<ulong, Task>> BroadcastObjectDespawned;

                    // These functions are somewhat auxiliar and build on top of the
                    // message senders and broadcasters, when needed. In particular,
                    // these stand to object synchronization.

                    internal byte[] AllocateFullDataMessageBytes()
                    {
                        // The socket message size will be low-clamped to 512.
                        // This makes safe to subtract 16 to this size, with
                        // those 4*4 bytes being the upper bound of four 4-byte
                        // numbers: scope id, object prefab id, object id, and
                        // overall message size.
                        return new byte[MaxSocketMessageSize - 16];
                    }
                }
            }
        }
    }
}