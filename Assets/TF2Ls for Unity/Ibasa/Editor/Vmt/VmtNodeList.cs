using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ibasa.Valve.Vmt
{
    /// <summary>
    /// Represents an ordered collection of nodes.
    /// </summary>
    public abstract class VmtNodeList : IEnumerable<VmtNode>
    {
        // Summary:
        //     Initializes a new instance of the System.Xml.XmlNodeList class.
        protected VmtNodeList()
        {

        }

        // Summary:
        //     Gets the number of nodes in the XmlNodeList.
        //
        // Returns:
        //     The number of nodes.
        public abstract int Count { get; }

        // Summary:
        //     Retrieves a node at the given index.
        //
        // Parameters:
        //   i:
        //     Zero-based index into the list of nodes.
        //
        // Returns:
        //     The System.Xml.XmlNode in the collection. If index is greater than or equal
        //     to the number of nodes in the list, this returns null.
        //public virtual VmtNode this[int i] { get { return Item(i); } }

        // Summary:
        //     Provides a simple "foreach" style iteration over the collection of nodes
        //     in the XmlNodeList.
        //
        // Returns:
        //     An System.Collections.IEnumerator.
        public abstract IEnumerator<VmtNode> GetEnumerator();

        // Summary:
        //     Provides a simple "foreach" style iteration over the collection of nodes
        //     in the XmlNodeList.
        //
        // Returns:
        //     An System.Collections.IEnumerator.
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        //
        // Summary:
        //     Retrieves a node at the given index.
        //
        // Parameters:
        //   index:
        //     Zero-based index into the list of nodes.
        //
        // Returns:
        //     The System.Xml.XmlNode in the collection. If index is greater than or equal
        //     to the number of nodes in the list, this returns null.
        public abstract VmtNode Item(int index);
    }
}