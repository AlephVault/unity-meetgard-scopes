using System.IO;
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
                ///   This message has an arbitrary model.
                /// </summary>
                public class WithArbitraryModel : ISerializable
                {
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

                    private void DumpIntoBuffer(Buffer buffer, Serializer serializer)
                    {
                        uint bitPosition1 = (uint)buffer.BitPosition;
                        serializer.Writer.WriteUInt32(0);
                        uint bitPosition2 = (uint)buffer.BitPosition;
                        Model.Serialize(serializer);
                        uint bitPosition3 = (uint)buffer.BitPosition;
                        // We go back to the first position.
                        buffer.BitPosition = bitPosition1;
                        // We send the size in bytes (rounded up).
                        serializer.Writer.WriteUInt32((bitPosition3 - bitPosition2 + 7) / 8);
                        // We go back to the end position.
                        buffer.BitPosition = bitPosition3;
                        // Then we write the remaining bits: 0 or 8 - (bp3-bp2)%8.
                        uint r = (bitPosition3 - bitPosition2) % 8;
                        r = r == 0 ? 0 : 8 - r;
                        for(int i = 0; i < r; i++) buffer.WriteBit(false);
                    }

                    private void DumpIntoStream(Stream stream, Serializer serializer)
                    {
                        uint position1 = (uint)stream.Position;
                        serializer.Writer.WriteUInt32(0);
                        uint position2 = (uint)stream.Position;
                        Model.Serialize(serializer);
                        uint position3 = (uint)stream.Position;
                        // We go back to the first position.
                        stream.Position = position1;
                        // We send the size in bytes (rounded up).
                        serializer.Writer.WriteUInt32(position3 - position2);
                        // We go back to the end position.
                        stream.Position = position3;
                    }
                    
                    public virtual void Serialize(Serializer serializer)
                    {
                        if (!serializer.IsReading)
                        {
                            if (Model != null)
                            {
                                Stream stream = serializer.Writer.GetStream();
                                if (stream is Buffer buffer)
                                {
                                    DumpIntoBuffer(buffer, serializer);
                                }
                                else
                                {
                                    DumpIntoStream(serializer.Writer.GetStream(), serializer);
                                }
                            }
                            else
                            {
                                serializer.Serialize(ref Data, false);
                            }
                        }
                        else
                        {
                            Stream stream = serializer.Reader.GetStream();
                            if (stream is Buffer buffer)
                            {
                                uint bitPos = (uint)buffer.BitPosition;
                                uint size = serializer.Reader.ReadUInt32();
                                buffer.BitPosition = bitPos;
                            }
                            else
                            {
                                uint pos = (uint)stream.Position;
                                uint size = serializer.Reader.ReadUInt32();
                                stream.Position = pos;
                            }

                            serializer.Serialize(ref Data, false);                            
                        }
                    }
                }
            }
        }
    }
}