using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace BSAG.IOCTalk.Container.MEF
{

    /// <summary>
    /// Exports a class who is not marked with an export attribute to a MEF container.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExportHelper<T>
    {
        private T export;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportHelper&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="export">The export.</param>
        public ExportHelper(T export)
        {
            this.export = export;
        }

        /// <summary>
        /// Gets the export.
        /// </summary>
        [Export]
        public virtual T Export
        {
            get { return this.export; }
        }

    }
}
