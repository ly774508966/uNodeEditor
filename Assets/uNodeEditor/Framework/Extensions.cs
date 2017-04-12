using System.Linq;
using UnityEngine;

// Helper Rect extension methods
public static class RectExtensions
{
	public static Vector2 TopLeft(this Rect rect)
	{
		return new Vector2(rect.xMin, rect.yMin);
	}

	public static Rect ScaleSizeBy(this Rect rect, float scale)
	{
		return rect.ScaleSizeBy(scale, rect.center);
	}

	public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
	{
		Rect result = rect;
		result.x -= pivotPoint.x;
		result.y -= pivotPoint.y;
		result.xMin *= scale;
		result.xMax *= scale;
		result.yMin *= scale;
		result.yMax *= scale;
		result.x += pivotPoint.x;
		result.y += pivotPoint.y;
		return result;
	}

	public static Rect ScaleSizeBy(this Rect rect, Vector2 scale)
	{
		return rect.ScaleSizeBy(scale, rect.center);
	}

	public static Rect ScaleSizeBy(this Rect rect, Vector2 scale, Vector2 pivotPoint)
	{
		Rect result = rect;
		result.x -= pivotPoint.x;
		result.y -= pivotPoint.y;
		result.xMin *= scale.x;
		result.xMax *= scale.x;
		result.yMin *= scale.y;
		result.yMax *= scale.y;
		result.x += pivotPoint.x;
		result.y += pivotPoint.y;
		return result;
	}
}

public class EditorZoomArea
{
	private const float kEditorWindowTabHeight = 21.0f;
	private static Matrix4x4 _prevGuiMatrix;

	public static Rect Begin(float zoomScale, Rect screenCoordsArea)
	{
		GUI.EndGroup(); // End the group Unity begins automatically for an EditorWindow to clip out the window tab. This allows us to draw outside of the size of the EditorWindow.

		Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());
		clippedArea.y += kEditorWindowTabHeight;
		GUI.BeginGroup(clippedArea);

		_prevGuiMatrix = GUI.matrix;
		Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
		Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
		GUI.matrix = translation * scale * translation.inverse * GUI.matrix;

		return clippedArea;
	}

	public static void End()
	{
		GUI.matrix = _prevGuiMatrix;
		GUI.EndGroup();
		GUI.BeginGroup(new Rect(0.0f, kEditorWindowTabHeight, Screen.width, Screen.height));
	}
}

public static class ResourceManager
{
    private static string _ResourcePath = "";

    public static void SetDefaultResourcePath(string defaultResourcePath)
    {
        _ResourcePath = defaultResourcePath;
    }

    public static string PreparePath(string path)
    {
        path = path.Replace(Application.dataPath, "Assets");
        if (Application.isPlaying)
        { // At runtime
            if (path.Contains("Resources"))
                path = path.Substring(path.LastIndexOf("Resources") + 10);
            path = path.Substring(0, path.LastIndexOf('.'));
            return path;
        }
        // In the editor
        if (!path.StartsWith("Assets/"))
            path = _ResourcePath + path;
        return path;
    }

    public static T[] LoadResources<T>(string path) where T : UnityEngine.Object
    {
        path = PreparePath(path);
        if (Application.isPlaying) // At runtime
            return UnityEngine.Resources.LoadAll<T>(path);
#if UNITY_EDITOR
        // In the editor
        return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).Cast<T>().ToArray();
#else
			return null;
#endif
    }

    public static T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        path = PreparePath(path);
        if (Application.isPlaying) // At runtime
            return UnityEngine.Resources.Load<T>(path);
#if UNITY_EDITOR
        // In the editor
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
			return null;
#endif
    }
}

public class uLine
{
	public Vector3 start;
	public Vector3 end;
}