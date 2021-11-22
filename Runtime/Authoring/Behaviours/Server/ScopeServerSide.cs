using AlephVault.Unity.Meetgard.Scopes.Types.Constants;
using AlephVault.Unity.Support.Authoring.Behaviours;
using System;
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
                ///   <para>
                ///     The server side implementation of a scope. The purpose
                ///     of this class is to provide both a space to group many
                ///     connections to communicate between themselves (by being
                ///     them on the same hierarchy).
                ///   </para>
                ///   <para>
                ///     How the in-scope objects will be synchronized, is out
                ///     of scope. But a mean, or a moment in time, will be given.
                ///   </para>
                ///   <para>
                ///     Typically, these scopes will be instantiated out of
                ///     prefabs. Otherwise, some sort of standardized mechanism
                ///     will exist to instantiate this scope and the matching
                ///     client side implementation of this scope.
                ///   </para>
                /// </summary>
                public partial class ScopeServerSide : MonoBehaviour
                {
                    // Whether to debug or not using XDebug.
                    private static bool debug = false;

                    /// <summary>
                    ///   The key for this scope. Only meaningful if the
                    ///   scope is to be used as an extra scope prefab.
                    /// </summary>
                    [SerializeField]
                    private string key;

                    /// <summary>
                    ///   See <see cref="key"/>.
                    /// </summary>
                    public string PrefabKey => key;

                    /// <summary>
                    ///   The ID of the prefab. It will either be a
                    ///   virtual/reserved prefab, a prefab index, or
                    ///   the <see cref="Scope.DefaultPrefab"/> constant.
                    ///   In the case of "prefab index", it will belong
                    ///   to the "extra prefabs", and not the "default"
                    ///   ones, of the underlying server.
                    /// </summary>
                    public uint PrefabId { get; internal set; }

                    /// <summary>
                    ///   The ID of this scope. It is always >= 1.
                    ///   If <see cref="PrefabId"/> is equal to the
                    ///   <see cref="Scope.DefaultPrefab"/> constant,
                    ///   then the actual prefab id will equal to this
                    ///   value minus 1 (and the corresponding prefab
                    ///   list will be different).
                    /// </summary>
                    public uint Id { get; internal set; }

                    /// <summary>
                    ///   The protocol server side this scope was created
                    ///   into. Helpful for sending messages of all sorts.
                    /// </summary>
                    public ScopesProtocolServerSide Protocol { get; internal set; }

                    /// <summary>
                    ///   Tells whether this scope is ready to manipulate
                    ///   server side logic (e.g. interacting with the
                    ///   protocol and messages).
                    /// </summary>
                    public bool Ready => Protocol != null;

                    /// <summary>
                    ///   Triggered when the scope is told to load.
                    /// </summary>
                    public event Func<Task> OnLoad = null;

                    /// <summary>
                    ///   Triggered when a connection joined this scope.
                    /// </summary>
                    public event Func<ulong, Task> OnJoining = null;

                    /// <summary>
                    ///   Triggered when a connection, so far in this scope,
                    ///   leaves the scope.
                    /// </summary>
                    public event Func<ulong, Task> OnLeaving = null;

                    /// <summary>
                    ///   Triggered when a connection, so far in this scope,
                    ///   disconnects from the game.
                    /// </summary>
                    public event Func<ulong, Task> OnGoodBye = null;

                    /// <summary>
                    ///   Triggered when the scope is told to unload.
                    /// </summary>
                    public event Func<Task> OnUnload = null;

                    private void Awake()
                    {
                        OnJoining += SyncExistingObjectsTo;
                    }
                }
            }
        }
    }
}
