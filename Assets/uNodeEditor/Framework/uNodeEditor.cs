using UnityEngine;
using UnityEditor;

namespace uNodeEditorFramework
{
    public class uNodeEditor : EditorWindow
    {
        // Properties
        public static uNodeEditor editor;
        private static int sideWindowWidth = 400;
        private uNodeManager manager;
        private bool flagRepainting;

        public Rect SideWindowRect
        {
            get
            {
                return new Rect(position.width - sideWindowWidth, 0, sideWindowWidth, position.height);
            }
        }

        public Rect CanvasWindowRect
        {
            get
            {
                return new Rect(0, 0, position.width - sideWindowWidth, position.height);
            }
        }


        // Member Functions
        [MenuItem("Tools/uNode Editor")]
        public static void ShowEditor()
        {
            editor = GetWindow<uNodeEditor>();
            editor.minSize = new Vector2(800, 600);
            editor.titleContent = new GUIContent("uNode Editor");
            editor.manager = new uNodeManager();
        }

        private void OnGUI()
        {
            CheckInit();
            manager.DrawNodes();
            DrawSideWindow();
            ProcessEvents();
        }

        private void ProcessEvents()
        {
            // Getting event info
            Event e = Event.current;
            if (e == null || !CanvasWindowRect.Contains(e.mousePosition))
                return;
            uNodeEditorState.mousePosition = e.mousePosition;
            bool leftClick = (e.button == 0), rightClick = (e.button == 1);
            uNodeEditorState.focusedNode = manager.NodeAtPosition(uNodeEditorState.mousePosition);
            uNodeEditorState.selectedInputKnob = manager.InputKnobAtPosition(uNodeEditorState.mousePosition);
            uNodeEditorState.selectedOutputKnob = manager.OutputKnobAtPosition(uNodeEditorState.mousePosition);

            // Event processing
            switch (e.type)
            {
                case EventType.MouseDrag:
                    // Drag selected node
                    if (leftClick && uNodeEditorState.selectedNode != null && uNodeEditorState.outputKnobSaved == null)
                    {
                        uNodeEditorState.mouseDelta = e.delta * (1f / uNodeEditorState.zoom);
                        manager.MoveSelectedNode();
                    }
                    // Drawing a connection line
                    if (leftClick && uNodeEditorState.outputKnobSaved != null)
                    {
                        uNodeEditorState.currentLineDrawn = new uLine();
                        Vector2 mousePosWithZoom = uNodeEditorState.mousePosition * (1f / uNodeEditorState.zoom);
                        uNodeEditorState.currentLineDrawn.start = new Vector3(uNodeEditorState.outputKnobSaved.Rect.x,
                                                                                uNodeEditorState.outputKnobSaved.Rect.y,
                                                                                0f);
                        uNodeEditorState.currentLineDrawn.end = new Vector3(mousePosWithZoom.x,
                                                                             mousePosWithZoom.y,
                                                                             0f);
                    }
                    // Left click on canvas : move around canvas
                    else if (leftClick && uNodeEditorState.outputKnobSaved == null && uNodeEditorState.selectedNode == null)
                    {
                        uNodeEditorState.mouseDelta = e.delta * (1f / uNodeEditorState.zoom);
                        manager.MoveNodesAlongMouse();
                    }
                    break;
                case EventType.MouseUp:
                    // Connection began in event MouseDrag, and release on an input connection of another node
                    // Try to create the connection between the two
                    if (uNodeEditorState.outputKnobSaved != null && uNodeEditorState.selectedInputKnob != null)
                    {
                        manager.AddConnection(uNodeEditorState.outputKnobSaved.Parent, uNodeEditorState.selectedInputKnob.Parent);
                    }
                    // Click on a outputKnob without drawing : Collapse/Show children
                    else if (uNodeEditorState.outputKnobSaved != null && uNodeEditorState.selectedInputKnob == null
                                && uNodeEditorState.selectedOutputKnob == uNodeEditorState.outputKnobSaved)
                    {
                        uNodeEditorState.selectedOutputKnob.ChangeShowState();
                    }
                    else if (uNodeEditorState.selectedNode != null)
                    {
                        manager.CurrentCanvas = uNodeEditorSaveManager.SaveCanvasState(manager.CurrentCanvas);
                    }

                    // Release selected elements
                    uNodeEditorState.currentLineDrawn = null;
                    uNodeEditorState.selectedNode = null;
                    uNodeEditorState.outputKnobSaved = null;
                    break;
                case EventType.MouseDown:
                    // Click on node output : Connection creation begin
                    if (uNodeEditorState.selectedOutputKnob != null)
                    {
                        uNodeEditorState.outputKnobSaved = uNodeEditorState.selectedOutputKnob;
                    }
                    // Click on node input : remove all connection
                    if (uNodeEditorState.selectedInputKnob != null)
                    {
                        manager.RemoveConnection(uNodeEditorState.selectedInputKnob.Parent);
                    }
                    // Click on node : select it
                    else if (uNodeEditorState.focusedNode != null && uNodeEditorState.selectedNode == null)
                    {
                        uNodeEditorState.selectedNode = uNodeEditorState.focusedNode;
                    }

                    // Context clicked on a node's property
                    //if (rightClick && uNodeEditorState.focusedNode != null)
                    //{

                    //}
                    // Context menu when clicked on canvas
                    if (rightClick && uNodeEditorState.focusedNode == null)
                    {
                        manager.ShowCanvasGenericMenu();
                    }
                    // Context menu when clicked on node
                    else if (rightClick && uNodeEditorState.focusedNode != null)
                    {
                        manager.ShowNodeGenericMenu();
                    }
                    break;
                case EventType.ScrollWheel:
                    uNodeEditorState.zoom = Mathf.Min(uNodeEditorSettings.maxZoom, Mathf.Max(uNodeEditorState.zoom - e.delta.y
                                                                                    / uNodeEditorSettings.zoomSpeedFactor,
                                                                                      uNodeEditorSettings.minZoom));
                    break;
                case EventType.KeyDown:
                    if (e.shift && e.keyCode == KeyCode.S && manager.CurrentCanvas != null)
                    {
                        manager.SaveCanvas(manager.CurrentCanvas.Path);
                    }
                    if (e.shift && e.keyCode == KeyCode.Z && manager.CurrentCanvas != null)
                    {
                        uCanvas n = uNodeEditorSaveManager.GetBeforeState();
                        if (n != null)
                            manager.CurrentCanvas = n;
                    }
                    else if (e.shift && e.keyCode == KeyCode.Y && manager.CurrentCanvas != null)
                    {
                        uCanvas n = uNodeEditorSaveManager.GetNextState();
                        if (n != null)
                            manager.CurrentCanvas = n;
                    }
                    break;
            }
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            if (flagRepainting)
            {
                Repaint();
                flagRepainting = false;
            }
        }

