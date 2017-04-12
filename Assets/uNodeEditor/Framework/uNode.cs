using System.Collections.Generic;
using UnityEngine;

namespace uNodeEditorFramework
{
    [System.Serializable]
	public class uNode : ScriptableObject
	{
        // Properties
        [SerializeField]
        protected string nodeName;
		public string NodeName
		{
			get
			{
				return nodeName;
			}
			set
			{
				nodeName = value;
			}
		}

        protected uNode parent;
		public uNode Parent
		{
			get
			{
				return parent;
			}
			set
			{
				parent = value;
			}
		}

        protected List<uNode> childrens;
		public List<uNode> Childrens
		{
			get
			{
				return childrens;
			}
            set
            {
                childrens = value;
            }
		}

        [SerializeField]
        protected bool isEnabled;
		public bool Enabled
		{
			get
			{
				return isEnabled;
			}
			set
			{
				isEnabled = value;
                for (int i = 0; i < childrens.Count; i++)
                    childrens[i].isEnabled = isEnabled;
			}
		}

        [SerializeField]
        protected Vector2 position;
		public Vector2 Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
			}
		}

        [SerializeField]
        protected Vector2 size;
		public Vector2 Size
		{
			get
			{
				return size;
			}
			set
			{
				size = value;
			}
		}

        [SerializeField]
        protected Rect selectionRect;
		public Rect SelectionRect
		{
			get
			{
				return selectionRect;
			}
			set
			{
				selectionRect = value;
			}
		}

        [SerializeField]
        protected bool displayed;
		public bool Displayed
		{
			get
			{
				return displayed;
			}
			set
			{
				displayed = value;
			}
		}

        [SerializeField]
        protected bool collapsed;
        public bool Collapsed
        {
            get
            {
                return collapsed;
            }
            set
            {
                collapsed = value;
            }
        }

        protected uKnob inputKnob;
		public uKnob InputKnob
		{
			get { return inputKnob; }
			set { inputKnob = value; }
		}

        protected uKnob outputKnob;
		public uKnob OutputKnob
		{
			get { return outputKnob; }
			set { outputKnob = value; }
		}

        [SerializeField]
        protected string parentName;
        public string ParentName
        {
            get
            {
                return parentName;
            }
            set
            {
                parentName = value;
            }
        }


		// Member Functions
		public virtual void Init(string name, Vector2 pos)
		{
			childrens = new List<uNode>();
			nodeName = name;
			position = pos;
			size = new Vector2(100, 100);
			selectionRect = new Rect();
			displayed = true;
			isEnabled = true;
            inputKnob = new uKnob(this);
            InputKnob.Parent = this;
            outputKnob = new uKnob(this);
            outputKnob.Parent = this;
        }

		public bool IsInInputKnob(Vector2 p)
		{
			return inputKnob.IsInKnob(p);
		}

		public bool IsInOutputKnob(Vector2 p)
		{
			return outputKnob.IsInKnob(p);
		}

		public bool IsInNode(Vector2 p)
		{
            return selectionRect.Contains(p);
		}

		public void AddChild(uNode child)
		{
			childrens.Add(child);
		}

		public void RemoveChild(uNode child)
		{
			childrens.Remove(child);
		}

		public bool HasChild(uNode child)
		{
			return childrens.Contains(child);
		}

        public void Collapse()
        {
            for (int i = 0; i < childrens.Count; i++)
            {
                childrens[i].Collapse();
                childrens[i].Displayed = false;
            }
            collapsed = true;
        }

        public void Show()
        {
            for (int i = 0; i < childrens.Count; i++)
            {
                childrens[i].Show();
                childrens[i].Displayed = true;
            }
            collapsed = false;
        }

        public void MoveNodeWithChildren(Vector2 delta)
        {
            Position += delta;
            for (int i = 0; i < childrens.Count; i++)
                childrens[i].MoveNodeWithChildren(delta);
        }

        // GUI Function
