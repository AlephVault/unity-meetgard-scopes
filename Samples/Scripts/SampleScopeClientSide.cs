using AlephVault.Unity.Meetgard.Scopes.Authoring.Behaviours.Client;
using System.Threading.Tasks;
using UnityEngine;


namespace AlephVault.Unity.Meetgard.Scopes
{
    namespace Samples
    {
        [RequireComponent(typeof(ScopeClientSide))]
        public class SampleScopeClientSide : MonoBehaviour
        {
            ScopeClientSide scope;

            private void Awake()
            {
                scope = GetComponent<ScopeClientSide>();
            }

            private void Start()
            {
                scope.OnLoad += Scope_OnLoad;
                scope.OnUnload += Scope_OnUnload;
            }

            private void Scope_OnLoad()
            {
                Debug.Log("ScopeClientSideScope::OnLoad");
                if (Camera.main)
                {
                    Camera.main.transform.position = new Vector3(
                        transform.position.x,
                        transform.position.y,
                        transform.position.z - 10
                    );
                }
            }

            private void Scope_OnUnload()
            {
                Debug.Log("ScopeClientSideScope::OnUnload");
            }
            private void OnDestroy()
            {
                scope.OnLoad -= Scope_OnLoad;
                scope.OnUnload -= Scope_OnUnload;
            }
        }
    }
}
