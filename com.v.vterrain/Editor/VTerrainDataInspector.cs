using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace V.VTerrain
{

    [CustomEditor(typeof(VTerrainData))]
    public class VTerrainDataInspector : Editor
    {
        public enum Mode
        {
            View,
            Paint
        }

        private Mode _selectedMode;
        private Mode _currentMode;
        private Vector3 hitPosition;

        VTerrainData data;
        int brushSize;

        private void OnEnable()
        {
            data = target as VTerrainData;
            //CreateBrush();
            CreatePatches();
            OnEnable_Brush();
            OnEnable_DensityMap();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawDensityMap();
            DrawSelectedBrushMask();   
        }

        private void OnSceneGUI()
        {
            Update();
            DrawModeGUI();
            ModeHandler();
            EventHandler();
            SceneView.RepaintAll();
        }


        void Update()
        {
            //Debug.Log(Camera.current);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (data.brushMesh)
            {
                DestroyImmediate(data.brushMesh);
            }
            if (data.brushObject)
            {
                DestroyImmediate(data.brushObject);
            }
            if (data.brushMaterial)
            {
                DestroyImmediate(data.brushMaterial);
            }
            if (data.patchMesh)
            {
                DestroyImmediate(data.patchMesh);
            }
            if (data.patchMaterial)
            {
                DestroyImmediate(data.patchMaterial);
            }
            if (data.patchObjects != null)
            {
                for (int i = 0; i < data.patchObjects.Count; i++)
                {
                    GameObject obj = data.patchObjects[i];
                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }
                data.patchObjects.Clear();
                data.patchObjects = null;
            }

            SaveDensityMap();
        }

        private void DrawModeGUI()
        {
            List<Mode> modes = GetListFromEnum<Mode>();
            List<string> modeLabels = new List<string>();
            foreach (Mode mode in modes)
            {
                modeLabels.Add(mode.ToString());
            }

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10f, 10f, 360, 40f));

            _selectedMode = (Mode)GUILayout.Toolbar((int)_currentMode, modeLabels.ToArray(), GUILayout.ExpandHeight(true));

            GUILayout.EndArea();
            Handles.EndGUI();
        }
        private void ModeHandler()
        {
            switch (_currentMode)
            {
                case Mode.Paint:
                    if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                    {
                        if (Event.current.button == 0)
                        {
                            Paint(hitPosition);               
                        }
                    }
                    break;
                case Mode.View:

                default:
                    break;

            }

            if (_selectedMode != _currentMode)
            {
                _currentMode = _selectedMode;
            }
        }
        private void EventHandler()
        {
            if(_currentMode == Mode.View){ return;}

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Vector3 mousePosition = Event.current.mousePosition;
            Camera camera = SceneView.currentDrawingSceneView.camera;
            mousePosition.y = camera.pixelHeight - mousePosition.y;

            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                hitPosition = hit.point;
                data.centerPos = hitPosition;
                Vector3 position = hitPosition;
                position.y += 0.5f;

                Handles.DrawWireDisc(position, hit.normal, data.BrushSize);

            }
        }

        private void Paint(Vector3 position)
        {
            //Debug.Log("Hit The Position: .........." + position);
            //data.UpdateDensityRT();
            data.HitPosition(position);
        }

        private void Erase(Vector3 position)
        {


        }

        private void Edit(Vector3 position)
        {

        }

        //private void CreateBrush()
        //{
        //    brushSize = data.BrushSize;

        //    if (data.brushMesh != null) { DestroyImmediate(data.brushMesh); }

        //    data.brushMesh = CreateGridMesh(data.BrushSize);

        //    if (data.brushObject != null) { DestroyImmediate(data.brushObject); }

        //    data.brushObject = new GameObject("Brush");


        //    MeshFilter mf = data.brushObject.AddComponent<MeshFilter>();
        //    MeshRenderer mr = data.brushObject.AddComponent<MeshRenderer>();

        //    Shader shader = Shader.Find("Lightweight Render Pipeline/Lit");
        //    if (shader)
        //    {
        //        data.brushMaterial = new Material(shader);
        //    }

        //    mf.sharedMesh = data.brushMesh;
        //    mr.sharedMaterial = data.brushMaterial;
        //}

        private void CreatePatches()
        {
            if (data.patchObjects != null)
            {
                if (data.patchObjects.Count > 0)
                {
                    for (int i = 0; i < data.patchObjects.Count; i++)
                    {
                        GameObject obj = data.patchObjects[i];
                        if (obj != null)
                        {
                            DestroyImmediate(obj);
                        }
                    }
                    data.patchObjects.Clear();
                    data.patchObjects = null;
                }
            }

            data.patchObjects = new List<GameObject>();

            int patchX = (int)data.Terrain.terrainData.size.x / (int)data.patchSize;
            int patchZ = (int)data.Terrain.terrainData.size.z / (int)data.patchSize;

            data.patchMesh = CreateGridMesh((int)data.patchSize + 1);
            data.patchMaterial = new Material(Shader.Find("Hidden/V/Editor/Terrain"));
            data.patchMaterial.SetFloat("_HeightMultiplier", data.Terrain.terrainData.size.y);
            data.patchMaterial.SetTexture("_HeightMap", data.Terrain.terrainData.heightmapTexture);
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

            for (int x = 0; x < patchX; x++)
            {
                for (int z = 0; z < patchZ; z++)
                {
                    GameObject obj = new GameObject();
                    MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                    MeshFilter mf = obj.AddComponent<MeshFilter>();

                    mf.sharedMesh = data.patchMesh;
                    mr.sharedMaterial = data.patchMaterial;
                    mr.receiveShadows = false;

                    Bounds bs = mf.sharedMesh.bounds;
                    bs.size = new Vector3(bs.size.x, 5000.0f, bs.size.z);
                    mf.sharedMesh.bounds = bs;


                    materialPropertyBlock.SetVector("_UVOffset", new Vector4(1.0f / patchX, 1.0f / patchZ, (float)x / patchX, (float)z / patchZ));
                    mr.SetPropertyBlock(materialPropertyBlock);

                    float offsetX = (float)data.patchSize * (0.5f + x);
                    float offsetZ = (float)data.patchSize * (0.5f + z);
                    obj.transform.localPosition = new Vector3(offsetX, 0.0f, offsetZ);
                    obj.transform.SetParent(data.transform);
                    data.patchObjects.Add(obj);
                }
            }


        }

        public static Mesh CreateGridMesh(int numberOfRows)
        {

            float botLeftX = (numberOfRows - 1) / -2f;
            float botLeftZ = (numberOfRows - 1) / -2f;

            Mesh meshyMcMeshFace = new Mesh();
            Vector3[] verts = new Vector3[numberOfRows * numberOfRows];
            Vector2[] uvs = new Vector2[numberOfRows * numberOfRows];

            int numSuqares = numberOfRows - 1;
            int[] tris = new int[numSuqares * numSuqares * 2 * 3];//trianges

            int i = 0;
            int t = 0;

            for (float x = 0; x < numberOfRows; ++x)
            {
                for (float z = 0; z < numberOfRows; ++z)
                {                                         // unity plane default vertices is 11 vertices. 10 squre
                    verts[i].x = botLeftX + x;
                    verts[i].y = 0;
                    verts[i].z = botLeftZ + z; //Based on the percentgy

                    uvs[i].x = (float)x / (numberOfRows - 1);
                    uvs[i].y = (float)z / (numberOfRows - 1);


                    if (x == numberOfRows - 1 || z == numberOfRows - 1)
                    {
                        ++i;
                        continue;
                    }

                    tris[t] = i;
                    tris[t + 1] = i + 1;
                    tris[t + 2] = i + numberOfRows + 1;

                    tris[t + 3] = i;
                    tris[t + 4] = i + numberOfRows + 1;
                    tris[t + 5] = i + numberOfRows;

                    t += 6;
                    ++i;
                }
            }
            meshyMcMeshFace.name = "The Generated M.D.";
            meshyMcMeshFace.vertices = verts;
            meshyMcMeshFace.uv = uvs;
            meshyMcMeshFace.triangles = tris;
            meshyMcMeshFace.RecalculateBounds();
            meshyMcMeshFace.RecalculateNormals();
            return meshyMcMeshFace;
        }


        #region Brush   
        //================================================

        private EObject m_itemSelected;
        private Texture2D m_itemPreview;

        void OnEnable_Brush()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            GrassAssetWindow.ItemSelectedAction -= UpdateCurrentBrush;
            GrassAssetWindow.ItemSelectedAction += UpdateCurrentBrush;
        }

        private void UnsubscribeEvents()
        {
            GrassAssetWindow.ItemSelectedAction -= UpdateCurrentBrush;
        }

        private void UpdateCurrentBrush(EObject item,Texture2D preview)
        {
            m_itemSelected = item;
            m_itemPreview = preview;
            data.brushMask = preview;
            Repaint();
        }

        private void DrawSelectedBrushMask()
        {
            if (data.brushMask == null)
            {
                EditorGUILayout.HelpBox("没有笔刷!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginVertical("box");
                if (GUILayout.Button(new GUIContent(data.brushMask),GUILayout.Width(64),GUILayout.Height(64), GUILayout.ExpandWidth(true)))
                {
                    GrassAssetWindow.ShowGrassAssetWindow();
                }   
                GUILayout.Height(10);
                EditorGUILayout.LabelField(data.brushMask.name);
                EditorGUILayout.EndVertical();
            }

        }

        //================================================

        #endregion

        #region DensityMap

        string destinationPath=null;

        private void OnEnable_DensityMap()
        {
            data.CheckBrushResources();

            if (data.m_FinalSource != null)
            {
                destinationPath = AssetDatabase.GetAssetPath(data.m_FinalSource);
            }
        }

        private void DrawDensityMap()
        {
            if (data.m_FinalSource == null && (destinationPath == null))
            {
                EditorGUILayout.BeginVertical("box");
                Color c = GUI.color;
                GUI.color = new Color(1.0f, 0.3f, 0.3f, 1.0f);
                if (GUILayout.Button(new GUIContent("没有DensityMap,点击创建")))
                {
                    string fullPath = EditorUtility.SaveFilePanel("创建DensityMap", WorkSpace(), data.gameObject.name, "png");

                    fullPath = UnitySlash(fullPath);
                    if (fullPath != null)
                    {
                        if (fullPath.Contains(WorkSpace()))
                        {
                            
                            destinationPath = fullPath;
                            data.CheckBrushResources();

                        }
                        else
                        {
                            destinationPath = null;
                        }
                    }
                }
                GUI.color = c;
                GUILayout.Height(60);
                EditorGUILayout.EndVertical();
            }
            else
            {
                if (data.m_LastDestination)
                {
                    data.CheckBrushResources();
                }
                EditorGUILayout.BeginVertical("box");
                if (GUILayout.Button(new GUIContent(data.m_LastDestination), GUILayout.Width(256), GUILayout.Height(256), GUILayout.ExpandWidth(true)))
                {
                    SaveDensityMap();
                }
                GUILayout.Height(10);
                EditorGUILayout.LabelField(data.m_LastDestination.name);
                EditorGUILayout.EndVertical();
            }
        }

        private void SaveDensityMap()
        {
            if (destinationPath != null)
            {
                Texture2D texture = CreateTexture_Linear(data.m_LastDestination);
                Texture2D loadTexture = WriteTextureToDisk(texture, destinationPath);
                AssetDatabase.Refresh();
                data.m_FinalSource = loadTexture;
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion


        public static List<T> GetListFromEnum<T>()
        {
            List<T> enumList = new List<T>();
            System.Array enums = System.Enum.GetValues(typeof(T));
            foreach (T e in enums)
            {
                enumList.Add(e);
            }
            return enumList;
        }

        public static string WorkSpace()
        {
            return System.IO.Path.GetFullPath("Assets").Replace('\\', '/');
        }

        public static string ToUnity(string path)
        {
            path = path.Replace('\\', '/');
            string workSpace = System.IO.Path.GetFullPath("Assets").Replace('\\', '/');
            if (path.Contains(workSpace))
                return "Assets" + path.Substring(workSpace.Length);
            else
                return null;
        }

        public static string UnitySlash(string path)
        {
            if (path == null) { return ""; }
            return path.Replace("\\", "/");
        }

        public static Texture2D CreateTexture_Linear(RenderTexture renderTextrue)
        {
            if (renderTextrue == null) { return null; }

            TextureFormat format = TextureFormat.RGBA32;
            Texture2D texture = new Texture2D(renderTextrue.width, renderTextrue.height, format, false, true);
            RenderTexture.active = renderTextrue;
            texture.ReadPixels(new Rect(0, 0, renderTextrue.width, renderTextrue.height), 0, 0);
            texture.Apply();
            return texture;
        }

        public static Texture2D WriteTextureToDisk(Texture2D textureCache, string targetPath)
        {
            if (targetPath == null) { return null; }
            if (textureCache == null) { return null; }
            System.IO.File.WriteAllBytes(targetPath, ImageConversion.EncodeToPNG(textureCache));
            AssetDatabase.ImportAsset(targetPath);
            Object.DestroyImmediate(textureCache);

            //TextureImporter textureImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;
            //PlatFormTextureSettings_RGBA(textureImporter);

            return AssetDatabase.LoadMainAssetAtPath(targetPath) as Texture2D;
        }
    }
}