#if UNITY_EDITOR
        public virtual uNode GetCopy()
        {
            uNode copy = CreateInstance<uNode>();
            UnityEditor.EditorUtility.CopySerialized(this, copy);
            copy.childrens = new List<uNode>();
            copy.parentName = parent != null ? parent.nodeName : "";
            copy.InputKnob = new uKnob(copy);
            copy.OutputKnob = new uKnob(copy);
            return copy;
        }

        public virtual void DrawNodeBase()
		{
			if (!displayed)
				return;

			// Node general elements drawing
			Color oldBackgroundColor = GUI.backgroundColor;

			// Node Rect with scale applied : useful for moving the node
			// The size of the node stay the same in the code, it is automatically scaled with EditorZoomArea class.
			Rect nodeRect = new Rect(position, size);
			selectionRect = nodeRect;
			selectionRect = selectionRect.ScaleSizeBy(uNodeEditorState.zoom, uNodeEditor.editor.CanvasWindowRect.TopLeft());

			// Header drawing
			GUI.backgroundColor = uNodeEditorSettings.nodeTitleBackgroundColor;
			Vector2 contentOffset = new Vector2(0f, 25f);
			Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width - 21f, contentOffset.y - 1);
            Rect headerEnableRect = new Rect(nodeRect.x + nodeRect.width - 20f, nodeRect.y, 20f, contentOffset.y - 1);
            GUI.Label(headerRect, nodeName, GUI.skin.box);
            bool enabledThisFrame = GUI.Toggle(headerEnableRect, Enabled, "");
            if (isEnabled != enabledThisFrame)
                Enabled = enabledThisFrame;

            // Body drawing
            GUI.backgroundColor = uNodeEditorSettings.nodeBackgroundColor;
			Rect bodyRect = new Rect(nodeRect.x, nodeRect.y + contentOffset.y, nodeRect.width, nodeRect.height - contentOffset.y);
			GUI.BeginGroup(bodyRect, GUI.skin.box);
			bodyRect.position = Vector2.zero;
			GUILayout.BeginArea(bodyRect, GUI.skin.box);
			DrawNodeContent();
			GUILayout.EndArea();
			GUI.EndGroup();

			// Node knob drawing
			inputKnob.DrawKnob(false);
			outputKnob.DrawKnob(true);

			// Node connection drawing
            if (!collapsed)
            {
                Vector3 outputPos = new Vector3(outputKnob.Rect.x, outputKnob.Rect.y, 0f);
                UnityEditor.Handles.BeginGUI();
                for (int i = 0; i < childrens.Count; i++)
                {
                    Vector3 childInPosition = new Vector3(childrens[i].InputKnob.Rect.x, childrens[i].InputKnob.Rect.y, 0f);
                    UnityEditor.Handles.DrawLine(outputPos, childInPosition);
                }
                UnityEditor.Handles.EndGUI();
            }
			GUI.backgroundColor = oldBackgroundColor;
		}
#endif
        public virtual void DrawNodeContent()
		{
            Debug.LogWarning("You may be manipulating uNode instead of child classes.");
		}
	}


    public class uKnob
    {
        private uNode parent;
        public uNode Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        private Rect rect;
        public Rect Rect
        {
            get
            {
                return rect;
            }
        }

        private Rect selectionRect;
        public Rect SelectionRect
        {
            get
            {
                return selectionRect;
            }
        }

        private bool collapsed;

        public uKnob(uNode p)
        {
            parent = p;
            rect = new Rect(parent.Position, new Vector2(uNodeEditorSettings.knobWidth, uNodeEditorSettings.knobHeight));
        }

        public void DrawKnob(bool output)
        {
            float knobW = uNodeEditorSettings.knobWidth;
            float knobH = uNodeEditorSettings.knobHeight;
            rect.position = parent.Position;
            rect.x += (parent.Size.x / 2.0f) - (knobW / 2.0f);
            if (output)
                rect.y += parent.Size.y + (knobH / 2.0f);
            else
                rect.y -= (knobH + knobH / 2.0f);
            GUI.Box(rect, "Knob");

            selectionRect = rect;
            selectionRect = selectionRect.ScaleSizeBy(uNodeEditorState.zoom, uNodeEditor.editor.CanvasWindowRect.TopLeft());
        }

        public bool IsInKnob(Vector2 p)
        {
            return selectionRect.Contains(p);
        }

        public void ChangeShowState()
        {
            if (collapsed)
                parent.Show();
            else
                parent.Collapse();
            collapsed = !collapsed;
        }
    }
}
