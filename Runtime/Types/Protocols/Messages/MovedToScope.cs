using AlephVault.Unity.Binary;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Types
    {
        namespace Protocols
        {
            namespace Messages
            {
                /// <summary>
                ///   A message telling the connection was added to
                ///   a certain scope. This is only for the connection
                ///   and not for any object. The client will dispose
                ///   all of the previous scope objects automatically.
                /// </summary>
                public class MovedToScope : ISerializable
                {
                    /// <summary>
                    ///   The index of the prefab to use. If this value
                    ///   is <see cref="Constants.Scope.DefaultPrefab"/>,
                    ///   then the value in <see cref="ScopeIndex"/> will
                    ///   stand for both the prefab index and scope index
                    ///   among the default setup scopes (in both the
                    ///   server and the client. Otherwise, a value
                    ///   lower than <see cref="Constants.Scope.MaxScopePrefabs"/>
                    ///   will stand for the index of a non-default prefab
                    ///   (i.e. registered as "extra" prefab).
                    /// </summary>
                    public uint PrefabIndex;

                    /// <summary>
                    ///   The effective index of the scope. If the prefab
                    ///   index is <see cref="Constants.Scope.DefaultPrefab"/>,
                    ///   then this index will be lower than the amount of
                    ///   default prefabs/scopes. Otherwise, the scope will
                    ///   be a reserved virtual scope (e.g. Limbo) or one
                    ///   of the non-defauly, dynamically spawned, scopes
                    ///   (spawned from an "extra" prefab). In this latter
                    ///   case, the scope will have an index greater than
                    ///   or equal to the amount of default scopes.
                    /// </summary>
                    public uint ScopeIndex;

                    public void Serialize(Serializer serializer)
                    {
                        serializer.Serialize(ref PrefabIndex);
                        serializer.Serialize(ref ScopeIndex);
                    }
                }
            }
        }
    }
}