        private void DrawSideWindow()
        {
            sideWindowWidth = Mathf.Min(600, Mathf.Max(200, (int)(position.width / 5)));
            GUILayout.BeginArea(SideWindowRect, GUI.skin.box);

            // Management
            GUILayout.Label(new GUIContent("Management"), uNodeEditorSettings.boldStyle);
            if (GUILayout.Button(new GUIContent("New Canvas", "Loads an empty Canvas")))
            {
                manager.NewCanvas();
                flagRepainting = true;
            }

            if (GUILayout.Button(new GUIContent("Load Canvas", "Loads the Canvas")))
            {
                string path = EditorUtility.OpenFilePanel("Load Canvas", "Assets/", "asset");
                manager.LoadCanvas(path);
            }

            if (GUILayout.Button(new GUIContent("Save As", "Saves the Canvas")) && manager.CurrentCanvas != null)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Canvas", "test", "asset", "Save Canvas");
                manager.SaveCanvas(path);
            }

            if (GUILayout.Button(new GUIContent("Save", "Saves the Canvas")) && manager.CurrentCanvas != null)
            {
                manager.SaveCanvas(manager.CurrentCanvas.Path);
            }


            // Actions
            GUILayout.Space(20);
            GUILayout.Label(new GUIContent("Actions"), uNodeEditorSettings.boldStyle);
            if (GUILayout.Button(new GUIContent("New Node", "Creates new uNode")) && manager.CurrentCanvas != null)
            {
                manager.CreateNewNode();
                flagRepainting = true;
            }

            if (GUILayout.Button(new GUIContent("Undo", "Undo Last Action")) && manager.CurrentCanvas != null)
            {
                uCanvas n = uNodeEditorSaveManager.GetBeforeState();
                if (n != null)
                {
                    manager.CurrentCanvas = n;
                    flagRepainting = true;
                }
            }

            if (GUILayout.Button(new GUIContent("Redo", "Redo Last Action")) && manager.CurrentCanvas != null)
            {
                uCanvas n = uNodeEditorSaveManager.GetNextState();
                if (n != null)
                {
                    manager.CurrentCanvas = n;
                    flagRepainting = true;
                }
            }

