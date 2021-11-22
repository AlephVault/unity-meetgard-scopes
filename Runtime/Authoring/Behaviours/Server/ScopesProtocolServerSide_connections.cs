using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using AlephVault.Unity.Meetgard.Scopes.Types.Constants;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols.Messages;
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
                public partial class ScopesProtocolServerSide : ProtocolServerSide<ScopesProtocolDefinition>
                {
                    // A reverse map between a connection id and the scope it belongs to.
                    // Virtual / reserved scopes can be specified here.
                    private Dictionary<ulong, uint> scopeForConnection = new Dictionary<ulong, uint>();

                    /// <summary>
                    ///   This event is triggered when a new connection arrived and was
                    ///   set to the limbo in that moment.
                    /// </summary>
                    public event Func<ulong, Task> OnWelcome = null;

                    /// <summary>
                    ///   This event is triggered when a connection leaves a scope.
                    ///   Sent when changing scope (leaving the current one), even
                    ///   if the scope is virtual/reserved. A default implementation
                    ///   will exist: to notify the scope if non-reserved, and to
                    ///   remove the connection from the scope (also, ig non-reserved).
                    /// </summary>
                    public event Func<ulong, uint, Task> OnLeavingScope = null;

                    /// <summary>
                    ///   This event is triggered when a connection joins a scope.
                    ///   Sent when changing scope (joining a new one), even if
                    ///   the scope is virtual/reserved. A default implementation
                    ///   will exist: to add the connection to the scope (if
                    ///   non-reserved), and to notify the scope (also, if
                    ///   non-reserved).
                    /// </summary>
                    public event Func<ulong, uint, Task> OnJoiningScope = null;

                    /// <summary>
                    ///   This event is triggered when a connection left the game.
                    ///   A default implementation will exist: to remove the connection
                    ///   and to notify the scope (if non-reserved).
                    /// </summary>
                    public event Func<ulong, uint, Task> OnGoodBye = null;

                    // Clears all of the connections in a given scope.
                    private async Task ClearConnectionsFromScope(ScopeServerSide scope)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"ClearConnectionsFromScope({scope.Id})", debug);
                        debugger.Start();
                        // Create the limbo message.
                        var message = new MovedToScope()
                        {
                            PrefabIndex = Scope.LimboPrefab,
                            ScopeIndex = Scope.Limbo
                        };
                        debugger.Info("Sending all the connections to Limbo");
                        // Then send it to each connection in the scope.
                        foreach (ulong connection in scope.connections)
                        {
                            scopeForConnection[connection] = Scope.Limbo;
                            try
                            {
                                _ = SendMovedToScope(connection, message);
                            }
                            catch { /* Diaper-ignore */ }
                        }
                        scope.connections.Clear();
                        debugger.End();
                    }

                    // Default implementation for the OnLeavingScope event.
                    private async Task DefaultOnLeavingScope(ulong connectionId, uint scopeId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"DefaultOnLeavingScope({connectionId}, {scopeId})", debug);
                        debugger.Start();
                        // There is no notification to send here.
                        switch(scopeId)
                        {
                            case Scope.Limbo:
                            case Scope.Maintenance:
                                break;
                            default:
                                debugger.Info($"Checking the scope {scopeId} is loaded");
                                if (loadedScopes.TryGetValue(scopeId, out ScopeServerSide scope))
                                {
                                    debugger.Info($"Removing the connection from the scope");
                                    scope.connections.Remove(connectionId);
                                    debugger.Info($"Notifying the connection left the scope");
                                    await scope.TriggerOnLeaving(connectionId);
                                };
                                break;
                        }
                        debugger.End();
                    }

                    // Default implementation for the OnJoiningScope event.
                    private async Task DefaultOnJoiningScope(ulong connectionId, uint scopeId)
                    {
                        // There is no notification to send here.
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"DefaultOnJoiningScope({connectionId}, {scopeId})", debug);
                        debugger.Start();
                        switch (scopeId)
                        {
                            case Scope.Limbo:
                                _ = SendMovedToScope(connectionId, new MovedToScope() { PrefabIndex = Scope.LimboPrefab, ScopeIndex = scopeId });
                                break;
                            case Scope.Maintenance:
                                _ = SendMovedToScope(connectionId, new MovedToScope() { PrefabIndex = Scope.MaintenancePrefab, ScopeIndex = scopeId });
                                break;
                            default:
                                debugger.Info($"Checking the scope {scopeId} is loaded");
                                if (loadedScopes.TryGetValue(scopeId, out ScopeServerSide scope)) {
                                    debugger.Info("Adding the connection to the scope");
                                    scope.connections.Add(connectionId);
                                    debugger.Info("Notifying to the connection that it joined a new scope");
                                    _ = SendMovedToScope(connectionId, new MovedToScope() { PrefabIndex = scope.PrefabId, ScopeIndex = scopeId });
                                    debugger.Info("Notifying the connection joined the scope");
                                    await scope.TriggerOnJoining(connectionId);
                                } else {
                                    debugger.Info("Scope not found. Sending the connection to Limbo");
                                    _ = SendToLimbo(connectionId);
                                }
                                break;
                        }
                        debugger.End();
                    }

                    // Default implementation for the OnGoodBye event.
                    // Yes: It also takes the current scope id, but must
                    // depart silently in that case.
                    private async Task DefaultOnGoodBye(ulong connectionId, uint scopeId)
                    {
                        // There is no notification to send here.
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"DefaultOnJoiningScope({connectionId}, {scopeId})", debug);
                        debugger.Start();
                        switch (scopeId)
                        {
                            case Scope.Limbo:
                            case Scope.Maintenance:
                                break;
                            default:
                                debugger.Info($"Checking the scope {scopeId} is loaded");
                                if (loadedScopes.TryGetValue(scopeId, out ScopeServerSide scope))
                                {
                                    debugger.Info($"Removing the connection from the scope");
                                    scope.connections.Remove(connectionId);
                                    debugger.Info($"Notifying the connection left the scope and the game");
                                    await scope.TriggerOnGoodBye(connectionId);
                                };
                                break;
                        }
                        debugger.End();
                    }

                    /// <summary>
                    ///   Sends a connection, which must exist, to another scope.
                    ///   It is removed from the current scope (it will have one),
                    ///   and added to a new scope.
                    /// </summary>
                    /// <param name="connectionId">The id of the connection being moved</param>
                    /// <param name="newScopeId">The id of the new scope to move the connection to</param>
                    /// <param name="force">Whether to execute the logic, even if the scope is the same</param>
                    public Task SendTo(ulong connectionId, uint newScopeId, bool force = false)
                    {
                        return RunInMainThread(async () => {
                            XDebug debugger = new XDebug("Meetgard.Scopes", this, $"SendTo({connectionId}, {newScopeId})", debug);
                            debugger.Start();
                            uint scopePrefabId;
                            try
                            {
                                switch(newScopeId)
                                {
                                    case Scope.Limbo:
                                        scopePrefabId = Scope.LimboPrefab;
                                        break;
                                    case Scope.Maintenance:
                                        scopePrefabId = Scope.Maintenance;
                                        break;
                                    default:
                                        scopePrefabId = loadedScopes[newScopeId].PrefabId;
                                        break;
                                }
                            }
                            catch(KeyNotFoundException)
                            {
                                debugger.End();
                                throw new InvalidOperationException("The specified new scope does not exist");
                            }

                            uint currentScopeId;
                            try
                            {
                                currentScopeId = scopeForConnection[connectionId];
                            }
                            catch(KeyNotFoundException)
                            {
                                debugger.End();
                                throw new InvalidOperationException("The specified connection does not exist");
                            }

                            debugger.Info("Checking whether it is a different scope, or it is forced");
                            if (force || currentScopeId != newScopeId)
                            {
                                debugger.Info($"Triggering OnLeavingScope({connectionId}, {currentScopeId})");
                                await (OnLeavingScope?.InvokeAsync(connectionId, currentScopeId, async (e) => {
                                    Debug.LogError(
                                        $"An error of type {e.GetType().FullName} has occurred in server side's OnLeavingScope event. " +
                                        $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                        $"The exception details are: {e.Message}"
                                    );
                                }) ?? Task.CompletedTask);
                                scopeForConnection[connectionId] = newScopeId;
                                debugger.Info($"Triggering OnJoiningScope({connectionId}, {newScopeId})");
                                await (OnJoiningScope?.InvokeAsync(connectionId, newScopeId, async (e) => {
                                    Debug.LogError(
                                        $"An error of type {e.GetType().FullName} has occurred in server side's OnJoiningScope event. " +
                                        $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                        $"The exception details are: {e.Message}"
                                    );
                                }) ?? Task.CompletedTask);
                            }
                            debugger.End();
                        });
                    }

                    /// <summary>
                    ///   Sends a connection, which must exist, to the
                    ///   limbo. This task is queued.
                    /// </summary>
                    /// <param name="connectionId">The id of the connection being moved</param>
                    public Task SendToLimbo(ulong connectionId)
                    {
                        return SendTo(connectionId, Scope.Limbo);
                    }
                }
            }
        }
    }
}