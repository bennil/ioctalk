using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
        /// <summary>
    /// Abstract json structure class
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public abstract class AbstractStructure : IJsonTypeStructure
    {
        #region AbstractStructure fields
        // ----------------------------------------------------------------------------------------
        // AbstractStructure fields
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// JSON key name
        /// </summary>
        protected string key;

        /// <summary>
        /// Key length
        /// </summary>
        protected int keyLength;

        /// <summary>
        /// Is array item flag
        /// </summary>
        protected bool isArrayItem;

        /// <summary>
        /// If key expected flag
        /// </summary>
        protected bool keyExpected = false;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractStructure constructors
        // ----------------------------------------------------------------------------------------
        // AbstractStructure constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>AbstractStructure</c> class.
        /// </summary>
        public AbstractStructure(string key, bool isArrayItem)
        {
            this.key = key;
            this.isArrayItem = isArrayItem;

            if (isArrayItem)
            {
                this.keyLength = 0;
            }
            else
            {
                if (key != null)
                {
                    this.keyLength = key.Length + 3;    // plus quotations and colon separator
                    this.keyExpected = true;
                }
                else
                {
                    this.keyLength = 0;
                }
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractStructure properties
        // ----------------------------------------------------------------------------------------
        // AbstractStructure properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the key.
        /// </summary>
        public string Key
        {
            get { return key; }
        }

        // ----------------------------------------------------------------------------------------
        #endregion
        
        #region AbstractStructure methods
        // ----------------------------------------------------------------------------------------
        // AbstractStructure methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="context">The context.</param>
        public abstract void Serialize(StringBuilder sb, object obj, SerializationContext context);

        /// <summary>
        /// Deserializes the specified json string.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="currentReadIndex">Index of the current read.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public abstract object Deserialize(string json, ref int currentReadIndex, SerializationContext context);

       



        // ----------------------------------------------------------------------------------------
        #endregion



    }

}
