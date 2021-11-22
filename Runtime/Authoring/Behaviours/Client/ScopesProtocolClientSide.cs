using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Authoring.Behaviours.Client;
using AlephVault.Unity.Meetgard.Scopes.Types.Constants;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols.Messages;
using AlephVault.Unity.Support.Authoring.Behaviours;
using AlephVault.Unity.Support.Generic.Vendor.IUnified.Authoring.Types;
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
            namespace Client
            {
                /// <summary>
                ///   <para>
                ///     The client side implementation of the scopes-managing protocol.
                ///     It works for a client connection and will be aware of the other
                ///     side (i.e. client) of the scopes the server instantiates over
                ///     the network. It also manages the related objects. For both the
                ///     objects and the scopes, exactly one counterpart will exist in
                ///     the client, and a perfect match must exist to avoid any kind
                ///     of errors and mismatches.
                ///   </para>
                ///   <para>
                ///     Different to the server side, the world is NOT loaded, but
                ///     every time only a single scope is kept as loaded (others will
                ///     be destroyed / somehow unloaded).
                ///   </para>
                /// </summary>
                public partial class ScopesProtocolClientSide : ProtocolClientSide<ScopesProtocolDefinition>
                {
                    /// <summary>
                    ///   A container for a UnityObject implementing the interface:
                    ///   <see cref="IObjectClientSideInstanceManager"/>.
                    /// </summary>
                    [Serializable]
                    public class IObjectClientSideInstanceManagerContainer : IUnifiedContainer<IObjectClientSideInstanceManager> {}

                    // Whether to debug or not using XDebug.
                    private static bool debug = false;

                    /// <summary>
                    ///   This is the list of scopes that will be paid attention to
                    ///   when receiving a message which instantiates one out of the
                    ///   default scopes (i.e. static scopes).
                    /// </summary>
                    [SerializeField]
                    private ScopeClientSide[] defaultScopePrefabs;

                    /// <summary>
                    ///   This is the list of scopes that will be paid attention to
                    ///   when receiving a message which instantiates one out of the
                    ///   extra scopes (i.e. dynamic scopes).
                    /// </summary>
                    [SerializeField]
                    private ScopeClientSide[] extraScopePrefabs;

                    /// <summary>
                    ///   This is the lis of object prefabs that will be paid attention
                    ///   to when spawning an object.
                    /// </summary>
                    [SerializeField]
                    private ObjectClientSide[] objectPrefabs;

                    /// <summary>
                    ///   The currently loaded scope object. Only applicable
                    ///   for the default and extra scopes, and not for the
                    ///   special ones.
                    /// </summary>
                    public ScopeClientSide CurrentScope { get; private set; }

                    // The currently loaded objects.
                    private Dictionary<uint, ObjectClientSide> currentObjects = new Dictionary<uint, ObjectClientSide>();

                    /// <summary>
                    ///   The currently loaded scope id. This is particularly
                    ///   useful for special scopes, where no scope object
                    ///   actually exists.
                    /// </summary>
                    public uint CurrentScopeId { get; private set; }

                    // A sender for the LocalError message.
                    private Func<Task> SendLocalError;

                    /// <summary>
                    ///   An event for the Welcome message. When this
                    ///   event is handled, there is no active scope.
                    ///   The only thing that should be done here is:
                    ///   1. Clear any current scope, if any.
                    ///   2. Show some sort of "welcome"/"Loading" page.
                    ///      Each game must do this on its own.
                    /// </summary>
                    public event Action OnWelcome;

                    /// <summary>
                    ///   An event for the MovedToScope message. When
                    ///   this event is handled, there is one active scope.
                    ///   The scope is already loaded, so perhaps additional
                    ///   elements (e.g. hud) may appear on screen. The ids
                    ///   might correspond to the special scopes as well,
                    ///   so those cases must be considered as well when
                    ///   deciding what HUD or stuff to display.
                    /// </summary>
                    public event Action<ScopeClientSide> OnMovedToScope;

                    /// <summary>
                    ///   An event for the ObjectSpawned message. The object
                    ///   is, by this point, already spawned and registered,
                    ///   and their events were already triggered.
                    /// </summary>
                    public event Action<ObjectClientSide> OnSpawned;

                    /// <summary>
                    ///   An event for the ObjectRefreshed message. The object
                    ///   is, by this point, already spawned, registered, and
                    ///   refreshed, and their events were already triggered.
                    /// </summary>
                    public event Action<ObjectClientSide, ISerializable> OnRefreshed;

                    /// <summary>
                    ///   An event for the ObjectDespawned message. The object
                    ///   is, by this point, already despawned andd unregistered,
                    ///   and their events were already triggered.
                    /// </summary>
                    public event Action<ObjectClientSide> OnDespawned;

                    /// <summary>
                    ///   An event for when a local error occurs. Previously, the
                    ///   LocaError message was sent and the connection was closed.
                    /// </summary>
                    public event Action<string> OnLocalError;

                    /// <summary>
                    ///   The local instance manager, if any.
                    /// </summary>
                    [SerializeField]
                    public IObjectClientSideInstanceManagerContainer InstanceManager;

                    /// <summary>
                    ///   An enumerator of the currently spawned objects in the current scope.
                    /// </summary>
                    public IEnumerable<ObjectClientSide> Objects()
                    {
                        return currentObjects.Values;
                    }

                    /// <summary>
                    ///   Gets an object among the current ones in the current scope by its id.
                    /// </summary>
                    /// <param name="id">The id of the object to retrieve</param>
                    /// <returns>The object by its id, or null if absent</returns>
                    public ObjectClientSide GetObject(uint id)
                    {
                        return currentObjects.TryGetValue(id, out ObjectClientSide value) ? value : null;
                    }

                    protected override void Initialize()
                    {
                        SendLocalError = MakeSender("LocalError");
                    }

                    protected override void SetIncomingMessageHandlers()
                    {
                        AddIncomingMessageHandler("Welcome", async (proto) => {
                            _ = RunInMainThread(async () =>
                            {
                                XDebug debugger = new XDebug("Meetgard.Scopes", this, "HandleMessage:Welcome", debug);
                                debugger.Start();
                                debugger.Info("Clearing any current scope");
                                ClearCurrentScope();
                                debugger.Info("Setting Limbo as current scope");
                                CurrentScopeId = Scope.Limbo;
                                debugger.Info("Triggering OnWelcome event");
                                OnWelcome?.Invoke();
                                debugger.End();
                            });
                        });
                        AddIncomingMessageHandler<MovedToScope>("MovedToScope", async (proto, message) => {
                            _ = RunInMainThread(async () =>
                            {
                                XDebug debugger = new XDebug("Meetgard.Scopes", this, $"HandleMessage:MovedToScope({message.ScopeIndex}, {message.PrefabIndex})", debug);
                                debugger.Start();
                                debugger.Info("Clearing any current scope");
                                ClearCurrentScope();
                                try
                                {
                                    debugger.Info("Loading a new scope");
                                    LoadNewScope(message.ScopeIndex, message.PrefabIndex);
                                    CurrentScopeId = message.ScopeIndex;
                                }
                                catch (Exception e)
                                {
                                    debugger.Exception(e);
                                    await LocalError("ScopeLoadError");
                                    return;
                                }
                                debugger.Info("Triggering OnMovedToScope event");
                                OnMovedToScope?.Invoke(CurrentScope);
                                debugger.End();
                            });
                        });
                        AddIncomingMessageHandler<ObjectSpawned>("ObjectSpawned", async (proto, message) => {
                            XDebug debugger = new XDebug("Meetgard.Scopes", this, $"HandleMessage:ObjectSpawned({message.ScopeIndex}:Queue, {message.ObjectIndex}, {message.ObjectPrefabIndex})", debug);
                            debugger.Start();
                            _ = RunInMainThread(async () =>
                            {
                                XDebug debugger = new XDebug("Meetgard.Scopes", this, $"HandleMessage:ObjectSpawned({message.ScopeIndex}, {message.ObjectIndex}, {message.ObjectPrefabIndex})", debug);
                                debugger.Start();
                                debugger.Info("Checking");
                                // It is to be checked: The current scope is a good one, object-holding and matching.
                                if (!await RequireIsCurrentScopeAndHoldsObjects(message.ScopeIndex)) return;

                                ObjectClientSide spawned;
                                try
                                {
                                    debugger.Info("Spawning the object");
                                    spawned = Spawn(message.ObjectIndex, message.ObjectPrefabIndex, message.Data);
                                }
                                catch (Exception e)
                                {
                                    debugger.Exception(e);
                                    await LocalError("SpawnError");
                                    return;
                                }

                                // This event occurs after the per-object spawned event.
                                debugger.Info($"Triggering OnSpawned event for object: {spawned}");
                                OnSpawned?.Invoke(spawned);
                                debugger.End();
                            });
                        });
                        AddIncomingMessageHandler<ObjectRefreshed>("ObjectRefreshed", async (proto, message) => {
                            _ = RunInMainThread(async () =>
                            {
                                XDebug debugger = new XDebug("Meetgard.Scopes", this, $"HandleMessage:ObjectRefreshed({message.ScopeIndex}, {message.ObjectIndex})", debug);
                                debugger.Start();
                                debugger.Info("Checking");
                                // It is to be checked: The current scope is a good one, object-holding and matching.
                                if (!await RequireIsCurrentScopeAndHoldsObjects(message.ScopeIndex)) return;

                                Tuple<ObjectClientSide, ISerializable> result;
                                try
                                {
                                    debugger.Info("Refreshing the object");
                                    result = Refresh(message.ObjectIndex, message.Data);
                                }
                                catch (Exception e)
                                {
                                    debugger.Exception(e);
                                    await LocalError("RefreshError");
                                    return;
                                }

                                // This event occurs after the per-object refreshed event.
                                debugger.Info("Triggering OnRefreshed event");
                                OnRefreshed?.Invoke(result.Item1, result.Item2);
                                debugger.End();
                            });
                        });
                        AddIncomingMessageHandler<ObjectDespawned>("ObjectDespawned", async (proto, message) => {
                            _ = RunInMainThread(async () =>
                            {
                                XDebug debugger = new XDebug("Meetgard.Scopes", this, $"HandleMessage:ObjectDespawned({message.ScopeIndex}, {message.ObjectIndex})", debug);
                                debugger.Start();
                                debugger.Info("Checking");
                                // It is to be checked: The current scope is a good one, object-holding and matching.
                                if (!await RequireIsCurrentScopeAndHoldsObjects(message.ScopeIndex)) return;

                                ObjectClientSide despawned;
                                try
                                {
                                    debugger.Info("Despawning the object");
                                    despawned = Despawn(message.ObjectIndex);
                                }
                                catch (Exception e)
                                {
                                    debugger.Exception(e);
                                    await LocalError("DespawnError");
                                    return;
                                }

                                // This event occurs after the per-object despawned event.
                                debugger.Info("Invoking OnDespawned event");
                                OnDespawned?.Invoke(despawned);
                                debugger.End();
                            });
                        });
                    }

                    public override async Task OnDisconnected(Exception reason)
                    {
                        var _ = RunInMainThread(() =>
                        {
                            XDebug debugger = new XDebug("Meetgard.Scopes", this, "OnDisconnected", debug);
                            debugger.Start();
                            ClearCurrentScope();
                            debugger.End();
                        });
                    }

                    /// <summary>
                    ///   Checks the current scope to be a valid object-holding scope.
                    ///   If not, either the server is misconfigured or the client lost
                    ///   synchronization, and must close.
                    /// </summary>
                    /// <param name="scopeIndex">The scope id to check</param>
                    /// <returns>Whether the current scope is the given one, and the given one holds objects</returns>
                    public async Task<bool> RequireIsCurrentScopeAndHoldsObjects(uint scopeIndex)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"RequireIsCurrentScopeAndHoldsObjects({scopeIndex})", debug);
                        debugger.Start();
                        if (CurrentScope.Id >= Scope.MaxScopes)
                        {
                            // This is an error: The scope id is aboce the maximum
                            // scopes that can be related to scope objects and thus
                            // reflect object states.
                            debugger.Error($"Invalid scope. Current scope, as the server told, is {CurrentScopeId} which is not an object-holding scope");
                            await LocalError("InvalidServerScope");
                            debugger.End();
                            return false;
                        }

                        if (CurrentScope == null || CurrentScope.Id != scopeIndex)
                        {
                            // This is an error: Either the current scope is null,
                            // or unmatched against the incoming scope index.
                            //
                            // This all will be treated as a local error instead.
                            debugger.Error($"Scope mismatch. Current scope is {CurrentScopeId} and message scope is {scopeIndex}");
                            await LocalError("ScopeMismatch");
                            debugger.End();
                            return false;
                        }
                        debugger.End();
                        return true;
                    }

                    // Clears the current scope, destroying everything.
                    private void ClearCurrentScope()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"ClearCurrentScope()", debug);
                        debugger.Start();
                        if (CurrentScope)
                        {
                            debugger.Info($"There is a current objects-holding scope ({CurrentScopeId})");
                            foreach(ObjectClientSide instance in currentObjects.Values)
                            {
                                instance.Despawn();
                                // TODO Allow defining a strategy for spawning (e.g. direct or pooling),
                                // TODO instead of just instantiating the object.
                                Destroy(instance.gameObject);
                            }
                            currentObjects.Clear();
                            CurrentScope.Unload();
                            Destroy(CurrentScope.gameObject);
                            CurrentScope = null;
                        }
                        debugger.End();
                    }

                    // Initializes a new scope.
                    private void LoadNewScope(uint scopeId, uint scopePrefabId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"LoadNewScope({scopeId}, {scopePrefabId})", debug);
                        debugger.Start();
                        if (scopeId < Scope.MaxScopes)
                        {
                            debugger.Info($"The mew scope is object-holding");
                            ScopeClientSide prefab;
                            if (scopePrefabId == Scope.DefaultPrefab)
                            {
                                prefab = defaultScopePrefabs[scopeId - 1];
                            }
                            else
                            {
                                prefab = extraScopePrefabs[scopePrefabId];
                            }
                            debugger.Info($"Instantiating the object");
                            ScopeClientSide instance = Instantiate(prefab);
                            instance.Id = scopeId;
                            CurrentScope = instance;
                            CurrentScopeId = scopeId;
                            debugger.Info($"Loading te new scope");
                            instance.Load();
                        }
                        else
                        {
                            debugger.Info($"The mew scope is not object-holding");
                            CurrentScopeId = scopeId;
                        }
                        debugger.End();
                    }

                    // Spawns a new object.
                    private ObjectClientSide Spawn(uint objectId, uint objectPrefabId, byte[] data)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"Spawn({objectId}, {objectPrefabId})", debug);
                        debugger.Start();
                        if (currentObjects.ContainsKey(objectId))
                        {
                            debugger.End();
                            throw new InvalidOperationException($"The object id: {objectId} is already in use");
                        }
                        else
                        {
                            // Get a new instance, register it and spawn it.
                            debugger.Info($"Instantiating the object locally (objectId={objectId}, prefabId={objectPrefabId}, prefab={objectPrefabs[objectPrefabId]})");
                            ObjectClientSide instance = InstanceManager.Result != null ? InstanceManager.Result.Get(objectPrefabs[objectPrefabId]) : Instantiate(objectPrefabs[objectPrefabId]);
                            debugger.Info($"Assigning current client protocol and registering");
                            instance.Protocol = this;
                            currentObjects.Add(objectId, instance);
                            debugger.Info($"Spawning the object locally");
                            instance.Spawn(CurrentScope, objectId, data);
                            debugger.End();
                            return instance;
                        }
                    }

                    // Refreshes an object. Returns both the refreshed object and the
                    // data used for refresh.
                    private Tuple<ObjectClientSide, ISerializable> Refresh(uint objectId, byte[] data)
                    {
                        if (!currentObjects.TryGetValue(objectId, out ObjectClientSide instance))
                        {
                            throw new InvalidOperationException($"The object id: {objectId} is not in use");
                        }
                        else
                        {
                            ISerializable model = instance.Refresh(data);
                            return new Tuple<ObjectClientSide, ISerializable>(instance, model);
                        }
                    }

                    // Despawns an object. Returns the already despawned object.
                    private ObjectClientSide Despawn(uint objectId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"Despawn({objectId})", debug);
                        debugger.Start();
                        if (!currentObjects.TryGetValue(objectId, out ObjectClientSide instance))
                        {
                            debugger.End();
                            throw new InvalidOperationException($"The object id: {objectId} is not in use");
                        }
                        else
                        {
                            debugger.Info($"Despawning {objectId}");
                            // Despawn the instance, unregister it, and release it.
                            instance.Despawn();
                            currentObjects.Remove(objectId);
                            if (InstanceManager.Result != null) {
                                InstanceManager.Result.Release(instance);
                            } else {
                                Destroy(instance.gameObject);
                            };
                            // The instance is already unspawned by this point. Depending on the
                            // strategy to use, this may imply the instance is destroyed..
                            debugger.End();
                            return instance;
                        }
                    }

                    /// <summary>
                    ///   Raises a local error and closes the connection.
                    /// </summary>
                    /// <param name="context">
                    ///   The context to raise the error.
                    ///   Only useful locally, for the <see cref="OnLocalError"/> event
                    /// </param>
                    public async Task LocalError(string context)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"LocalError({context})", debug);
                        debugger.Start();
                        // We actually wait for this message before closing
                        // the connection, to ensure it was sent.
                        await SendLocalError();
                        client.Close();
                        debugger.Info("Triggering OnLocalError");
                        OnLocalError?.Invoke(context);
                        debugger.End();
                    }
                }
            }
        }
    }
}