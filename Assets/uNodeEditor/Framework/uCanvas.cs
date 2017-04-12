using System.Collections.Generic;
using UnityEngine;

namespace uNodeEditorFramework
{
    [System.Serializable]
    public class uCanvas : ScriptableObject
    {
        [SerializeField]
        private string canvasName;
        public string Name
        {
            get
            {
                return canvasName;
            }
            set
            {
                canvasName = value;
            }
        }

        [SerializeField]
        private List<uNode> nodes;
        public List<uNode> Nodes
        {
            get
            {
                return nodes;
            }
        }

        [SerializeField]
        private string path;
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
            }
        }


        public void Init(string n)
        {
            canvasName = n;
            nodes = new List<uNode>();
        }

        public void SetParentFromCopy()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].ParentName == "")
                    continue;
                nodes[i].Parent = FindNode(nodes[i].ParentName);
                nodes[i].Parent.AddChild(nodes[i]);
            }
        }

        public uNode FindNode(string name)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].NodeName == name)
                    return nodes[i];
            return null;
        }
    }
}
