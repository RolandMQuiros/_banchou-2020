// using System.Linq;
// using System.Collections.Generic;

// using UnityEngine;
// using UnityEditor;
// using UnityEditor.Animations;

// namespace Banchou.Pawn.Part {
//     [CustomEditor(typeof(FSMRollback))]
//     public class FSMRollbackInspector : Editor {
//         [SerializeField] private AnimatorController _animatorController = null;

//         private FSMRollback _target;

//         private void OnEnable() {
//             _target = (FSMRollback)target;
//         }

//         public override void OnInspectorGUI() {
//             var newAnimatorController = (AnimatorController)EditorGUILayout.ObjectField(
//                 "AnimatorController",
//                 _animatorController,
//                 typeof(AnimatorController),
//                 allowSceneObjects: false
//             );
//             if (_animatorController != newAnimatorController) {
//                 _animatorController = newAnimatorController;

//                 var transitions = new List<AnimatorStateTransition>();
//                 var stack = new Stack<AnimatorState>(
//                     _animatorController.layers.SelectMany(l => l.stateMachine.states.Select(s => s.state))
//                 );

//                 while (stack.Count > 0) {
//                     var currentState = stack.Pop();

//                     var sourceState = currentState.name;
//                     foreach (var transition in currentState.transitions) {
//                         var duration = transition.duration;
//                         var triggers = transition
//                             .conditions
//                             .Where(
//                                 c => _animatorController.parameters.Any(
//                                     p => p.type == AnimatorControllerParameterType.Trigger && p.name == c.parameter
//                                 )
//                             )
//                             .Select(c => c.parameter);
//                         var transitionDuration = transition.duration;
//                         var destination = transition.destinationState;

//                         stack.Push(destination);
//                     }
//                 }
//             }
//         }
//     }
// }
