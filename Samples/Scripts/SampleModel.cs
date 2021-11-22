using AlephVault.Unity.Binary;
using AlephVault.Unity.Support.Generic.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [Serializable]
        public class SampleModel : ISerializable, ICopy<SampleModel>
        {
            public Color32 Color;
            public Vector3 Position;

            public SampleModel Copy(bool deep = false)
            {
                return new SampleModel()
                {
                    Color = Color,
                    Position = Position
                };
            }

            public void Serialize(Serializer serializer)
            {
                serializer.Serialize(ref Color);
                serializer.Serialize(ref Position);
            }
        }
    }
}