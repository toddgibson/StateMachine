using System;
using System.Collections.Generic;
using System.Linq;

namespace StateManagement
{
    public class StateMachine
    {
        private List<State> _states = new();
        private List<TransitionCondition> _anyStateTransitionConditions = new();

        private State _currentState;

        public Type PreviousStateType { get; private set; }

        public delegate void StateChangedEventHandler(object sender, StateChangedEventArgs e);
        public event StateChangedEventHandler OnStateChanged;

        public bool IsInEndState => _currentState.IsEndState;

        public void Initialize(List<State> states, List<TransitionCondition> anyStateTransitionConditions)
        {
            if (!states.Any()) return;

            _states = states;
            _states.ForEach(state =>
            {
                state.SetStateMachine(this);
                state.Initialize();

                if (!state.IsEndState && state.TransitionConditionCount == 0)
                    throw new InvalidOperationException($"State {state.GetType()} has no configured transitions. Configure them in the {nameof(Initialize)} method of your state implementation.");
            });
            _anyStateTransitionConditions = anyStateTransitionConditions;

            _currentState = states[0];
            _currentState.OnEnter();
        }

        protected internal void ChangeState(State newState)
        {
            var previousState = _currentState;

            PreviousStateType = previousState.GetType();
            previousState.ClearTimedTickAction();
            previousState.OnExit();

            _currentState = newState;
            _currentState.OnEnter();

            if (OnStateChanged != null)
                OnStateChanged(this, new StateChangedEventArgs(_currentState.GetType(), previousState.GetType()));
        }

        public void Tick(float deltaTime = 0.0f)
        {
            if (_currentState == null) return;

            _currentState.Tick(deltaTime);
            _currentState.UpdateTimer(deltaTime);
            _currentState.CheckStateTransitions();
            CheckAnyStateTransitions();
        }

        private void CheckAnyStateTransitions()
        {
            if (_currentState == null || _currentState.IsEndState) return;

            var validTransition = _anyStateTransitionConditions.FirstOrDefault(transition => transition.Condition());
            if (validTransition == null) return;

            ChangeState(GetStateOfType(validTransition.ToStateType));
        }

        public State GetStateOfType(Type toStateType)
        {
            var nextState = _states.FirstOrDefault(p => p.GetType() == toStateType);
            if (nextState == null)
                throw new InvalidOperationException($"Invalid Transition! State machine does not have a state of type {toStateType}");
            return nextState;
        }
    }

    public class StateChangedEventArgs
    {
        public readonly Type currentState;
        public readonly Type previousState;

        public StateChangedEventArgs(Type currentState, Type previousState)
        {
            this.currentState = currentState;
            this.previousState = previousState;
        }
    }

    public abstract class State
    {
        private StateMachine _stateMachine;

        protected List<TransitionCondition> transitionConditions = new();
        public int TransitionConditionCount => transitionConditions.Count;
        public Type PreviousStateMachineState => _stateMachine.PreviousStateType;

        public void SetStateMachine(StateMachine stateMachine) => _stateMachine = stateMachine;

        public bool IsEndState { get; set; }

        private bool _timedTickEnabled = false;
        private float _tickTimerSeconds = float.MinValue;
        private Action TimedTickAction { get; set; }

        /// <summary>
        /// When implementing this method, configure the Transition Conditions
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Called immediately upon changing to this state
        /// </summary>
        public abstract void OnEnter();
        /// <summary>
        /// Called each frame
        /// </summary>
        public abstract void Tick(float deltaTime = 0.0f);
        /// <summary>
        /// Called immediately upon leaving this state
        /// </summary>
        public abstract void OnExit();

        internal void CheckStateTransitions()
        {
            if (_stateMachine.IsInEndState) return;

            var validTransition = transitionConditions.FirstOrDefault(transition => transition.Condition());
            if (validTransition == null) return;

            _stateMachine.ChangeState(_stateMachine.GetStateOfType(validTransition.ToStateType));
        }

        /// <summary>
        /// Call a method after the specified seconds have passed.
        /// </summary>
        /// <param name="seconds">the amount of seconds to elapse before calling the method</param>
        /// <param name="timedTickAction">the method to be called</param>
        protected void SetTimedTick(float seconds, Action timedTickAction)
        {
            _tickTimerSeconds = seconds;
            _timedTickEnabled = true;
            TimedTickAction = timedTickAction;
        }

        internal void UpdateTimer(float deltaTime)
        {
            if (!_timedTickEnabled) return;

            _tickTimerSeconds -= deltaTime;
            if (_tickTimerSeconds > 0.0f) return;

            _timedTickEnabled = false;

            TimedTickAction?.Invoke();
        }

        internal void ClearTimedTickAction()
        {
            TimedTickAction = null;
        }
    }

    public class TransitionCondition
    {
        public TransitionCondition(Type toStateType, Func<bool> condition)
        {
            ToStateType = toStateType;
            Condition = condition;
        }

        public readonly Type ToStateType;
        public readonly Func<bool> Condition;
    }
}