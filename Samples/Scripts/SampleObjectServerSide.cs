using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(SampleObject))]
        public class SampleObjectServerSide : ModelServerSide<SampleModel, SampleModel>
        {
            private SampleObject relatedObject;
            private SampleModel data = new SampleModel();
            private bool dirty = true;
            private SampleModel backgroundData;

            private void Awake()
            {
                relatedObject = GetComponent<SampleObject>();
            }

            public uint InternalId;

            public Color32 Color
            {
                get
                {
                    return relatedObject.Color;
                }
                set
                {
                    relatedObject.Color = value;
                    data.Color = value;
                    dirty = true;
                }
            }

            public Vector3 Position
            {
                get
                {
                    return relatedObject.Position;
                }
                set
                {
                    relatedObject.Position = value;
                    data.Position = value;
                    dirty = true;
                }
            }

            private void UpdateBackgroundData()
            {
                if (dirty)
                {
                    dirty = false;
                    backgroundData = data.Copy();
                }
            }

            protected override SampleModel GetFullData(ulong connection)
            {
                UpdateBackgroundData();
                return backgroundData;
            }

            protected override SampleModel GetRefreshData(ulong connection, string context)
            {
                UpdateBackgroundData();
                return backgroundData;
            }
        }

    }
}