using AlephVault.Unity.Meetgard.Protocols;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols.Messages;
using AlephVault.Unity.Meetgard.Types;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Types
    {
        namespace Protocols
        {
            /// <summary>
            ///   The scopes protocol is a pure server side protocol
            ///   which defines the messages for the server to send
            ///   and the client(s) to handle. Those messages will
            ///   be related to the clients being moved to scopes
            ///   and being kicked from them.
            /// </summary>
            public class ScopesProtocolDefinition : ProtocolDefinition
            {
                protected override void DefineMessages()
                {
                    /***
                     * The main messages to be considered here, that
                     * the server sends to the client, are:
                     * 
                     * Welcome: The connection just arrived and it
                     *   is in the "Limbo" scope. Additional game
                     *   logic will tell the client that is being
                     *   moved somewhere else, later.
                     * 
                     * MovedToScope(prefab, scope): The connection
                     *   was just moved to a scope given by its id,
                     *   and with a prefab. If the prefab is none*,
                     *   then the given index is expected to exist
                     *   as a "default" scope. Otherwise, the prefab
                     *   must be available among the "extra" (i.e.
                     *   non-default) scope prefabs: the client
                     *   must instantiate the prefab and assign it
                     *   the given id.
                     *   
                     *   The indices to use are restricted to be
                     *   between 0 (including) and 0xffffff00 (not
                     *   including). Indices from 0xffffff00 will
                     *   exist but with a particular, reserved,
                     *   meaning:
                     *   - 00: Limbo (the one corresponding to the
                     *     "Welcome" message).
                     *   - 01: Reloading (corresponds to a temporary
                     *     scope for when the current scope the
                     *     connection belonged to is currently in
                     *     maintenance/reload).
                     *   
                     *   A prefab index of 0xffffffff is interpreted
                     *   as "none".
                     *   
                     *   Only ONE scope will a connection belong to
                     *   at the same time. However, a connection may
                     *   "own" many objects at the same time (this is
                     *   as per-game logic) in different scopes.
                     * 
                     * Spawned(scope, prefab, object, data): In the
                     *   current scope (whose id is sent as well for
                     *   redundancy) an object is being instantiated.
                     *   The object has a specified prefab (which
                     *   must exist in a matching pair between client
                     *   and server). This is both for all of the
                     *   existing objects for a new connection, and
                     *   a new object for all the existing connections.
                     *   The id of the object is also provided, to
                     *   make the object track possible. The data is
                     *   a byte array, which object prefab pairs must
                     *   have matching implementations to encode and
                     *   decode.
                     * 
                     * Refreshed(scope, object, data): In the current
                     *   scope (whose id is sent as well for redundancy),
                     *   and for a given object which belongs to that
                     *   scope (whose id is sent as well), its data
                     *   will be refreshed. It is NOT necessary for
                     *   the data encoding/decoding process to be the
                     *   same as in the Spawned message (and, in fact,
                     *   most of the times it will NOT be the case).
                     *   The client must reflect the changes in the
                     *   new data, onto the existing object, without
                     *   creating/destroying it.
                     *                      * 
                     * Despawned(scope, object): In the current scope
                     *   (whose id is sent as well for redundancy),
                     *   and for a given object which belongs to that
                     *   scope (whose id is sent as well), it has been
                     *   despawned. Despawning an object typically
                     *   means it was destroyed or it belongs now to
                     *   a different scope (i.e. was moved out of the
                     *   current scope). The client side must locally
                     *   destroy the object. Also, if the object had
                     *   another type of interaction, both the server
                     *   and the client must end those types of
                     *   interactions as well (e.g. observing extra
                     *   data, or owner-specific data, from the object).
                     * 
                     * Other messages are game-specific and will not
                     * be specified here, even when they are related
                     * to the game objects or the scope itself.
                     * 
                     * The client may send an object to the server:
                     * 
                     * LocalError(): The client had a local error
                     *   while it was processing one or more of the
                     *   server messages, and is telling it will close
                     *   the connection. Much like a Logout message,
                     *   everything must be closed and released from
                     *   the server perspective (i.e. sessions and
                     *   that sort of things) and the connection must
                     *   be explicitly closed in the server side as
                     *   well, without asking further questions.
                     */
                    DefineServerMessage<Nothing>("Welcome");
                    DefineServerMessage<MovedToScope>("MovedToScope");
                    DefineServerMessage<ObjectSpawned>("ObjectSpawned");
                    DefineServerMessage<ObjectRefreshed>("ObjectRefreshed");
                    DefineServerMessage<ObjectDespawned>("ObjectDespawned");
                    DefineClientMessage<Nothing>("LocalError");
                }
            }
        }
    }
}