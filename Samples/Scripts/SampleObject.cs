using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(SpriteRenderer))]
        public class SampleObject : MonoBehaviour
        {
            SpriteRenderer renderer;

            private void Awake()
            {
                renderer = GetComponent<SpriteRenderer>();
            }

            public Color32 Color
            {
                get
                {
                    return renderer.color;
                }
                set
                {
                    renderer.color = value;
                }
            }

            public Vector3 Position
            {
                get { return transform.position; }
                set { transform.position = value; }
            }
        }
    }
}
