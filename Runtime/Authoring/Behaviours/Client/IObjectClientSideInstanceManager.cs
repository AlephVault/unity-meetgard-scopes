using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Scopes.Types.Constants;
using AlephVault.Unity.Support.Authoring.Behaviours;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Client
            {
                /// <summary>
                ///   An interfact for instance management (to avoid
                ///   having a lot instantiations/destructions).
                /// </summary>
                public interface IObjectClientSideInstanceManager
                {
                    /// <summary>
                    ///   Gets a new instance. A new one, or a pooled one.
                    /// </summary>
                    /// <param name="prefab">The prefab to get the instance for</param>
                    /// <returns>The instance, either new or retrieved</returns>
                    public ObjectClientSide Get(ObjectClientSide prefab);

                    /// <summary>
                    ///   Releases an instance. The instance is delivered
                    ///   to the pool (and, perhaps, it is destroyed).
                    /// </summary>
                    /// <param name="instance">The instance to release</param>
                    public ObjectClientSide Release(ObjectClientSide instance);
                }
            }
        }
    }
}

