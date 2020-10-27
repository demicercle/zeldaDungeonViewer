using UnityEngine;
using System.Collections;
using B83.Win32;
using System.Collections.Generic;

public class DungeonViewer : MonoBehaviour
{
    const float LABEL_WIDTH = 80f;

    public Camera mainCamera;
    public RectTransform guiRect;
    public GameObject help;
    public SpriteRenderer mapRenderer;
    public SpriteRenderer cameraFrame;
    public Vector2 roomSize = new Vector2(10, 8);
    public Vector3 cameraOffset = new Vector3(0, 0, -10);
    public float pixelPerUnit = 16f;
    public float pixelSize = 1f;
    public float moveSpeed = 1f;
    public string mapName = "dungeon.png";

    private Vector3 _cameraPosition;
    private bool _isMoving;
    private float _startZoom;
    private float _maxZoom;
    private float _zoomCoef;

    public Rect GetScreenCoordinatesOfCorners(RectTransform uiElement)
    {
        var worldCorners = new Vector3[4];
        uiElement.GetWorldCorners(worldCorners);
        var result = new Rect(
                      worldCorners[0].x,
                      worldCorners[0].y,
                      worldCorners[2].x - worldCorners[0].x,
                      worldCorners[2].y - worldCorners[0].y);
        return result;
    }

    public Vector2 GetPixelPositionOfRect(RectTransform uiElement)
    {
        Rect screenRect = GetScreenCoordinatesOfCorners(uiElement);

        return new Vector2(screenRect.center.x, screenRect.center.y);
    }

    public void LoadMap()
    {
        var spritePath = (Application.isEditor ? Application.streamingAssetsPath : Application.dataPath) + "/" + mapName;
        LoadMap(spritePath);
    }

    public void LoadMap(string spritePath)
    {
        var fileExists = System.IO.File.Exists(spritePath);
        if (!fileExists)
            return;

        var spriteBytes = System.IO.File.ReadAllBytes(spritePath);
        var spriteTex = new Texture2D(0, 0);
        spriteTex.LoadImage(spriteBytes);

        mapRenderer.sprite = Sprite.Create(spriteTex, new Rect(0, 0, spriteTex.width, spriteTex.height), Vector2.zero, 16f);
    }

    private void Start()
    {
        if( mainCamera == null )
            mainCamera = Camera.main;

        _startZoom = mainCamera.orthographicSize;
        _maxZoom = _startZoom * 10;

        LoadCamera();
        LoadMap();
    }

    private void OnGUI()
    {
        if (guiRect == null || !guiRect.gameObject.activeSelf)
            return;

        Rect r = GetScreenCoordinatesOfCorners(guiRect);

        GUILayout.BeginArea(r, GUIContent.none, new GUIStyle("window"));

        GUITextField("MAP NAME", ref mapName);

        GUINumber("CELL SIZE", ref pixelPerUnit);

        GUINumber("ROOM WIDTH", ref roomSize.x);
        GUINumber("ROOM HEIGHT", ref roomSize.y);

        GUILayout.FlexibleSpace();

        if( GUILayout.Button("RELOAD", GUILayout.ExpandWidth(false)) )
        {
            LoadMap();
        }

        GUILayout.EndArea();
    }

    private void GUITextField(string label, ref string text)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(LABEL_WIDTH));
        text = GUILayout.TextField(text);
        GUILayout.EndHorizontal();
    }

    private void GUINumber(string label, ref float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(LABEL_WIDTH));
        string valueStr = GUILayout.TextField(((int)(value)).ToString());
        int i;
        if (int.TryParse(valueStr, out i)) value = i;
        GUILayout.EndHorizontal();
    }

    private void Update()
    {
        if( !_isMoving )
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {

                if (Input.GetKey(KeyCode.UpArrow))
                    _cameraPosition.y += pixelSize;
                if (Input.GetKey(KeyCode.DownArrow))
                    _cameraPosition.y -= pixelSize;
                if (Input.GetKey(KeyCode.RightArrow))
                    _cameraPosition.x += pixelSize;
                if (Input.GetKey(KeyCode.LeftArrow))
                    _cameraPosition.x -= pixelSize;

            }
            else
            {
                if (Input.GetKey(KeyCode.UpArrow))
                    _cameraPosition.y += roomSize.y;
                if (Input.GetKey(KeyCode.DownArrow))
                    _cameraPosition.y -= roomSize.y;
                if (Input.GetKey(KeyCode.RightArrow))
                    _cameraPosition.x += roomSize.x;
                if (Input.GetKey(KeyCode.LeftArrow))
                    _cameraPosition.x -= roomSize.x;

                if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
                    _zoomCoef -= .1f;
                if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                    _zoomCoef += .1f;

                _zoomCoef = Mathf.Clamp01(_zoomCoef);
            }
        }

        mainCamera.orthographicSize = Mathf.Lerp(_startZoom, _maxZoom, _zoomCoef);
        mainCamera.transform.position = Vector3.MoveTowards(mainCamera.transform.position, _cameraPosition + cameraOffset, Time.deltaTime * moveSpeed * pixelPerUnit);
        _isMoving = Vector3.Distance(mainCamera.transform.position, _cameraPosition + cameraOffset) > .01f;

        if( _isMoving )
        {
            SaveCamera();
        }

        cameraFrame.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, mapRenderer.transform.position.z);

        if( help != null)
        {
            if (Input.GetKeyDown(KeyCode.F1))
                help.SetActive(!help.activeSelf);
        }
    }

    private void SaveCamera()
    {
        PlayerPrefs.SetFloat("_cameraPosition.x", _cameraPosition.x);
        PlayerPrefs.SetFloat("_cameraPosition.y", _cameraPosition.y);
    }

    private void LoadCamera()
    {
        _cameraPosition.x = PlayerPrefs.GetFloat("_cameraPosition.x", _cameraPosition.x);
        _cameraPosition.y = PlayerPrefs.GetFloat("_cameraPosition.y", _cameraPosition.y);

        mainCamera.transform.position = _cameraPosition + cameraOffset;
    }

    private void OnEnable()
    {
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;

    }
    private void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
    }

    void OnFiles(List<string> aFiles, POINT aPos)
    {
        string file = "";
        // scan through dropped files and filter out supported image types
        foreach (var f in aFiles)
        {
            var fi = new System.IO.FileInfo(f);
            var ext = fi.Extension.ToLower();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                file = f;
                break;
            }
        }
        // If the user dropped a supported file, create a DropInfo
        if (file != "")
        {
            LoadMap(file);
        }
    }
}
