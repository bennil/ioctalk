using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Attributes
{
    /// <summary>
    /// The <see cref="ExposeSubTypeAttribute"/> exposes a specialized sub type during remote transfer when the service interface is not specialized.
    /// Only derived interfaces are allowed. Concrete class types are not supported!
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-27
    /// </remarks>
    [AttributeUsageAttribute(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExposeSubTypeAttribute : System.Attribute
    {
        #region ExposeSubTypeAttribute fields
        // ----------------------------------------------------------------------------------------
        // ExposeSubTypeAttribute fields
        // ----------------------------------------------------------------------------------------

        private Type type;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region ExposeSubTypeAttribute constructors
        // ----------------------------------------------------------------------------------------
        // ExposeSubTypeAttribute constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>ExposeSubTypeAttribute</c> class.
        /// </summary>
        public ExposeSubTypeAttribute()
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region ExposeSubTypeAttribute properties
        // ----------------------------------------------------------------------------------------
        // ExposeSubTypeAttribute properties
        // ----------------------------------------------------------------------------------------


        /// <summary>
        /// Gets or sets the sub interface type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public Type Type
        {
            get { return type; }
            set
            {
                type = value;

                if (type != null
                    && !type.IsInterface)
                {
                    throw new InvalidOperationException(string.Format("The exposed sub type \"{0}\" must be an interface!", type.FullName));
                }
            }
        }


        // ----------------------------------------------------------------------------------------
        #endregion

        #region ExposeSubTypeAttribute methods
        // ----------------------------------------------------------------------------------------
        // ExposeSubTypeAttribute methods
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
