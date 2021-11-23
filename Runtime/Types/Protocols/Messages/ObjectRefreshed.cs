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
                ///   is being refreshed. The client must refresh the
                ///   existing object according to the given data (this
                ///   implies: each object type has its own way for
                ///   data encoding/decoding regarding refresh). Note:
                ///   Is it not forcefully needed that the data type
                ///   used in this message matches the data type used
                ///   in the <see cref="ObjectSpawned"/> messsage for
                ///   a given object type.
                /// </summary>
                public class ObjectRefreshed : WithArbitraryModel
                {
                    /// <summary>
                    ///   The effective index of the current scope, sent
                    ///   for validity. This message should be discarded
                    ///   if the scope is no more valid.
                    /// </summary>
                    public uint ScopeIndex;

                    /// <summary>
                    ///   The effective index of the object being refreshed
                    ///   in the current scope.
                    /// </summary>
                    public uint ObjectIndex;
                    
                    public override void Serialize(Serializer serializer)
                    {
                        serializer.Serialize(ref ScopeIndex);
                        serializer.Serialize(ref ObjectIndex);
                        base.Serialize(serializer);
                    }
                }
            }
        }
    }
}