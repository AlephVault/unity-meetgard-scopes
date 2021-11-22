using AlephVault.Unity.Meetgard.Authoring.Behaviours.Server;
using AlephVault.Unity.Meetgard.Scopes.Types.Protocols;
using AlephVault.Unity.Support.Utils;
using System.Collections.Generic;
using System;


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
                    // This is the reverse lookup for the index of an object prefab.
                    // It is populated on awake, based on the set of registered
                    // prefab objects for this server.
                    private Dictionary<ObjectServerSide, uint> indexByObjectPrefab;

                    // This is a complementary lookup of a prefab by its key. Only
                    // prefabs having a non-empty key will be referenced here. This
                    // is thus an optional feature and not a mandatory one (i.e. it
                    // is NOT expected for the prefab to have a key all the times).
                    private Dictionary<string, ObjectServerSide> prefabByKey;

                    /// <summary>
                    ///   <para>
                    ///     Given a prefab id, instantiates an object in this server.
                    ///     The object is set its prefab id and the current server
                    ///     instance as object properties. It is not, however, spawned
                    ///     in any scope yet, but it is however ready to be spawned.
                    ///   </para>
                    ///   <para>
                    ///     It raises an error when the prefab id is not valid (i.e.
                    ///     outside the [0 .. prefabs.Length) range).
                    ///   </para>
                    /// </summary>
                    /// <param name="prefabId">The index of the prefab to instantiate</param>
                    /// <returns>The object instance</returns>
                    public ObjectServerSide InstantiateHere(uint prefabId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"InstantiateHere({prefabId})", debug);
                        debugger.Start();
                        if (prefabId >= objectPrefabs.Length)
                        {
                            throw new ArgumentOutOfRangeException("prefabId");
                        }
                        debugger.End();

                        // Now the object is to be instantiated and returned.
                        return InstantiateAndFill(objectPrefabs[prefabId], prefabId);
                    }

                    /// <summary>
                    ///   <para>
                    ///     Given a prefab id, instantiates an object in this server.
                    ///     The object is set its prefab id and the current server
                    ///     instance as object properties. It is not, however, spawned
                    ///     in any scope yet, but it is however ready to be spawned.
                    ///   </para>
                    ///   <para>
                    ///     It raises an error when the prefab key is not valid (i.e.
                    ///     null, empty, or unknown among the registered object prefabs).
                    ///   </para>
                    /// </summary>
                    /// <param name="prefab">The key of the prefab to instantiate</param>
                    /// <returns>The object instance</returns>
                    public ObjectServerSide InstantiateHere(string key)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"InstantiateHere({key})", debug);
                        debugger.Start();
                        key = key?.Trim();
                        if (key == null || key.Length == 0)
                        {
                            throw new ArgumentException("The target object prefab key must not be null/empty for instantiation");
                        }

                        ObjectServerSide prefab;
                        uint prefabId;
                        try
                        {
                            prefab = prefabByKey[key];
                            // It is safe now - the prefab will exist and have an index.
                            prefabId = indexByObjectPrefab[prefab];
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new ArgumentException("Unknown prefab key: " + key);
                        }
                        debugger.End();

                        // Now the object is to be instantiated and returned.
                        return InstantiateAndFill(objectPrefabs[prefabId], prefabId);
                    }

                    /// <summary>
                    ///   <para>
                    ///     Given a prefab instance, instantiates an object in this
                    ///     server. The object is set its prefab id and the current
                    ///     server instance as object properties. It is not, however,
                    ///     spawned in any scope yet, but it is however ready to be
                    ///     spawned.
                    ///   </para>
                    ///   <para>
                    ///     It raises an error when the prefab is not valid (i.e. null
                    ///     or not among the registered prefabs).
                    ///   </para>
                    /// </summary>
                    /// <param name="prefab">The prefab to instantiate</param>
                    /// <returns>The object instance</returns>
                    public ObjectServerSide InstantiateHere(ObjectServerSide prefab)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"InstantiateHere({prefab})", debug);
                        debugger.Start();
                        if (prefab == null)
                        {
                            throw new ArgumentNullException("prefab");
                        }

                        uint prefabId;
                        try
                        {
                            prefabId = indexByObjectPrefab[prefab];
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new ArgumentException("Unknown prefab: " + prefab.name);
                        }
                        debugger.End();

                        // Now the object is to be instantiated and returned.
                        return InstantiateAndFill(objectPrefabs[prefabId], prefabId);
                    }

                    // Instantiates and populates the object's id fields.
                    private ObjectServerSide InstantiateAndFill(ObjectServerSide prefab, uint prefabId)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"InstantiateAndFill({prefab}, {prefabId})", debug);
                        debugger.Start();
                        ObjectServerSide instance = Instantiate(prefab);
                        instance.PrefabId = prefabId;
                        instance.Protocol = this;
                        debugger.End();
                        return instance;
                    }
                }
            }
        }
    }
}