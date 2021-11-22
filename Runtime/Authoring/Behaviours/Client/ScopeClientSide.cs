using System;
using UnityEngine;
using AlephVault.Unity.Support.Utils;


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
                ///     The client side implementation of a scope. The purpose
                ///     of this class is to provide a reflection of the content
                ///     that is rendered in the server side of this scope. This
                ///     refers to the objects that will be synchronized from
                ///     the server side.
                ///   </para>
                ///   <para>
                ///     How the in-scope objects will be synchronized, is out
                ///     of scope. But a mean, or a moment in time, will be given.
                ///   </para>
                ///   <para>
                ///     Typically, these scopes will be instantiated out of
                ///     prefabs. Otherwise, some sort of standardized mechanism
                ///     will exist to instantiate this scope and the matching
                ///     server side implementation of this scope.
                ///   </para>
                /// </summary>
                public class ScopeClientSide : MonoBehaviour
                {
                    // Whether to debug or not using XDebug.
                    private static bool debug = false;

                    /// <summary>
                    ///   The id of the current scope. Given by the server.
                    /// </summary>
                    public uint Id { get; internal set; }

                    /// <summary>
                    ///   This event is triggered when the client scope is loading.
                    /// </summary>
                    public event Action OnLoad;

                    /// <summary>
                    ///   This event is triggered when the client scope is unloading.
                    /// </summary>
                    public event Action OnUnload;

                    /// <summary>
                    ///   Initializes the scope. Typically, this invokes
                    ///   registered callbacks (<see cref="OnLoad"/> event)
                    ///   to work.
                    /// </summary>
                    internal void Load()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "Load()", debug);
                        debugger.Start();
                        debugger.Info("Triggering OnLoad");
                        OnLoad?.Invoke();
                        debugger.End();
                    }

                    /// <summary>
                    ///   Finalizes the scope. Typically, this invokes
                    ///   registered callbacks (<see cref="OnLoad"/>
                    ///   event) to work.
                    /// </summary>
                    internal void Unload()
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "Unload()", debug);
                        debugger.Start();
                        debugger.Info("Triggering OnUnload");
                        OnUnload?.Invoke();
                        debugger.End();
                    }
                }
            }
        }
    }
}
