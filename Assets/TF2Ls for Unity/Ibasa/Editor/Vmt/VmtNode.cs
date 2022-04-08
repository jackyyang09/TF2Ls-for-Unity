using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ibasa.Valve.Vmt
{
    /// <summary>
    /// Represents a single node in the VMT document.
    /// </summary>
    public abstract class VmtNode
    {
        public string Name { get; set; }

        public abstract string InnerText { get; }
        public abstract string OuterText { get; }

        protected static string Escape(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value;

            if (value.Any((c) => char.IsWhiteSpace(c)))
                return string.Concat("\"", value, "\"");

            return value;
        }

        #region Children
        /// <summary>
        /// Gets a value indicating whether this node has any child nodes.
        /// </summary>
        /// <returns>
        /// true if the node has child nodes; otherwise, false.
        /// </returns>
        public virtual bool HasChildNodes { get; }
        /// <summary>
        /// Gets all the child nodes of the node.
        /// </summary>
        /// <returns>
        /// An VmtNodeList that contains all the child nodes of the node.If
        /// there are no child nodes, this property returns an empty VmtNodeList.
        /// </returns>
        public virtual VmtNodeList ChildNodes { get; }
        /// <summary>
        /// Gets the first child of the node.
        /// </summary>
        /// <returns>
        /// The first child of the node. If there is no such node, null is returned.
        /// </returns>
        public virtual VmtNode FirstChild { get; }
        /// <summary>
        /// Gets the last child of the node.
        /// </summary>
        /// <returns>
        /// The last child of the node. If there is no such node, null is returned.
        /// </returns>
        public virtual VmtNode LastChild { get; }
        /// <summary>
        /// Gets the node immediately following this node.
        /// </summary>
        /// <returns>
        /// The next VmtNode. If there is no next node, null is returned.
        /// </returns>
        public virtual VmtNode NextSibling { get; }
        /// <summary>
        /// Gets the node immediately preceding this node.
        /// </summary>
        /// <returns>
        /// The preceding VmtNode. If there is no preceding node, null is returned.
        /// </returns>
        public virtual VmtNode PreviousSibling { get; }
        /// <summary>
        /// Gets the parent of this node (for nodes that can have parents).
        /// </summary>
        /// <returns>
        /// The VmtNode that is the parent of the current node. If a node has just been
        /// created and not yet added to the tree, or if it has been removed from the
        /// tree, the parent is null.
        /// </returns>
        public virtual VmtNode ParentNode { get; }
        #endregion

        /// <summary>
        /// When overridden in a derived class, saves all the child nodes of the node
        /// to the specified VmtWriter.
        /// </summary>
        /// <param name="w">The VmtWriter to which you want to save.</param>
        public abstract void WriteContentTo(VmtWriter w);

        /// <summary>
        /// When overridden in a derived class, saves the current node to the specified
        /// VmtWriter.
        /// </summary>
        /// <param name="w">The VmtWriter to which you want to save.</param>
        public abstract void WriteTo(VmtWriter w);
    }

    public sealed class VmtGroupNode : VmtNode
    {
        public new List<VmtNode> ChildNodes { get; private set; }

        public VmtGroupNode()
        {
            ChildNodes = new List<VmtNode>();
        }

        public VmtNode this[string name]
        {
            get { return ChildNodes.Single((node) => string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase)); }
        }

        public override string InnerText
        {
            get
            {
                return string.Join("\n", from child in ChildNodes select child.OuterText);
            }
        }

        public override string OuterText
        {
            get { return string.Concat(Escape(Name), "\n{\n", InnerText, "\n}"); }
        }

        public override void WriteContentTo(VmtWriter w)
        {
        }

        public override void WriteTo(VmtWriter w)
        {
        }
    }
    public sealed class VmtValueNode : VmtNode
    {
        public string Value { get; set; }

        public override string InnerText
        {
            get { return string.Concat(Escape(Name), " ", Escape(Value)); }
        }

        public override string OuterText
        {
            get { return string.Concat(Escape(Name), " ", Escape(Value)); }
        }

        public override void WriteContentTo(VmtWriter w)
        {
        }

        public override void WriteTo(VmtWriter w)
        {
        }
    }

    public sealed class Vmt
    {
        private IEnumerable<string> Tokenize(TextReader reader)
        {
            int c;
            while ((c = reader.Read()) != -1)
            {
                while (char.IsWhiteSpace((char)c) && c != '\n') //want new lines
                {
                    c = reader.Read();
                }

                switch (c)
                {
                    case -1:
                        break;
                    case '/':
                        {
                            if (reader.Read() != '/')
                            {
                                throw new System.IO.InvalidDataException("Expected comment string.");
                            }

                            while (reader.Read() != '\n') { }
                            yield return Environment.NewLine;
                        }
                        break;
                    case '"':
                        {
                            StringBuilder builder = new StringBuilder();

                            while ((c = reader.Read()) != '"' && c != -1)
                            {
                                if (c == '\n')
                                {
                                    throw new System.IO.InvalidDataException("Newline in string.");
                                }
                                builder.Append((char)c);
                            }

                            if (c == -1)
                            {
                                throw new System.IO.InvalidDataException("Expected closing quote.");
                            }

                            yield return builder.ToString();
                        }
                        break;
                    case '{':
                        yield return "{"; break;
                    case '}':
                        yield return "}"; break;
                    case '\n':
                        yield return Environment.NewLine; break;
                    default:
                        {
                            StringBuilder builder = new StringBuilder();

                            do
                            {
                                builder.Append((char)c);
                            } while ((c = reader.Read()) != -1
                                && !char.IsWhiteSpace((char)c)
                                && c != '{' && c != '}' && c != '\n');

                            yield return builder.ToString();
                        }
                        break;
                }
            }
        }

        public VmtGroupNode Root { get; set; }

        public void Load(string path)
        {
            using (var reader = File.OpenText(path))
            {
                Load(reader);
            }
        }
        public void Load(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                Load(reader);
            }
        }

        private bool SkipNewlines(IEnumerator<string> tokens)
        {
            while (tokens.Current == Environment.NewLine)
            {
                if (!tokens.MoveNext())
                    return false;
            }
            return true;
        }
        public void Load(TextReader reader)
        {
            var tokens = Tokenize(reader).GetEnumerator();

            if (!tokens.MoveNext() || !SkipNewlines(tokens) || tokens.Current == "{" || tokens.Current == "}")
                throw new System.IO.InvalidDataException("Expected shader name.");

            string name = tokens.Current;
            if (!tokens.MoveNext())
                throw new System.IO.InvalidDataException("Expected group.");
            Root = ParseGroup(tokens, name);

            if (tokens.MoveNext())
            {
                if (SkipNewlines(tokens))
                    throw new System.IO.InvalidDataException("Expected end of file.");
            }
        }

        private VmtGroupNode ParseGroup(IEnumerator<string> tokens, string name)
        {
            VmtGroupNode node = new VmtGroupNode();
            node.Name = name;

            if (!SkipNewlines(tokens) || tokens.Current != "{")
                throw new System.IO.InvalidDataException("Expected open brace.");

            while (true)
            {
                if (!tokens.MoveNext() || !SkipNewlines(tokens) || tokens.Current == "{")
                    throw new System.IO.InvalidDataException("Expected node name or close brace.");

                //wont find } untill here 
                if (tokens.Current == "}")
                    break;

                string nodeName = tokens.Current;
                if (!tokens.MoveNext())
                    throw new System.IO.InvalidDataException("Expected node value or open brace.");

                if (tokens.Current != Environment.NewLine && tokens.Current != "{")
                    node.ChildNodes.Add(ParseValue(tokens, nodeName));
                else
                    node.ChildNodes.Add(ParseGroup(tokens, nodeName));
            }
            return node;
        }

        private VmtValueNode ParseValue(IEnumerator<string> tokens, string name)
        {
            VmtValueNode node = new VmtValueNode();
            node.Name = name;

            node.Value = tokens.Current;
            if (!tokens.MoveNext())
                throw new System.IO.InvalidDataException("Expected node value or new line.");

            while (tokens.Current != Environment.NewLine)
            {
                node.Value += " " + tokens.Current;
                if (!tokens.MoveNext())
                    throw new System.IO.InvalidDataException("Expected node value or new line.");
            }
            return node;
        }
    }
}