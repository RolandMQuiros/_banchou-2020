using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentBehaviourTree
{
    /// <summary>
    /// A behaviour tree leaf node for running an action.
    /// </summary>
    public class ActionNode<TContext> : IBehaviourTreeNode<TContext>
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        private string name;

        /// <summary>
        /// Function to invoke for the action.
        /// </summary>
        private Func<TContext, BehaviourTreeStatus> fn;


        public ActionNode(string name, Func<TContext, BehaviourTreeStatus> fn)
        {
            this.name=name;
            this.fn=fn;
        }

        public BehaviourTreeStatus Tick(TContext context)
        {
            return fn(context);
        }
    }
}
