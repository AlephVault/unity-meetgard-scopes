using AlephVault.Unity.Support.Utils;
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
                public partial class ScopeServerSide : MonoBehaviour
                {
                    // These methods are internal and are invoked in the context
                    // of the global server - never by this scope on itself. This
                    // also means these methods will be invoked in the context of
                    // a queued task in the main global server.

                    // The set of connections in the current scope. They will be
                    // also managed in the context of the server.
                    internal HashSet<ulong> connections = new HashSet<ulong>();

                    /// <summary>
                    ///   Initializes the scope. Typically, this invokes
                    ///   registered callbacks to work.
                    /// </summary>
                    internal async Task Load()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "Load()", debug);
                        debugger.Start();
                        debugger.Info("Triggering OnLoad");
                        await (OnLoad?.InvokeAsync(async (e) => {
                            Debug.LogException(e);
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in scope server side's OnLoad event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. "
                            );
                        }) ?? Task.CompletedTask);
                        debugger.End();
                    }

                    /// <summary>
                    ///   Finalizes the scope. Typically, this invokes
                    ///   registered callbacks to work.
                    /// </summary>
                    internal async Task Unload()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "Unload()", debug);
                        debugger.Start();
                        debugger.Info("Triggering OnUnload");
                        await (OnUnload?.InvokeAsync(async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in scope server side's OnUnload event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        }) ?? Task.CompletedTask);
                        debugger.End();
                    }

                    /// <summary>
                    ///   Triggers the <see cref="OnJoining"/> event.
                    ///   A default implementation will synchronize
                    ///   all the existing objects into the connection.
                    /// </summary>
                    /// <param name="connectionId">The id of the joining connection</param>
                    internal async Task TriggerOnJoining(ulong connectionId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "TriggerOnJoining()", debug);
                        debugger.Start();
                        debugger.Info("Triggering OnJoining");
                        await (OnJoining?.InvokeAsync(connectionId, async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in scope server side's OnJoining event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        }) ?? Task.CompletedTask);
                        debugger.End();
                    }

                    /// <summary>
                    ///   Triggers the <see cref="OnLeaving"/> event.
                    /// </summary>
                    internal async Task TriggerOnLeaving(ulong connectionId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "TriggerOnLeaving()", debug);
                        debugger.Start();
                        debugger.Info("Triggering OnLeaving");
                        await (OnLeaving?.InvokeAsync(connectionId, async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in scope server side's OnLeaving event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        }) ?? Task.CompletedTask);
                        debugger.End();
                    }

                    /// <summary>
                    ///   Triggers the <see cref="OnGoodBye"/> event.
                    ///   This invocation is queued in the per-scope
                    ///   async queue.
                    /// </summary>
                    internal async Task TriggerOnGoodBye(ulong connectionId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "TriggerOnGoodBye()", debug);
                        debugger.Start();
                        debugger.Info("Triggering OnGoodBye");
                        await (OnGoodBye?.InvokeAsync(connectionId, async (e) => {
                            Debug.LogError(
                                $"An error of type {e.GetType().FullName} has occurred in scope server side's OnGoodBye event. " +
                                $"If the exceptions are not properly handled, the game state might be inconsistent. " +
                                $"The exception details are: {e.Message}"
                            );
                        }) ?? Task.CompletedTask);
                        debugger.End();
                    }

                    /// <summary>
                    ///   Returns an iterator of all the connections in the scope.
                    /// </summary>
                    /// <returns>The iterator</returns>
                    public IEnumerable<ulong> Connections()
                    {
                        foreach (ulong connection in connections) yield return connection;
                    }
                }
            }
        }
    }
}
