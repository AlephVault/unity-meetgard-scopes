using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(ScopesProtocolServerSide))]
        public class SampleStorage : MonoBehaviour
        {
            [Serializable]
            public class ScopeEntryObjectsDictionary : Support.Generic.Authoring.Types.Dictionary<uint, SampleModel> {}

#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(ScopeEntryObjectsDictionary))]
            public class ScopeEntryObjectsDictionaryDrawer : Support.Generic.Authoring.Types.DictionaryPropertyDrawer { }
#endif

            [Serializable]
            public struct ScopeEntry
            {
                public float RefreshRate;
                public ScopeEntryObjectsDictionary Objects;
            }

            public ScopeEntry[] Data;
        }
    }
}
