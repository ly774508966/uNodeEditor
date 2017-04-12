using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace uNodeEditorFramework
{
    public class uNodeManager
    {
        // Properties
        private uCanvas currentCanvas;
        public uCanvas CurrentCanvas
        {
            get
            {
                return currentCanvas;
            }
            set
            {
                currentCanvas = value;
            }
        }

        
        public uNodeManager()
        {
            currentCanvas = null;
            if (uNodeEditorState.savedCanvas != null)
                currentCanvas = uNodeEditorState.savedCanvas;
            uNodeEditorSaveManager.Flush();
        }


        // Canvas functions
        public void SaveCanvas(string path)
        {
            if (currentCanvas == null)
                return;
            currentCanvas = uNodeEditorSaveManager.SaveNodeCanvas(path, currentCanvas);
        }

        public void LoadCanvas(string path)
        {
            uCanvas c = uNodeEditorSaveManager.LoadNodeCanvas(path);
            if (c == null)
                return;
            currentCanvas = c;
            for (int i = 0; i < currentCanvas.Nodes.Count; i++)
            {
                currentCanvas.Nodes[i].InputKnob = new uKnob(currentCanvas.Nodes[i]);
                currentCanvas.Nodes[i].OutputKnob = new uKnob(currentCanvas.Nodes[i]);
            }
            uNodeEditorSaveManager.Flush();
        }

        public void NewCanvas()
        {
            if (currentCanvas != null)
            {
                uNodeEditorSaveManager.Flush();
                // @To do ask for confirmation to clear the current canvas
                // Ask for save (?)
            }
            currentCanvas = ScriptableObject.CreateInstance<uCanvas>();
            currentCanvas.Init("NewCancas");
            currentCanvas = uNodeEditorSaveManager.SaveCanvasState(currentCanvas);
        }

        public void ShowCanvasGenericMenu()
        {
            GenericMenu m = new GenericMenu();
            m.AddItem(new GUIContent("Add Terrain Node"), false, CanvasMenuItemClick, MenuItemAction.AddTerrainNode);
            m.AddItem(new GUIContent("New Canvas"), false, CanvasMenuItemClick, MenuItemAction.NewCanvas);
            m.AddItem(new GUIContent("Save Canvas"), false, CanvasMenuItemClick, MenuItemAction.Save);
            m.AddItem(new GUIContent("Close"), false, CanvasMenuItemClick, MenuItemAction.Close);
            m.ShowAsContext();
        }

        public void ShowNodeGenericMenu()
        {
            GenericMenu m = new GenericMenu();
            m.AddItem(new GUIContent("Duplicate"), false, NodeMenuItemClicked, MenuItemAction.DuplicateNode);
            m.AddItem(new GUIContent("Delete"), false, NodeMenuItemClicked, MenuItemAction.DeleteNode);
            m.ShowAsContext();
        }


        // Node Utility
        public void DrawNodes()
        {
            if (uNodeEditor.editor == null || currentCanvas == null)
                return;
            EditorZoomArea.Begin(uNodeEditorState.zoom, uNodeEditor.editor.CanvasWindowRect);
            // Draw all nodes
            for (int i = 0; i < currentCanvas.Nodes.Count; i++)
                currentCanvas.Nodes[i].DrawNodeBase();
            // Draw the line being created by the user to connect two nodes
            if (uNodeEditorState.currentLineDrawn != null)
            {
                Handles.BeginGUI();
                Handles.color = Color.red;
                Handles.DrawLine(uNodeEditorState.currentLineDrawn.start, uNodeEditorState.currentLineDrawn.end);
                Handles.EndGUI();
            }
            EditorZoomArea.End();
        }

        public void CreateNewNode()
        {
            if (currentCanvas == null)
                return;
            uNode n = ScriptableObject.CreateInstance<uTerrainNode>();
            Vector2 p = uNodeEditor.editor.CanvasWindowRect.Contains(uNodeEditorState.mousePosition)
                            ? uNodeEditorState.mousePosition 
                            : new Vector2(200f, 200f);
            // @ To do : scale p with EditorZoom
            n.Init("Node" + currentCanvas.Nodes.Count, p);
            currentCanvas.Nodes.Add(n);
            currentCanvas = uNodeEditorSaveManager.SaveCanvasState(currentCanvas);
        }

        public void AddNewNode(uNode n)
        {
            currentCanvas.Nodes.Add(n);
            currentCanvas = uNodeEditorSaveManager.SaveCanvasState(currentCanvas);
        }

        public void DeleteNode(uNode n)
        {
            currentCanvas.Nodes.Remove(n);
            if (n.Parent != null)
                n.Parent.Childrens.Remove(n);
            if (n.Childrens.Count > 0)
            {
                for (int i = 0; i < n.Childrens.Count; i++)
                    n.Childrens[i].Parent = null;
                n.Childrens.Clear();
            }
            Object.DestroyImmediate(n);
            currentCanvas = uNodeEditorSaveManager.SaveCanvasState(currentCanvas);
        }

        public void DuplicateNode(uNode n)
        {
            // Get copy returns n copy without hierarchical info 
            // (except for ParentName, used in Save/Load functions)
            uNode copy = n.GetCopy();
            copy.ParentName = "";
            copy.Position = new Vector2(copy.Position.x + copy.Size.x + 50f, copy.Position.y);
            AddNewNode(copy);
        }

        public void DuplicateNodeWithChildren(uNode n)
        {
            // TO DO
            uNode copy = n.GetCopy();
            copy.ParentName = "";
            copy.Position = new Vector2(copy.Position.x + copy.Size.x + 50f, copy.Position.y);
            AddNewNode(copy);
        }

        public void AddConnection(uNode parent, uNode child)
        {
            if (IsValidConnection(parent, child))
            {
                parent.AddChild(child);
                child.Parent = parent;
                currentCanvas = uNodeEditorSaveManager.SaveCanvasState(currentCanvas);
            }
        }

        public void RemoveConnection(uNode child)
        {
            if (child.Parent != null)
            {
                child.Parent.RemoveChild(child);
                child.Parent = null;
                currentCanvas = uNodeEditorSaveManager.SaveCanvasState(currentCanvas);
            }
        }

        public uNode NodeAtPosition(Vector2 p)
        {
            if (currentCanvas == null)
                return null;
            for (int i = 0; i < currentCanvas.Nodes.Count; i++)
            {
                if (currentCanvas.Nodes[i].IsInNode(p))
                    return currentCanvas.Nodes[i];
            }
            return null;
        }

        public uKnob InputKnobAtPosition(Vector2 p)
        {
            if (currentCanvas == null)
                return null;
            for (int i = 0; i < currentCanvas.Nodes.Count; i++)
            {
                if (currentCanvas.Nodes[i].IsInInputKnob(p))
                    return currentCanvas.Nodes[i].InputKnob;
            }
            return null;
        }

        public uKnob OutputKnobAtPosition(Vector2 p)
        {
            if (currentCanvas == null)
                return null;
            for (int i = 0; i < currentCanvas.Nodes.Count; i++)
            {
                if (currentCanvas.Nodes[i].IsInOutputKnob(p))
                    return currentCanvas.Nodes[i].OutputKnob;
            }
            return null;
        }

        public void MoveNodesAlongMouse()
        {
            if (currentCanvas == null)
                return;
            for (int i = 0; i < currentCanvas.Nodes.Count; i++)
                currentCanvas.Nodes[i].Position += uNodeEditorState.mouseDelta * (1f / uNodeEditorState.zoom);
        }

        public void MoveSelectedNode()
        {
            uNodeEditorState.selectedNode.MoveNodeWithChildren(uNodeEditorState.mouseDelta * (1f / uNodeEditorState.zoom));
        }


        // Private member functions
        private bool IsValidConnection(uNode parent, uNode child)
        {
            if (parent == child)
                return false;
            if (parent.HasChild(child))
                return false;
            if (child.Parent != null)
                return false;
            return true;
        }

        public void CanvasMenuItemClick(object a)
        {
            MenuItemAction action = (MenuItemAction)a;
            if (action == MenuItemAction.AddTerrainNode)
                CreateNewNode();
            else if (action == MenuItemAction.NewCanvas)
                NewCanvas();
            else if (action == MenuItemAction.Save && CurrentCanvas != null)
                SaveCanvas(CurrentCanvas.Path);
        }

        public void NodeMenuItemClicked(object a)
        {
            MenuItemAction action = (MenuItemAction)a;
            if (action == MenuItemAction.DeleteNode && uNodeEditorState.focusedNode != null)
            {
                DeleteNode(uNodeEditorState.focusedNode);
            }
            else if (action == MenuItemAction.DuplicateNode && uNodeEditorState.focusedNode != null)
            {
                DuplicateNode(uNodeEditorState.focusedNode);
            }
            else if (action == MenuItemAction.DuplicateWithChildren && uNodeEditorState.focusedNode != null)
            {
                DuplicateNodeWithChildren(uNodeEditorState.focusedNode);
            }
        }
    }


    public static class uNodeEditorSaveManager
    {
        // Properties
        private static List<uCanvas> canvasStates;
        public static List<uCanvas> CanvasStates
        {
            get
            {
                return canvasStates;
            }
        }

        private static int currentIndex;
        public static int CurrentIndex
        {
            get
            {
                return currentIndex;
            }
        }


        // Constructor
        static uNodeEditorSaveManager()
        {
            canvasStates = new List<uCanvas>();
            currentIndex = -1;
        }


        // Native save
        public static uCanvas SaveNodeCanvas(string path, uCanvas nodeCanvas)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Cannot save: No path to save");
                return nodeCanvas;
            }
            if (nodeCanvas == null)
            {
                Debug.LogWarning("Cannot save: The NodeCanvas is null!");
                return null;
            }

            string p = path;
            path = path.Replace(Application.dataPath, "Assets");

            // Write canvas
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(nodeCanvas, path);

            // Write nodes + contents
            for (int i = 0; i < nodeCanvas.Nodes.Count; i++)
                AddSubAsset(nodeCanvas.Nodes[i], nodeCanvas);

            // Saving
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Returning a copy
            uCanvas ret = CreateWorkingCopy(nodeCanvas);
            ret.Path = p;
            return ret;
        }

        private static void AddSubAsset(ScriptableObject subAsset, ScriptableObject mainAsset)
        {
            AssetDatabase.AddObjectToAsset(subAsset, mainAsset);
            subAsset.hideFlags = HideFlags.HideInHierarchy;
        }

        private static void AddSubAsset(ScriptableObject subAsset, string path)
        {
            AssetDatabase.AddObjectToAsset(subAsset, path);
            subAsset.hideFlags = HideFlags.HideInHierarchy;
        }

        public static uCanvas LoadNodeCanvas(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Cannot load: path is null");
                return null;
            }

            // Fetch all objects in the save file
            ScriptableObject[] objects = ResourceManager.LoadResources<ScriptableObject>(path);
            if (objects == null || objects.Length == 0)
                throw new UnityException("Cannot Load NodeCanvas: The specified path '" + path + "' does not point to a save file!");

            // Filter out the NodeCanvas out of these objects
            uCanvas nodeCanvas = objects.Single((ScriptableObject obj) => (obj as uCanvas) != null) as uCanvas;
            if (nodeCanvas == null)
                throw new UnityException("Cannot Load NodeCanvas: The file at the specified path '" + path + "' is no valid save file as it does not contain a NodeCanvas!");

            uCanvas copy = CreateWorkingCopy(nodeCanvas);

            // Path
            copy.Path = path;

            // Returning
            return copy;
        }

        public static uCanvas CreateWorkingCopy(uCanvas canvas)
        {
            uCanvas ret = ScriptableObject.CreateInstance<uCanvas>();
            ret.Init(canvas.Name);

            // Copy each node
            foreach (uNode n in canvas.Nodes)
            {
                uNode copy = n.GetCopy();
                ret.Nodes.Add(copy);
            }

            // Restore hirearchy
            ret.SetParentFromCopy();

            return ret;
        }


        // Runtime save : used by Undo/Redo functionnality
        public static void Flush()
        {
            canvasStates.Clear();
            currentIndex = -1;
        }

        public static uCanvas SaveCanvasState(uCanvas c)
        {
            // New branch created
            if (currentIndex + 1 < canvasStates.Count)
            {
                for (int i = currentIndex + 1; i < canvasStates.Count; i++)
                    canvasStates.RemoveAt(i);
            }
            canvasStates.Add(CreateWorkingCopy(c));
            currentIndex++;

            // Maximum amout of canvas saved at runtime
            if (canvasStates.Count > uNodeEditorSettings.maxRuntimeCanvasSaved)
            {
                canvasStates.RemoveAt(0);
                currentIndex--;
            }

            return c;
        }

        public static uCanvas GetBeforeState()
        {
            if (currentIndex == -1)
                return null;
            if (currentIndex > 0)
                currentIndex--;
            return CreateWorkingCopy(canvasStates[currentIndex]);
        }

        public static uCanvas GetNextState()
        {
            if (currentIndex == -1)
                return null;
            if (currentIndex + 1 < canvasStates.Count)
                currentIndex++;
            return CreateWorkingCopy(canvasStates[currentIndex]);
        }
    }
}
