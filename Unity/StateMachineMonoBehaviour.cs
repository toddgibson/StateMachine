using UnityEngine;

namespace StateManagement
{
    /// <summary>
    /// For components with state, inherit from this class.
    /// </summary>
    public abstract class StateMachineMonoBehaviour : MonoBehaviour
    {
        protected readonly StateMachine stateMachine = new();

        /// <summary>
        /// Note: Calling Update from an inherited class will override this. It is recommended that the active state handle any per-frame logic.
        /// </summary>
        private void Update() => stateMachine.Tick();
    }
}