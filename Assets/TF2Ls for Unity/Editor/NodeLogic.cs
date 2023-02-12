using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TF2Ls
{
    public class Node
    {
        public string name = string.Empty;
        public string property = string.Empty;

        public Node parent;
        public List<Node> children = new List<Node>();

        // For easy access of children, doesn't work if multiple nodes with the same parent share names
        public Dictionary<string, Node> childrenDictionary = new Dictionary<string, Node>();
        public Node Get(string name) => childrenDictionary[name];

        // Only useful in Unity Editor
        public bool foldout;
    }

    public class NodeReader
    {
        public Node rootNode;

        /// <summary>
        /// Useful for debug purposes, or if you know the range of data you want
        /// </summary>
        public Vector2 lengthRange = new Vector2(0, int.MaxValue);

        public NodeReader(string file)
        {
            rootNode = new Node();
            Node currentNode = rootNode;
            Node newNode;

            string stringBuilder = string.Empty;
            bool quoteOpened = false;
            bool nodeComplete = false;
            for (int i = (int)lengthRange.x; i < (int)Mathf.Min(file.Length, lengthRange.y); i++)
            {
                switch (file[i])
                {
                    case '\"':
                        if (quoteOpened)
                        {
                            if (nodeComplete)
                            {
                                newNode = new Node();
                                newNode.parent = currentNode.parent;
                                currentNode.parent.children.Add(newNode);
                                currentNode = newNode;
                                nodeComplete = false;
                            }

                            if (currentNode.name == string.Empty)
                            {
                                currentNode.name = stringBuilder;
                            }
                            else
                            {
                                currentNode.property = stringBuilder;
                                nodeComplete = true;
                            }
                        }
                        else
                        {
                            stringBuilder = string.Empty;
                        }
                        quoteOpened = !quoteOpened;
                        break;
                    case '{':
                        newNode = new Node();
                        newNode.parent = currentNode;
                        currentNode.children.Add(newNode);
                        currentNode = newNode;
                        break;
                    case '}':
                        currentNode = currentNode.parent;
                        foreach (var node in currentNode.children)
                        {
                            if (!currentNode.childrenDictionary.ContainsKey(node.name))
                                currentNode.childrenDictionary.Add(node.name, node);
                        }
                        break;
                    default:
                        if (quoteOpened)
                        {
                            // Is this a comment?
                            if (file[i] == '/' && file[i + 1] == '/') i++;
                            else
                            {
                                stringBuilder += file[i];
                            }
                        }
                        break;
                }
            }
        }

        public void PrintTree(Node node)
        {
            Debug.Log(node.name);
            if (node.children.Count == 0)
            {
                Debug.Log(node.property);
            }
            else
            {
                foreach (var p in node.children)
                {
                    PrintTree(p);
                }
            }
        }
    }
}
