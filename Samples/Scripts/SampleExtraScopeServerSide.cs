using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Server;
using AlephVault.Unity.Support.Authoring.Behaviours;
using AlephVault.Unity.Support.Generic.Types.Sampling;
using AlephVault.Unity.Support.Types.Sampling;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(ScopeServerSide))]
        public class SampleExtraScopeServerSide : MonoBehaviour
        {
            ScopeServerSide scope;

            private float refreshRate;
            private float currentRefresh = 0;
            private bool loaded = false;

            private void Awake()
            {
                scope = GetComponent<ScopeServerSide>();
                scope.OnLoad += Scope_OnLoad;
                scope.OnUnload += Scope_OnUnload;
                scope.OnLeaving += Scope_OnLeaving;
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
                foreach (ulong connection in scope.Connections())
                {
                    scope.Protocol.RunInMainThread(() => scope.RefreshExistingObjectsTo(connection, ""));
                }
            }

            private async Task Scope_OnLeaving(ulong arg)
            {
                // Typically, the dev will make async calls here.
                Debug.Log("ServerSideScopeScope::OnLeaving");
                if (scope.Connections().ToArray().Length == 0)
                {
                    // Destroy this extra scene.
                    var _ = scope.Protocol.UnloadExtraScope(scope.Id);
                }
            }

            private async Task Scope_OnLoad()
            {
                // Typically, the dev will make async calls here.
                Debug.Log("SampleExtraSSS::OnLoad::Begin");
                // Take the protocol and make a default spawn.
                Debug.Log("SampleExtraSSS::OnLoad::--Getting current protocol");
                ScopesProtocolServerSide protocol = scope.Protocol;
                // Instantiate the object.
                Debug.Log($"SampleExtraSSS::OnLoad::--Giving refresh rate and creating one object (Protocol: {protocol})");
                refreshRate = 1f;
                SampleObjectServerSide obj = protocol.InstantiateHere(0) as SampleObjectServerSide;
                Debug.Log("SampleExtraSSS::OnLoad::--Populating new object");
                obj.Color = new Random<Color32>(new Color32[] { Color.white, Color.red, Color.green, Color.blue }).Get();
                obj.Position = new RandomBox3(new Vector3(-4, -4, -4), new Vector3(4, 4, 4)).Get();
                Debug.Log("SampleExtraSSS::OnLoad::--Adding new object to scope (main Thread)");
                var _ = scope.AddObject(obj);
                loaded = true;
                Debug.Log("SampleExtraSSS::OnLoad::End");
            }

            private async Task Scope_OnUnload()
            {
                Debug.Log("SampleExtraSSS::OnLoad::Begin");
                loaded = false;
                Debug.Log("SampleExtraSSS::OnLoad::End");
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
