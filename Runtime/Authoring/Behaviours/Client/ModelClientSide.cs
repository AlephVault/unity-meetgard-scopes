using AlephVault.Unity.Binary;
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
                ///   This client side implementation of an object maintains
                ///   certain data elements that will be kept synchronizable
                ///   on spawn and refresh. 
                /// </summary>
                public abstract class ModelClientSide<SpawnType, RefreshType> : ObjectClientSide
                    where SpawnType : ISerializable, new()
                    where RefreshType : ISerializable, new()
                {
                    // Whether to debug or not using XDebug.
                    private static bool debug = false;

                    /// <inheritdoc/>
                    protected override void ReadSpawnData(byte[] data)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"ReadSpawnData() [current id: {Id}]", debug);
                        debugger.Start();
                        SpawnType obj = new SpawnType();
                        BinaryUtils.Load(obj, data);
                        debugger.Info("Inflating");
                        InflateFrom(obj);
                        debugger.End();
                    }

                    /// <summary>
                    ///   Inflates the current object from the input data.
                    /// </summary>
                    /// <param name="fullData">The full data to inflate the object from</param>
                    protected abstract void InflateFrom(SpawnType fullData);

                    /// <inheritdoc/>
                    protected override ISerializable ReadRefreshData(byte[] data)
                    {
                        XDebug debugger = new XDebug("Meetgard.Scopes", this, $"ReadRefreshData() [current id: {Id}]", debug);
                        debugger.Start();
                        RefreshType obj = new RefreshType();
                        BinaryUtils.Load(obj, data);
                        debugger.Info("Updating");
                        UpdateFrom(obj);
                        debugger.End();
                        return obj;
                    }

                    /// <summary>
                    ///   Updates the current object from the input data.
                    /// </summary>
                    /// <param name="refreshData">The refresh data to update the object from</param>
                    protected abstract void UpdateFrom(RefreshType refreshData);
                }
            }
        }
    }
}

