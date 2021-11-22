using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Scopes.Types.Constants;
using AlephVault.Unity.Support.Authoring.Behaviours;
using AlephVault.Unity.Support.Utils;
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
                /// <summary>
                ///   The server side implementation of an object that must
                ///   exist both in the client and the server. This involves
                ///   also knowing which prefab id this object is registered
                ///   with, in a specific server making use of it, and even
                ///   a specific scope inside the server, if the object is
                ///   created into that server but spawned into no particular
                ///   server side scope.
                /// </summary>
                public abstract class ObjectServerSide : MonoBehaviour
                {
                    // These two fields are set by the protocol.

                    /// <summary>
                    ///   The id/index of the internal server-side registered
                    ///   prefab object this object is associated to. Typically,
                    ///   this means that an object is created in a particular
                    ///   server and with a particular prefab therein. This
                    ///   means that this value is meaningless, even when being
                    ///   zero, if <see cref="Protocol"/> is not set.
                    /// </summary>
                    public uint PrefabId { get; internal set; }

                    /// <summary>
                    ///   An optional key to be used. This is only meaningful
                    ///   if the object this key is assigned to, is actually a
                    ///   prefab object inside a specific server. When this
                    ///   value is set (in constrast to null or ""), it will be
                    ///   added to an internal dictionary of prefabs by their
                    ///   keys, and thus be available to be instantiated via
                    ///   a method taking its key instead of its index.
                    /// </summary>
                    [SerializeField]
                    private string prefabKey;

                    /// <summary>
                    ///   See <see cref="prefabKey"/>.
                    /// </summary>
                    public string PrefabKey => prefabKey;

                    /// <summary>
                    ///   The protocol this object is associated to. Typically,
                    ///   this means that an object is created in that particular
                    ///   server and with a particular prefab therein. This
                    ///   means that this value is associated with the value in
                    ///   the <see cref="PrefabId"/> field.
                    /// </summary>
                    public ScopesProtocolServerSide Protocol { get; internal set; }

                    // This field is set by the owning scope.

                    /// <summary>
                    ///   The id this object is assigned to inside a particular
                    ///   scope. This also requires a particular scope to be set
                    ///   in the <see cref="Scope"/> field. Also, the scope being
                    ///   assigned will belong to the same protocol that was given
                    ///   as value in the <see cref="Protocol"/> field. This
                    ///   means that this value is meaningless, even when being
                    ///   zero, if <see cref="Protocol"/> is not set.
                    /// </summary>
                    public uint Id { get; internal set; }

                    /// <summary>
                    ///   If the object is spawned, this field will have which
                    ///   scope this object is spawned into. If the object belongs
                    ///   to a server but it is not spawned, this value will be
                    ///   null. When this value is set, the <see cref="Id"/> field
                    ///   will have a meaninful value: the id this object was given
                    ///   when spawning this object (and also populating this field).
                    /// </summary>
                    public ScopeServerSide Scope { get; internal set; }

                    /// <summary>
                    ///   Triggered when the object is spawned inside the
                    ///   current scope.
                    /// </summary>
                    public event Func<Task> OnSpawned = null;

                    /// <summary>
                    ///   Triggered when the object spawn was fully performed.
                    ///   Additional logic will take place with a fully spawned
                    ///   and notified object.
                    /// </summary>
                    public event Func<Task> OnAfterSpawned = null;

                    /// <summary>
                    ///   Triggered when the object is despawned from the
                    ///   last scope. By this point, the object will not
                    ///   have any sort of scope association information.
                    /// </summary>
                    public event Func<Task> OnDespawned = null;

                    /// <summary>
                    ///   Triggered before the object despawn is fully.
                    ///   Additional logic will take place with a still spawned,
                    ///   yet about to be despawned, object.
                    /// </summary>
                    public event Func<Task> OnBeforeDespawned = null;

                    /// <summary>
                    ///   Returns the data of this object to synchronize for the
                    ///   connections. Each set of connection might receive a custom
                    ///   data set for this object. Warning: Both client and server
                    ///   must agree on the data type and it must be the same in
                    ///   all of the entries, save for certain fields which can be
                    ///   "censored" to null or empty (the data type itself must
                    ///   tolerate this).
                    /// </summary>
                    /// <param name="connections">The whole connections in a scope</param>
                    /// <returns>A list of pairs (connections, data), so each set of connections can potentially receive different sets of data</returns>
                    public abstract List<Tuple<HashSet<ulong>, ISerializable>> FullData(HashSet<ulong> connections);

                    /// <summary>
                    ///   Returns the data of this object to synchronize for the
                    ///   connection. Warning: Both client and server must agree on
                    ///   the data type and it must be the same in all the possible
                    ///   cases this function might return, save for the fact that
                    ///   some fields in the result might become "censored" to null
                    ///   or empty.
                    /// </summary>
                    /// <param name="connection">A connection in a scope</param>
                    /// <returns>The data to send to that connection</returns>
                    public abstract ISerializable FullData(ulong connection);

                    /// <summary>
                    ///   Returns the data of this object to synchronize for the
                    ///   connection, given a certain context. Typically it will,
                    ///   like in <see cref="FullData(ulong)"/> method, return the
                    ///   full object data, but only when the given context is one
                    ///   that matters for the object itslf. Otherwise, it returns
                    ///   null (meaning: given the current context, there is nothing
                    ///   to refresh). Warning: Both client and server must agree on
                    ///   the data type and it must be the same in all the possible
                    ///   cases this function might return, save for the fact that
                    ///   some fields in the result might become "censored" to null
                    ///   or empty. 
                    /// </summary>
                    /// <param name="connection">A connection in a scope</param>
                    /// <param name="context">The refresh context</param>
                    /// <returns>The data to send to that connection, or null when no need</returns>
                    public abstract ISerializable RefreshData(ulong connection, string context);

                    // When the object starts, it must track itself to find the scope
                    // it belongs to.
                    protected void Start()
                    {
                        TrackCurrentScopeHierarchy();
                    }

                    // When the object is enabled, it must track itself to find the scope
                    // it belongs to.
                    protected void OnEnable()
                    {
                        TrackCurrentScopeHierarchy();
                    }

                    // When the object is disabled, it must remove from the scope.
                    protected void OnDisable()
                    {
                        if (Scope != null) Scope.RemoveObject(this);
                    }

                    // When a parent changes, this object must retrack itself to find
                    // the scope it belongs to.
                    protected void OnTransformParentChanged()
                    {
                        TrackCurrentScopeHierarchy();
                    }

                    // This method tracks which one is the current scope and auto-adds
                    // itself to it, if any. For this to work, the current scope must
                    // be instantiated properly (i.e. within a working server) and
                    // any future scope must as well.
                    private void TrackCurrentScopeHierarchy()
                    {
                        ScopeServerSide newScope = GetComponentInParent<ScopeServerSide>();
                        if (Scope != newScope)
                        {
                            if (Scope != null) Scope.RemoveObject(this);
                            if (newScope != null) newScope.AddObject(this);
                        }
                    }

                    // Triggers the OnSpawned event.
                    internal Task TriggerOnSpawned()
                    {
                        return OnSpawned?.InvokeAsync(async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in object server side's OnSpawned event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        });
                    }

                    // Triggers the OnAfterSpawned event.
                    internal Task TriggerOnAfterSpawned()
                    {
                        return OnAfterSpawned?.InvokeAsync(async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in object server side's OnAfterSpawned event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        });
                    }

                    // Triggers the OnDespawned event.
                    internal Task TriggerOnDespawned()
                    {
                        return OnDespawned?.InvokeAsync(async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in object server side's OnDespawned event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        });
                    }

                    // Triggers the OnBeforeDespawned event.
                    internal Task TriggerOnBeforeDespawned()
                    {
                        return OnBeforeDespawned?.InvokeAsync(async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in object server side's OnBeforeDespawned event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        });
                    }
                }
            }
        }
    }
}

