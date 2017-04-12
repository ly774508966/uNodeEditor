using UnityEngine;

namespace uNodeEditorFramework
{
    public static class uNodeEditorState
    {
        public static Vector2 mousePosition = Vector2.zero;
        public static Vector2 mouseDelta = Vector2.zero;
        public static float zoom = 1.0f;
        public static uNode focusedNode = null;
        public static uNode selectedNode = null;
        public static uKnob selectedInputKnob = null;
        public static uKnob selectedOutputKnob = null;
        public static uLine currentLineDrawn = null;
        public static uKnob outputKnobSaved = null;

        public static uCanvas savedCanvas = null;
    }

    [System.Serializable]
    public static class uNodeEditorSettings
    {
        // Editor Settings
        public static int maxRuntimeCanvasSaved = 30;

		// Zoom Settings
        public static float maxZoom						= 8.0f;
        public static float minZoom						= 0.1f;
        public static float zoomSpeedFactor				= 35.0f;

		// Graphics Settings
		public static float knobWidth					= 45.0f;
		public static float knobHeight					= 20.0f;
		public static Color nodeBackgroundColor			= new Color(0.8f, 0.8f, 0.8f);
		public static Color nodeTitleBackgroundColor	= new Color(0.6f, 0.6f, 0.6f);
        public static GUIStyle boldStyle                = new GUIStyle();
        
        static uNodeEditorSettings()
        {
            boldStyle.fontStyle = FontStyle.Bold;
        }

        public static void Serialize()
        {
            // @TODO
        }
	}
}