            if (GUILayout.Button(new GUIContent("Settings", "Open Settings Panel")))
            {
                uNodeSettingsWindow.ShowWindow(this);
            }


            // Information
            GUILayout.Space(20);
            GUILayout.Label("Current Canvas : " + (manager.CurrentCanvas != null ? manager.CurrentCanvas.Name : "No Canvas"), uNodeEditorSettings.boldStyle);
            EditorGUILayout.Slider(new GUIContent("Zoom", "Use the Mousewheel to zoom/unzoom"), uNodeEditorState.zoom, 0.6f, 10.0f);
            GUILayout.Label("Cursor Position : " + uNodeEditorState.mousePosition);
            GUILayout.Label("Cursor delta : " + uNodeEditorState.mouseDelta);
            GUILayout.Label("Focused Node : " + (uNodeEditorState.focusedNode == null ? "null" : uNodeEditorState.focusedNode.Position.ToString()));
            GUILayout.Label("Selected Node : " + (uNodeEditorState.selectedNode == null ? "null" : uNodeEditorState.selectedNode.Position.ToString()));
            GUILayout.Label("Output Knob : " + (uNodeEditorState.outputKnobSaved == null ? "null" : uNodeEditorState.outputKnobSaved.Rect.ToString()));
            GUILayout.Label("Input Knob : " + (uNodeEditorState.selectedInputKnob == null ? "null" : uNodeEditorState.selectedInputKnob.Rect.ToString()));

            GUILayout.Space(20);
            GUILayout.Label("Current Canvas : " + uNodeEditorSaveManager.CurrentIndex);
            GUILayout.Label("Saved Canvas Count : " + uNodeEditorSaveManager.CanvasStates.Count);

            GUILayout.EndArea();
        }

        private void CheckInit()
        {
            if (manager == null)
                manager = new uNodeManager();
        }

        public void OnDestroy()
        {
            uNodeEditorSaveManager.Flush();
            uNodeSettingsWindow.CloseWindow();
            uNodeEditorState.savedCanvas = manager.CurrentCanvas;
        }
    }

    public class uNodeSettingsWindow : EditorWindow
    {
        private static uNodeEditor parent;
        private static uNodeSettingsWindow window;

        public static void CloseWindow()
        {
            if (window != null)
                window.Close();
        }

        public static void ShowWindow(uNodeEditor p)
        {
            parent = p;
            window = (uNodeSettingsWindow)GetWindow(typeof(uNodeSettingsWindow));
            window.titleContent = new GUIContent("Settings");
            window.minSize = new Vector2(300, 260);
            window.position = new Rect(new Vector2(Screen.width / 2f, Screen.height / 2f), window.minSize);
            window.Show();
        }

        private void OnGUI()
        {
            // Settings
            EditorGUILayout.BeginHorizontal();
            uNodeEditorSettings.minZoom = EditorGUILayout.FloatField("Min Zoom", uNodeEditorSettings.minZoom);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            uNodeEditorSettings.maxZoom = EditorGUILayout.FloatField("Max Zoom", uNodeEditorSettings.maxZoom);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            uNodeEditorSettings.zoomSpeedFactor = EditorGUILayout.FloatField("Zoom Speed Factor", uNodeEditorSettings.zoomSpeedFactor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            uNodeEditorSettings.knobWidth = EditorGUILayout.FloatField("Knob Width", uNodeEditorSettings.knobWidth);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            uNodeEditorSettings.knobHeight = EditorGUILayout.FloatField("Knob Height", uNodeEditorSettings.knobHeight);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            uNodeEditorSettings.nodeBackgroundColor = EditorGUILayout.ColorField("Node Background", uNodeEditorSettings.nodeBackgroundColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            uNodeEditorSettings.nodeTitleBackgroundColor = EditorGUILayout.ColorField("Node Title Background", uNodeEditorSettings.nodeTitleBackgroundColor);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space();
            EditorGUILayout.Space();


            // Inputs & Controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Right Click : Context Menu");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Left Click : Drag, Create Connection, Remove Connection");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shift + S : Save Canvas");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shift + Z : Cancel Last Action");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shift + Y : Restore Last Action");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scroll Wheel : Zoom / Unzoom");
            EditorGUILayout.EndHorizontal();

            parent.Repaint();
        }
    }

    public enum MenuItemAction
    {
        AddTerrainNode = 0,
        Save = 1,
        NewCanvas = 2,
        Close = 3, // Not used at the moment
        DeleteNode = 4,
        DuplicateNode = 5,
        DuplicateWithChildren = 6,
    }
}
