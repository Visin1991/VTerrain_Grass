using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace V.VTerrain
{
    public class GrassAssetWindow : EditorWindow
    {
        private List<ECategory> m_categories;
        private List<string> m_categoryLabels;
        private List<EObject> _items;
        private Dictionary<ECategory, List<EObject>> _categorizedItems;
        private Dictionary<EObject, Texture2D> _previews;
        private ECategory _categorySelected;

        private Vector2 _scrollPosition;
        private const float ButtonWidth = 80;
        private const float ButtonHeight = 90;

        public static GrassAssetWindow instance;

        public static void ShowGrassAssetWindow()
        {
            instance = (GrassAssetWindow)EditorWindow.GetWindow(typeof(GrassAssetWindow));
            instance.titleContent = new GUIContent("GrassEditorAsset");
            instance.Show();
        }

        private void OnEnable()
        {
            InitCategories();
            InitContent();
            GeneratePreviews();
        }

        private void OnGUI()
        {
            DrawTabs();
            DrawScroll();
        }

        private void Update()
        {
            //if (_previews.Count != _items.Count)
            //{
            //    GeneratePreviews();
            //}
        }


        private void InitCategories()
        {
            m_categories = GetListFromEnum<ECategory>();
            m_categoryLabels = new List<string>();
            foreach (ECategory category in m_categories)
            {
                m_categoryLabels.Add(category.ToString());
            }
        }

        private void DrawTabs()
        {
            int index = (int)_categorySelected;
            index = GUILayout.Toolbar(index, m_categoryLabels.ToArray());
            _categorySelected = m_categories[index];
        }

        private void InitContent()
        {
            _items = new List<EObject>();
            _categorizedItems = new Dictionary<ECategory, List<EObject>>();
            _previews = new Dictionary<EObject, Texture2D>();

            LoadBuiltInResources();
            LoadExternalResrouces();

            foreach (ECategory category in m_categories)
            {
                _categorizedItems.Add(category, new List<EObject>());
            }

            // Assign items to each category 
            foreach (EObject item in _items)
            {
                _categorizedItems[item.category].Add(item);
            }
        }

        private void DrawScroll()
        {
            if (_categorizedItems[_categorySelected].Count == 0)
            {
                EditorGUILayout.HelpBox("This category is empty!", MessageType.Info);
                return;
            }

            int rowCapacity = Mathf.FloorToInt(position.width / (ButtonWidth));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            int selectionGridIndex = -1;
            selectionGridIndex = GUILayout.SelectionGrid(
            selectionGridIndex,
            GetGUIContentsFromItems(),
            rowCapacity,
            GetGUIStyle());
            GetSelectedItem(selectionGridIndex);
            GUILayout.EndScrollView();
        }

        public static System.Action<EObject, Texture2D> ItemSelectedAction;
        private void GetSelectedItem(int index)
        {
            if (index != -1)
            {
                EObject selectedItem = _categorizedItems[_categorySelected][index];
                if (ItemSelectedAction != null)
                {
                    ItemSelectedAction(selectedItem, _previews[selectedItem]);
                }
            }
        }
        private void GeneratePreviews()
        {
            foreach (EObject item in _items)
            {
                if (!_previews.ContainsKey(item))
                {
                    Texture2D preview = item.GetPreview();
                    if (preview != null)
                    {
                        _previews.Add(item, preview);
                    }
                }
            }
        }

        private GUIContent[] GetGUIContentsFromItems()
        {
            List<GUIContent> guiContents = new List<GUIContent>();

            //Debug.Log("_previews Count : " + _previews.Count);
            //Debug.Log("_items Count : " + _items.Count);

            if (_previews.Count == _items.Count)
            {
                int totalItems = _categorizedItems[_categorySelected].Count;
                for (int i = 0; i < totalItems; i++)
                {
                    GUIContent guiContent = new GUIContent();
                    guiContent.text = _categorizedItems[_categorySelected][i].name;
                    guiContent.image = _previews[_categorizedItems[_categorySelected][i]];
                    guiContents.Add(guiContent);
                }
            }
            return guiContents.ToArray();
        }


        private GUIStyle GetGUIStyle()
        {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.button);
            guiStyle.alignment = TextAnchor.LowerCenter;
            guiStyle.imagePosition = ImagePosition.ImageAbove;
            guiStyle.fixedWidth = ButtonWidth;
            guiStyle.fixedHeight = ButtonHeight;
            return guiStyle;
        }

        private static List<T> GetListFromEnum<T>()
        {
            List<T> enumList = new List<T>();
            System.Array enums = System.Enum.GetValues(typeof(T));
            foreach (T e in enums)
            {
                enumList.Add(e);
            }
            return enumList;
        }

        private static List<T> GetAssetWithScript<T>(string path) where T : MonoBehaviour
        {
            T tmp;
            string assetPath;
            GameObject asset;
            List<T> assetList = new List<T>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
            for (int i = 0; i < guids.Length; i++)
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                asset = AssetDatabase.LoadAssetAtPath(assetPath,typeof(GameObject)) as GameObject;
                tmp = asset.GetComponent<T>();
                if (tmp != null)
                {
                    assetList.Add(tmp);
                }
            }
            return assetList;
        }

        private void LoadBuiltInResources()
        {
            string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { "Packages/com.v.vterrain/Editor/Textures" });
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Texture2D texture = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D)) as Texture2D;
                if (texture != null)
                {
                    EObject eObject = new EObject();
                    eObject.name = texture.name;
                    eObject.texture2D = texture;
                    eObject.category = ECategory.Brush;
                    _items.Add(eObject);
                }
            }
        }

        private void LoadExternalResrouces()
        {

        }
    }

    public enum ECategory
    {
        Brush,
        GrassType
    }

    public class EObject
    {
        public string name;
        public ECategory category;
        public Texture2D texture2D;
        public GameObject gameObject;
        public Texture2D GetPreview()
        {
            if (gameObject)
            {
                AssetPreview.GetAssetPreview(gameObject);
            }
            if (texture2D)
            {
                return texture2D;
            }
            return null;
        }

        public override string ToString()
        {
            return name;
        }

    }
}
