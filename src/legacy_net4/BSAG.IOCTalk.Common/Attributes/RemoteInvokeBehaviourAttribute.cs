using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Attributes
{
    /// <summary>
    /// The <see cref="RemoteInvokeBehaviourAttribute"/> specifies the remote invoke behaviour.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-11-21
    /// </remarks>
    [AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple = false, Inherited=true)]
    public class RemoteInvokeBehaviourAttribute : System.Attribute
    {
        #region InvokeBehaviourAttribute fields
        // ----------------------------------------------------------------------------------------
        // InvokeBehaviourAttribute fields
        // ----------------------------------------------------------------------------------------
        private bool isAsyncRemoteInvoke = false;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region InvokeBehaviourAttribute constructors
        // ----------------------------------------------------------------------------------------
        // InvokeBehaviourAttribute constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>InvokeBehaviourAttribute</c> class.
        /// </summary>
        public RemoteInvokeBehaviourAttribute()
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region InvokeBehaviourAttribute properties
        // ----------------------------------------------------------------------------------------
        // InvokeBehaviourAttribute properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether this instance is async remote invoke.
        /// If <c>true</c> IOC Talk will call the method non-blocking and activate the automatic message flow control who only expects a response if necessary (buffer full).
        /// This is only valid on methods with return type "void".
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is async remote invoke; otherwise, <c>false</c>.
        /// </value>
        public bool IsAsyncRemoteInvoke
        {
            get { return isAsyncRemoteInvoke; }
            set { isAsyncRemoteInvoke = value; }
        }


        // ----------------------------------------------------------------------------------------
        #endregion

        #region InvokeBehaviourAttribute methods
        // ----------------------------------------------------------------------------------------
        // InvokeBehaviourAttribute methods
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
