using UnityEditor.Animations;
using UnityEngine;

namespace ModularMovement.Editor
{
    // Checks the controller's wiring without changing assets or running the Animator.
    public static class PlayerAnimationControllerValidation
    {
        public static string GetIssue(RuntimeAnimatorController runtimeController)
        {
            // An override swaps clips but keeps its base controller's rules. Check those rules.
            while (runtimeController is AnimatorOverrideController overrides)
                runtimeController = overrides.runtimeAnimatorController;
            if (!(runtimeController is AnimatorController controller))
                return "Assign an Animator Controller or an Animator Override Controller with a base controller.";

            // A matching name is not enough: whole numbers choose states, decimal values set progress and pace.
            if (!HasParameter(controller, PlayerAnimation.StateParameterName, AnimatorControllerParameterType.Int))
                return "Add an Int parameter named MovementState for Idle = 0, Run = 1 and Dash = 2.";
            if (!HasParameter(controller, PlayerAnimation.DashProgressParameterName, AnimatorControllerParameterType.Float))
                return "Add a Float parameter named DashProgress to synchronize the dash animation.";
            if (!HasParameter(controller, PlayerAnimation.RunSpeedParameterName, AnimatorControllerParameterType.Float))
                return "Add a Float parameter named RunPlaybackSpeed to scale the running animation.";
            if (controller.layers.Length == 0)
                return "Add a base layer containing Idle, Run and Dash states.";

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            // Dash must use progress supplied by the ability, rather than its clip's original duration.
            AnimatorState dash = FindState(machine, "Dash");
            if (dash == null || dash.motion == null)
                return "Add a Dash state with its animation clip on the base layer.";
            if (!dash.timeParameterActive || dash.timeParameter != PlayerAnimation.DashProgressParameterName)
                return "Enable Motion Time on the Dash state and select DashProgress.";
            // Run uses a speed multiplier. Motion Time would take control away from normal playback.
            AnimatorState run = FindState(machine, "Run");
            if (run == null || run.motion == null)
                return "Add a Run state with its animation clip on the base layer.";
            if (!run.speedParameterActive || run.speedParameter != PlayerAnimation.RunSpeedParameterName)
                return "Enable the Speed multiplier on the Run state and select RunPlaybackSpeed.";
            if (run.timeParameterActive || !Mathf.Approximately(run.speed, 1f))
                return "Set Run Speed to 1 and disable Motion Time on Run so its multiplier controls playback.";
            // No message means these checks found no issue; this does not judge how the animation looks.
            return null;
        }

        private static bool HasParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
                if (parameter.name == name && parameter.type == type) return true;
            return false;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            if (machine == null) return null;
            foreach (ChildAnimatorState child in machine.states)
                if (child.state.name == name) return child.state;
            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
            {
                // States can be grouped inside other state machines, so look inside those groups too.
                AnimatorState state = FindState(child.stateMachine, name);
                if (state != null) return state;
            }
            return null;
        }
    }
}
