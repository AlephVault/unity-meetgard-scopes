using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;
using AlephVault.Unity.Support.Authoring.Behaviours;
using AlephVault.Unity.Support.Generic.Types.Sampling;
using AlephVault.Unity.Support.Types.Sampling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(ScopeServerSide))]
        public class SampleDefaultScopeServerSide : MonoBehaviour
        {
            private ScopeServerSide scope;

            private float refreshRate;
            private float currentRefresh = 0;
            private bool loaded = false;

            private void Awake()
            {
                scope = GetComponent<ScopeServerSide>();
                scope.OnLoad += Scope_OnLoad;
                scope.OnUnload += Scope_OnUnload;
            }

            private void Update()
            {
                if (loaded)
                {
                    if (currentRefresh >= refreshRate)
                    {
                        currentRefresh -= refreshRate;
                        scope.Protocol.RunInMainThread(DoRefresh);
                    }
                    currentRefresh += Time.deltaTime;
                }
            }

            private void DoRefresh()
            {
                foreach (ObjectServerSide obj in scope.Objects())
                {
                    if (obj is SampleObjectServerSide sobj)
                    {
                        sobj.Color = new Random<Color32>(new Color32[] { Color.white, Color.red, Color.green, Color.blue }).Get();
                        sobj.Position = new RandomBox3(new Vector3(-4, -4, -4), new Vector3(4, 4, 4)).Get();
                    }
                }
                foreach(ulong connection in scope.Connections())
                {
                    scope.Protocol.RunInMainThread(() => scope.RefreshExistingObjectsTo(connection, ""));
                }
            }

            private async Task Scope_OnLoad()
            {
                Debug.Log("Scope::OnLoad");
                // Take the relevant data (protocol, id) and perform
                // the whole load.
                ScopesProtocolServerSide protocol = scope.Protocol;
                uint id = scope.Id;
                SampleStorage.ScopeEntry entry = protocol.GetComponent<SampleStorage>().Data[id - 1];
                refreshRate = entry.RefreshRate;
                foreach(KeyValuePair<uint, SampleModel> pair in entry.Objects)
                {
                    SampleObjectServerSide obj = protocol.InstantiateHere(0) as SampleObjectServerSide;
                    obj.Color = pair.Value.Color;
                    obj.Position = pair.Value.Position;
                    var _ = scope.AddObject(obj);
                }
                loaded = true;
            }

            private async Task Scope_OnUnload()
            {
                loaded = false;
                // Typically, the dev will make async calls here.
                Debug.Log("Scope::OnUnload");
                // Nothing else to do here.
            }

            private void OnDestroy()
            {
                scope.OnLoad -= Scope_OnLoad;
                scope.OnUnload -= Scope_OnUnload;
            }
        }
    }
}
