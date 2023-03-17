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

    public class BasicVDFParser
    {
        public Node rootNode;

        /// <summary>
        /// Useful for debug purposes, or if you know the range of data you want
        /// </summary>
        public Vector2 lengthRange = new Vector2(0, int.MaxValue);

        public BasicVDFParser(string[] lines)
        {
            rootNode = new Node();
            Node currentNode = rootNode;
            Node newNode;

            string stringBuilder = string.Empty;
            bool escapeKey = false;
            bool quoteOpened = false;
            bool nodeComplete = false;
            for (int i = (int)lengthRange.x; i < (int)Mathf.Min(lines.Length, lengthRange.y); i++)
            {
                for (int j = 0; j < lines[i].Length; j++)
                {
                    switch (lines[i][j])
                    {
                        // Descriptions abuse this
                        case '\\':
                            escapeKey = true;
                            stringBuilder += lines[i][j];
                            break;
                        case '\"':
                            if (quoteOpened)
                            {
                                if (escapeKey)
                                {
                                    stringBuilder += lines[i][j];
                                    escapeKey = false;
                                    break;
                                }

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
                                    currentNode.name = stringBuilder.ToLowerInvariant();
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
                                if (!currentNode.childrenDictionary.ContainsKey(node.name.ToLowerInvariant()))
                                    currentNode.childrenDictionary.Add(node.name.ToLowerInvariant(), node);
                            }
                            break;
                        default:
                            if (quoteOpened)
                            {
                                if (escapeKey)
                                {
                                    escapeKey = false;
                                }
                                stringBuilder += lines[i][j];
                            }
                            else
                            {
                                // Is this a comment?
                                if (lines[i][j] == '/' && lines[i][j + 1] == '/')
                                {
                                    j = lines[i].Length;
                                }
                            }
                            break;
                    }
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
