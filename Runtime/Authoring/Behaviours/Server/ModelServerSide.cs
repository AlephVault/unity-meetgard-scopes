using AlephVault.Unity.Binary;
using AlephVault.Unity.Support.Utils;
using AlephVault.Unity.Support.Generic.Types;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Server
            {
                /// <summary>
                ///   This server side implementation of an object maintains
                ///   certain data elements that will be kept synchronizable
                ///   on spawn and refresh. 
                /// </summary>
                public abstract class ModelServerSide<SpawnType, RefreshType> : ObjectServerSide
                    where SpawnType : ISerializable, new()
                    where RefreshType : ISerializable, new()
                {
                    // Whether to debug or not using XDebug.
                    private static bool debug = false;

                    /// <summary>
                    ///   Builds the whole layout of values to send
                    ///   to the different subsets of connections,
                    ///   depending on game logic. For tips and more
                    ///   details, see <see cref="GetFullData"/>.
                    /// </summary>
                    /// <param name="connections">The whole connections in a scope</param>
                    /// <returns>A list of pairs (connections, data), so each set of connections can potentially receive different sets of data</returns>
                    public override List<Tuple<HashSet<ulong>, ISerializable>> FullData(HashSet<ulong> connections)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, "FullData(...)", debug);
                        debugger.Start();
                        Dictionary<SpawnType, HashSet<ulong>> results = new Dictionary<SpawnType, HashSet<ulong>>();
                        foreach(ulong connection in connections ?? new HashSet<ulong>())
                        {
                            debugger.Info($"Grouping connection {connection} by full data");
                            results.SetDefault(GetFullData(connection), () => new HashSet<ulong>()).Add(connection);
                        }
                        debugger.End();
                        return (from result in results select new Tuple<HashSet<ulong>, ISerializable>(
                            result.Value, result.Key
                        )).ToList();
                    }

                    /// <summary>
                    ///   Type-aware method to retrieve the appropriate
                    ///   object for full data for a given connection.
                    ///   Tip: This method may be implemented using the
                    ///   concept of "dirty" values so existing objects
                    ///   are not meant to be recomputed each time, until
                    ///   a relevant change is done. Alternatively, ensure
                    ///   an <see cref="Equals(object)"/> is class-defined
                    ///   appropriately.
                    /// </summary>
                    /// <param name="connection">The connection to get the data for</param>
                    /// <returns>The full data for that connection</returns>
                    protected abstract SpawnType GetFullData(ulong connection);

                    /// <inheritdoc />
                    public override ISerializable FullData(ulong connection)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"FullData({connection})", debug);
                        debugger.Start();
                        SpawnType fullData = GetFullData(connection);
                        debugger.End();
                        return fullData;
                    }

                    /// <summary>
                    ///   Type-aware method to retrieve the appropriate
                    ///   object for refresh data for a given connection.
                    ///   Tip: This method may be implemented using the
                    ///   concept of "dirty" values so existing objects
                    ///   are not meant to be recomputed each time, until
                    ///   a relevant change is done.
                    /// </summary>
                    /// <param name="connection">The connection to get the data for</param>
                    /// <param name="context">The refresh context</param>
                    /// <returns>The refresh data for that connection in that context</returns>
                    protected abstract RefreshType GetRefreshData(ulong connection, string context);

                    /// <inheritdoc />
                    public override ISerializable RefreshData(ulong connection, string context)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"RefreshData({connection}, {context})", debug);
                        debugger.Start();
                        RefreshType refreshData = GetRefreshData(connection, context);
                        debugger.End();
                        return refreshData;
                    }
                }
            }
        }
    }
}

