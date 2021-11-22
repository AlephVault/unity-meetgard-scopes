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
                public class ObjectRefreshed : ISerializable
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

                    /// <summary>
                    ///   The new data to apply. How the data is received,
                    ///   decoded and interpreted is up to the client. The
                    ///   data may involve a full refresh or some sort of
                    ///   "delta" refresh. What is known: this data is,
                    ///   actually, the dumped contents of an object
                    ///   satisfying <see cref="ISerializable"/>. Both
                    ///   the client and server must agree the underlying
                    ///   type being used to encode/decode the data.
                    /// </summary>
                    public byte[] Data;

                    /// <summary>
                    ///   This is a model to use in place of data. This is
                    ///   actually useful to avoid dumping data to an array
                    ///   and allocating buffers on each run, or having to
                    ///   worry about buffer management. In this case, when
                    ///   this Model is not null (this applies not to read
                    ///   but write only), it will be used instead of using
                    ///   the Data field.
                    /// </summary>
                    public ISerializable Model;

                    /// <summary>
                    ///   This is the size of the model. Only meaninful when
                    ///   the Model is provided.
                    /// </summary>
                    public int ModelSize;

                    public void Serialize(Serializer serializer)
                    {
                        serializer.Serialize(ref ScopeIndex);
                        serializer.Serialize(ref ObjectIndex);
                        if (!serializer.IsReading)
                        {
                            if (Model != null)
                            {
                                serializer.Serialize(ref ModelSize);
                                Model.Serialize(serializer);
                            }
                            else
                            {
                                serializer.Serialize(ref Data);
                            }
                        }
                        else
                        {
                            serializer.Serialize(ref Data);                            
                        }
                    }
                }
            }
        }
    }
}