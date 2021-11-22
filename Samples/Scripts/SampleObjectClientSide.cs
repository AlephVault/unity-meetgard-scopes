using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(SampleObject))]
        public class SampleObjectClientSide : ModelClientSide<SampleModel, SampleModel>
        {
            private SampleObject relatedObject;

            private void Awake()
            {
                relatedObject = GetComponent<SampleObject>();
            }

            protected override void InflateFrom(SampleModel fullData)
            {
                relatedObject.Color = fullData.Color;
                relatedObject.Position = fullData.Position;
            }

            protected override void UpdateFrom(SampleModel refreshData)
            {
                relatedObject.Color = refreshData.Color;
                relatedObject.Position = refreshData.Position;
            }
        }
    }
}

