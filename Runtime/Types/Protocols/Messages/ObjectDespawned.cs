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
                ///   A message telling an object in the current scope
                ///   is being despawned. Any additional current interaction
                ///   must be terminated as well. In client side, the object
                ///   must be destroyed.
                /// </summary>
                public class ObjectDespawned : ISerializable
                {
                    /// <summary>
                    ///   The effective index of the current scope, sent
                    ///   for validity. This message should be discarded
                    ///   if the scope is no more valid.
                    /// </summary>
                    public uint ScopeIndex;

                    /// <summary>
                    ///   The effective index of the object being despawned
                    ///   in the current scope.
                    /// </summary>
                    public uint ObjectIndex;

                    public void Serialize(Serializer serializer)
                    {
                        serializer.Serialize(ref ScopeIndex);
                        serializer.Serialize(ref ObjectIndex);
                    }
                }
            }
        }
    }
}