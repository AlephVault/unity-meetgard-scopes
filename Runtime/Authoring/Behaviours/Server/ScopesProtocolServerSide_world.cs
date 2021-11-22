using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using AlephVault.Unity.Meetgard.Scopes.Types.Constants;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols;
using AlephVault.Unity.Support.Types;
using AlephVault.Unity.Support.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                    /// <summary>
                    ///   The world load status. This only stands for the default
                    ///   scopes (dynamic scopes are not included here, and any
                    ///   potential load error or other exception will be handled
                    ///   appropriately - without destroying the whole server).
                    /// </summary>
                    public LoadStatus WorldLoadStatus { get; private set; }

                    // The currently loaded scope server sides, mapped against their
                    // assigned id.
                    private Dictionary<uint, ScopeServerSide> loadedScopes = new Dictionary<uint, ScopeServerSide>();

                    // A read-only version to the currently loaded scope server sides,
                    // mapped against their assigned id.
                    private ReadOnlyDictionary<uint, ScopeServerSide> readonlyLoadedScopes = null;

                    /// <summary>
                    ///   Retrieves a read-only dictionary of the loaded scopes.
                    /// </summary>
                    /// <returns>A read-only dictionary</returns>
                    public ReadOnlyDictionary<uint, ScopeServerSide> LoadedScopes
                    {
                        get
                        {
                            if (readonlyLoadedScopes == null)
                            {
                                readonlyLoadedScopes = new ReadOnlyDictionary<uint, ScopeServerSide>(loadedScopes);
                            }
                            return readonlyLoadedScopes;
                        }
                    }

                    // The ids generator for the currently loaded scope server sides.
                    private IdPool loadedScopesIds = new IdPool(Scope.MaxScopes);

                    /// <summary>
                    ///   This event is triggered when an error occurs while trying
                    ///   to load the whole world. The intention of this handler
                    ///   is to log the exception somewhere. It is advised to check
                    ///   any exception that may be raised here, since they will be
                    ///   otherwise captured and hidden.
                    /// </summary>
                    public event Action<System.Exception> OnLoadError = null;

                    /// <summary>
                    ///   This event is triggered when an error occurs while trying
                    ///   to unload a particular world scene. The intention of this
                    ///   handler is to log the exception somewhere. It is advised
                    ///   to check any exception that may be raised here, since they
                    ///   will be otherwise captured and hidden.
                    /// </summary>
                    public event Action<uint, ScopeServerSide, System.Exception> OnUnloadError = null;

                    // Loads all of the default scopes. For each attempted entry,
                    // one instance will be added to the loaded scopes. Such instance
                    // will not just be instantiated but also initialized (e.g. its
                    // data being loaded from database). Any exception raised here
                    // will cause all the scopes to be unloaded and the server to be
                    // closed, if already open.
                    private async Task LoadDefaultScopes()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "LoadDefaultScopes()", debug);
                        debugger.Start();
                        loadedScopes = new Dictionary<uint, ScopeServerSide>();
                        loadedScopesIds = new IdPool(Scope.MaxScopes);
                        foreach(ScopeServerSide scopePrefab in defaultScopePrefabs)
                        {
                            debugger.Info($"Instantiating the scope prefab: {scopePrefab}");
                            ScopeServerSide instance = Instantiate(scopePrefab, null, true);
                            uint newId = (uint)loadedScopesIds.Next();
                            instance.Id = newId;
                            instance.PrefabId = Scope.DefaultPrefab;
                            instance.Protocol = this;
                            debugger.Info($"Registering it with id: {newId}");
                            loadedScopes.Add(newId, instance);
                            await instance.Load();
                        }
                        debugger.End();
                    }

                    // Unloads all of the loaded scopes, one by one. This may involve
                    // things like storing the last scope state back into database and
                    // that sort of things, in a per-scope basis. Any exception that
                    // occurs in this process will be handled in a per-scope basis.
                    private async Task UnloadScopes()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "UnloadScopes()", debug);
                        debugger.Start();
                        foreach(KeyValuePair<uint, ScopeServerSide> pair in loadedScopes)
                        {
                            if (pair.Value != null)
                            {
                                try
                                {
                                    debugger.Info($"Clearing connections from scope: {pair.Key}");
                                    await ClearConnectionsFromScope(pair.Value);
                                    debugger.Info($"Unloading scope: {pair.Key}");
                                    await pair.Value.Unload();
                                }
                                catch (System.Exception e)
                                {
                                    try
                                    {
                                        debugger.Info("Triggeing OnUnloadError");
                                        OnUnloadError?.Invoke(pair.Key, pair.Value, e);
                                    }
                                    catch(System.Exception)
                                    {
                                        Debug.LogError(
                                            "An exception has been triggered while handling a previous exception " +
                                            "in OnUnloadError while trying to unload the world. This should not " +
                                            "occur. Ensure any exception that may occur inside an OnUnloadError " +
                                            "handler is properly handled inside that handler, instead of letting " +
                                            "it bubble"
                                        );
                                    }
                                }
                            }
                        }
                        debugger.End();
                    }

                    // Clears the collection of loaded scopes, and destroys
                    // the scopes one by one.
                    private void DestroyInstantiatedScopes()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "DestroyInstantiatedScopes()", debug);
                        debugger.Start();
                        Dictionary<uint, ScopeServerSide> instances = loadedScopes;
                        loadedScopes = null;
                        foreach(KeyValuePair<uint, ScopeServerSide> pair in instances)
                        {
                            if (pair.Value != null) Destroy(pair.Value.gameObject);
                            pair.Value.Id = 0;
                            pair.Value.Protocol = null;
                            loadedScopesIds.Release(pair.Key);
                        }
                        debugger.End();
                    }

                    // This task will try to load the world and ensure it
                    // becomes "ready". If any error occurs on world load,
                    // then everything is reverted and the server is closed.
                    private async Task LoadWorld()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "LoadWorld()", debug);
                        debugger.Start();

                        // This makes no sense when the world is not unloaded.
                        if (WorldLoadStatus != LoadStatus.Empty) return;

                        // Set the initial, in-progress, status.
                        WorldLoadStatus = LoadStatus.Loading;

                        try
                        {
                            // Do the whole load.
                            await LoadDefaultScopes();

                            // Set the final, success, status.
                            WorldLoadStatus = LoadStatus.Ready;
                        }
                        catch (System.Exception e)
                        {
                            // Set a temporary error status.
                            WorldLoadStatus = LoadStatus.LoadError;

                            // Diaper-log any load exception.
                            try
                            {
                                debugger.Info("Triggering OnLoadError");
                                OnLoadError?.Invoke(e);
                            }
                            catch
                            {
                                Debug.LogError(
                                    "An exception has been triggered while handling a previous exception " +
                                    "in OnLoadError while trying to load the world. This should not occur. " +
                                    "Ensure any exception that may occur inside an OnLoadError handler is " +
                                    "properly handled inside that handler, instead of letting it bubble"
                                );
                            }

                            // Destroy the scopes. There is no Unload
                            // needed, since no change occurred in the
                            // scope per se (or: there is no sense for
                            // any change to be accounted for, since
                            // the game hasn't yet started).
                            DestroyInstantiatedScopes();

                            // Set the final, reset, status.
                            WorldLoadStatus = LoadStatus.Empty;

                            // And finally, close the server.
                            if (server.IsListening) server.StopServer();
                        }

                        debugger.End();
                    }

                    // This task will try to unload the world and ensure it
                    // becomes "empty". The unload errors will be each caught
                    // separately and logged separately as well, but the process
                    // will ultimately finish and the world will become "empty".
                    // It is recommended that no unload brings any error, since
                    // unloading will also mean data backup / store and related
                    // stuff. By the end, the world will become empty, and both
                    // default and extra scopes will be both unloaded and destroyed.
                    private async Task UnloadWorld()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "UnloadWorld()", debug);
                        debugger.Start();

                        // This makes no sense when the world is not loaded.
                        if (WorldLoadStatus != LoadStatus.Ready) return;

                        // Set the initial, in-progress, status.
                        WorldLoadStatus = LoadStatus.Unloading;

                        // Unload all of the scopes. Any exception
                        // will be handled and/or diapered separately.
                        await UnloadScopes();

                        // Destroy the already unloaded scopes. In the
                        // worst case, the lack of backup will restore
                        // the scope state to a previous state, with
                        // some elements not so fully synchronized and,
                        // if well managed by the per-game logic, that
                        // will not affect the overall game experience.
                        DestroyInstantiatedScopes();

                        // Set the final, success, status.
                        WorldLoadStatus = LoadStatus.Empty;

                        debugger.End();
                    }

                    /// <summary>
                    ///   Instantiates a scope by specifying its prefab key and initializing it.
                    ///   The scope will be registered, assigned an ID, and returned. This task
                    ///   is queued.
                    /// </summary>
                    /// <param name="extraScopePrefabKey">
                    ///   The key of an extra scope prefab to use. As a tip, as the extra
                    ///   scope prefabs are configurable, let the value for this argument
                    ///   come from a configurable source (i.e. editor, authoring) and not
                    ///   a constant or hard-coded value in the codebase
                    /// </param>
                    /// <param name="init">A function to initialize the scope to be loaded</param>
                    /// <returns>The loaded (and registered) scope instance</returns>
                    public Task<ScopeServerSide> LoadExtraScope(string extraScopePrefabKey)
                    {
                        return RunInMainThread(async () =>
                        {
                            XDebug debugger = new XDebug("Meetgard.Scopes", this, $"LoadExtraScope({extraScopePrefabKey})", debug);
                            debugger.Start();
                            debugger.Info("Checking world satatus");
                            if (WorldLoadStatus != LoadStatus.Ready)
                            {
                                throw new InvalidOperationException(
                                    "The world is currently not ready to load an extra scope"
                                );
                            }

                            debugger.Info($"Getting appropriate extra prefab ({extraScopePrefabKey})");
                            uint extraScopePrefabIndex;
                            try
                            {
                                extraScopePrefabIndex = extraScopePrefabIndicesByKey[extraScopePrefabKey];
                            }
                            catch (KeyNotFoundException)
                            {
                                throw new ArgumentException($"Unknown extra scope prefab key: {extraScopePrefabKey}");
                            }

                            debugger.Info("Instantiating extra scope");
                            ScopeServerSide instance = Instantiate(extraScopePrefabs[extraScopePrefabIndex], null, true);
                            debugger.Info("Assigning ID to the extra scope");
                            uint newId = (uint)loadedScopesIds.Next();
                            instance.Id = newId;
                            instance.PrefabId = extraScopePrefabIndex;
                            instance.Protocol = this;
                            loadedScopes.Add(newId, instance);
                            debugger.Info("Delegating load");
                            await instance.Load();
                            debugger.End();
                            return instance;
                        });
                    }

                    // Unloads and perhaps destroys a scope.
                    private async Task DoUnloadExtraScope(uint scopeId, ScopeServerSide scopeToUnload, bool destroy)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"DoUnloadExtraScope({scopeId})", debug);
                        debugger.Start();
                        debugger.Info("Clearing connections");
                        await ClearConnectionsFromScope(scopeToUnload);
                        debugger.Info("Delegating unload");
                        await scopeToUnload.Unload();
                        debugger.Info("Resetting ID");
                        loadedScopes.Remove(scopeId);
                        loadedScopesIds.Release(scopeId);
                        if (scopeToUnload != null && destroy) Destroy(scopeToUnload.gameObject);
                        scopeToUnload.Id = 0;
                        scopeToUnload.PrefabId = 0;
                        scopeToUnload.Protocol = null;
                        debugger.End();
                    }

                    /// <summary>
                    ///   Unloads and perhaps destroys a scope. This task is queued.
                    /// </summary>
                    /// <param name="scopeId">The id of the scope to unload</param>
                    /// <param name="destroy">Whether to also destroy it or not</param>
                    public Task UnloadExtraScope(uint scopeId, bool destroy = true)
                    {
                        return RunInMainThread(async () => {
                            XDebug debugger = new XDebug("Meetgard.Scopes", this, $"UnloadExtraScope({scopeId})", debug);
                            debugger.Start();
                            debugger.Info("Checking the id is not a default one");
                            if (scopeId <= defaultScopePrefabs.Length)
                            {
                                throw new ArgumentException(
                                    $"Cannot delete the scope with ID: {scopeId} since that ID belongs " +
                                    $"to the set of default scopes"
                                );
                            }

                            debugger.Info("Getting scope id to unload");
                            ScopeServerSide scopeToUnload;
                            try
                            {
                                scopeToUnload = loadedScopes[scopeId];
                            }
                            catch (KeyNotFoundException)
                            {
                                throw new ArgumentException(
                                    $"Cannot delete the scope with ID: {scopeId} since that ID belongs " +
                                    $"to the set of default scopes"
                                );
                            }

                            debugger.Info("Performing unload");
                            await DoUnloadExtraScope(scopeId, scopeToUnload, destroy);
                            debugger.End();
                        });
                    }

                    /// <summary>
                    ///   Unloads and perhaps destroys a scope. This task is queued.
                    /// </summary>
                    /// <param name="scope">The scope instance to unload</param>
                    /// <param name="destroy">Whether to also destroy it or not</param>
                    public Task UnloadExtraScope(ScopeServerSide scope, bool destroy = true)
                    {
                        return RunInMainThread(async () => {
                            XDebug debugger = new XDebug("Meetgard.Scopes", this, $"UnloadExtraScope({scope})", debug);
                            debugger.Start();
                            debugger.Info("Checking scope instance");
                            if (scope == null)
                            {
                                throw new ArgumentNullException("scope");
                            }
                            else if (scope.Protocol != this)
                            {
                                throw new ArgumentException("The given scope does not belong to this server - it cannot be deleted");
                            }

                            debugger.Info("Checking the id is not a default one");
                            uint scopeId = scope.Id;
                            if (scopeId <= defaultScopePrefabs.Length)
                            {
                                throw new ArgumentException(
                                    $"Cannot delete the scope, which has ID: {scopeId} since that scope " +
                                    $"belongs to the set of default scopes"
                                );
                            }

                            debugger.Info("Performing unload");
                            await DoUnloadExtraScope(scopeId, scope, destroy);
                            debugger.End();
                        });
                    }
                }
            }
        }
    }
}