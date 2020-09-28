using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class SVNToolWindow : EditorWindow
{
    #region 用户ID

    private String m_SvnToolUserID = null; // 当前拿到的用户ID
    private const String SVN_TOOL_USER_ID_PREF_NAME = "svn_tool_user_id";
    private String m_InputSvnToolUserID = "";

    #endregion

    #region 存储配置相关

    private String filePath;
    private String m_SVNToolSourcePath = "/Editor/Source/SVNTool/";
    private Boolean m_FinishReadData = false;
    private SVNToolPrefabWrap _prefabWrap = new SVNToolPrefabWrap(5);
    private List<SVNToolPrefab> storedPrefabs = new List<SVNToolPrefab>();

    #endregion

    #region 选择状态相关

    private SVNToolPrefab m_SelectedPrefab = null;

    #endregion

    [MenuItem("SVN/SVN同步工具")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SVNToolWindow));
    }

    public void OnEnable()
    {
        // 读取当前设置的工号
        m_SvnToolUserID = PlayerPrefs.GetString(SVN_TOOL_USER_ID_PREF_NAME);
        
        minSize = new Vector2(600, 600);
    }

    /// <summary>
    /// Unity 绘制
    /// </summary>
    void OnGUI()
    {
        // 存在工号
        if (!String.IsNullOrEmpty(m_SvnToolUserID))
        {
            GUISVNToolOperation();
        }
        else
        {
            GUINeedPlayerInputID();
        }
    }

    #region 操作界面

    Rect prefabWindow = new Rect(20, 20, 120, 50);

    private void GUISVNToolOperation()
    {
        filePath = String.Concat(Application.dataPath, m_SVNToolSourcePath, m_SvnToolUserID, ".json");

        // 判断是否读取存储的配置数据
        if (!m_FinishReadData)
        {
            ReadSVNToolPrefabsFromJson(filePath);
            m_FinishReadData = true;
        }

        if (null == storedPrefabs)
        {
            storedPrefabs = new List<SVNToolPrefab>();
        }

        SetSVNToolPrefabGUI();

    }

    /// <summary>
    /// 读入序列化的预设
    /// </summary>
    private void ReadSVNToolPrefabsFromJson(String filePath)
    {
        // 反序列化
        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath); //读取所有数据送到json格式的字符串里面。
            Debug.Log(dataAsJson);
            _prefabWrap = JsonUtility.FromJson<SVNToolPrefabWrap>(dataAsJson);
            storedPrefabs = _prefabWrap.prefabs;
        }
    }

    /// <summary>
    /// 显示预设内容
    /// </summary>
    Vector2 leftScrollPos = Vector2.zero;
    Vector2 rightScrollPos = Vector2.zero;
    private void SetSVNToolPrefabGUI()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(500));
        
        #region 选择预设

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
        
        // 顶部按钮
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("预设选择", GUILayout.Width(100));
        if (GUILayout.Button("新增", GUILayout.Width(50)))
        {
            m_SelectedPrefab = new SVNToolPrefab();
            storedPrefabs.Add(m_SelectedPrefab);
        }

        GUI.enabled = SetIfCurrentActionEnableBySelected();
        if (GUILayout.Button("删除", GUILayout.Width(50)))
        {
            storedPrefabs.Remove(m_SelectedPrefab);
            m_SelectedPrefab = null;
        }
        GUI.enabled = true;
        
        if (GUILayout.Button("保存", GUILayout.Width(50)))
        {
            // TODO 保存前进行一次过滤
            _prefabWrap.prefabs = storedPrefabs;
            File.WriteAllText(filePath, JsonUtility.ToJson(_prefabWrap));
            m_FinishReadData = false;
        }
        EditorGUILayout.EndHorizontal();
        
        // 展示预设内容列表
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUI.skin.box);
        {
            for (int i = 0; i < storedPrefabs.Count; i++)
            {
                SVNToolPrefab currentPrefab = storedPrefabs[i];
                if (m_SelectedPrefab == currentPrefab)
                {
                    SetOnSelectedBackgroundColor();
                }
                
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                {
                    GUI.backgroundColor = Color.white;
                    // 预设名
                    currentPrefab.name = EditorGUILayout.TextField(currentPrefab.name, GUILayout.Width(120));

                    GUILayout.FlexibleSpace();
                    // 当前存在差异的
                    
                    // 操作
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        
                    }
                    if (GUILayout.Button("查看", GUILayout.Width(40)))
                    {
                        m_SelectedPrefab = currentPrefab;
                    }
                }
                
                EditorGUILayout.EndHorizontal();

                SetDefaultSVNToolBackgroundColor();
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        #endregion

        #region 操作具体内容

        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("预设配置");
        GUIStyle titleStyle2 = new GUIStyle();
        titleStyle2.fontSize = 14;
        titleStyle2.normal.textColor = Color.red;
        GUILayout.Label(null == m_SelectedPrefab ? "需要左侧选择目标预设" : "拖拽文件/文件夹到下方即可添加", titleStyle2);
        EditorGUILayout.EndHorizontal();
        
        // 设定ScrollView为一个Control对象
        GUI.SetNextControlName("text:");
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos, GUI.skin.box);
        {
            if (null != m_SelectedPrefab)
            {
                #region 显示新增
                
//                for (int i = 0; i < m_SelectedPrefab.addNewPaths.Count; i++)
//                {
//                    SVNToolObj boj = m_SelectedPrefab.addNewPaths[i];
//                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
//                    {
//                        // 路径名
//                        EditorGUILayout.TextField(boj.name, GUILayout.Width(120));
//                        GUILayout.FlexibleSpace();
//                    }
//                    EditorGUILayout.EndHorizontal();
//                }

                #endregion
                
                #region 显示文件夹

                foreach (var folder in m_SelectedPrefab.contentFolderPath)
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    {
                        // 路径名
                        EditorGUILayout.LabelField(folder.path, GUILayout.Width(300));
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                #endregion
                
                #region 显示文件

                foreach (var file in m_SelectedPrefab.contentFilePath)
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    {
                        // 路径名
                        EditorGUILayout.TextField(file.path, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                #endregion
            }
        }
        EditorGUILayout.EndScrollView();

        #region 添加拖拽事件
        
        var rect = GUILayoutUtility.GetLastRect();
        //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内  
        if ((Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            &&null != m_SelectedPrefab
            && rect.Contains(Event.current.mousePosition)
            )
        {
            //改变鼠标的外表  
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (Event.current.type == EventType.DragPerform
                && DragAndDrop.paths != null
                && DragAndDrop.paths.Length > 0
            )
            {
                AddNewSVNToolPath(DragAndDrop.paths);
            }
        }

        #endregion
        
        EditorGUILayout.EndVertical();

        #endregion

        EditorGUILayout.EndHorizontal();
        
        // 结果列表
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 用户输入工号界面

    /// <summary>
    /// 用户输入工号界面
    /// </summary>
    private void GUINeedPlayerInputID()
    {
        // 不存在工号缓存，需要用户输入工号并确认
        GUILayout.Label("请输入工号", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        m_InputSvnToolUserID = EditorGUILayout.TextField("用户工号", m_InputSvnToolUserID);
        // TODO 添加针对工号的解释
        if (GUILayout.Button("确定", GUILayout.Width(70f)))
        {
            if ("" != m_InputSvnToolUserID)
            {
                m_SvnToolUserID = m_InputSvnToolUserID;
                PlayerPrefs.SetString(SVN_TOOL_USER_ID_PREF_NAME, "242");
            }
            else
            {
                ShowNotification(new GUIContent("工号不能为空"));
            }
        }

        GUILayout.EndHorizontal();
    }

    #endregion
    
    // 设定回归默认颜色
    private void SetDefaultSVNToolBackgroundColor()
    {
//        GUI.backgroundColor = Color.gray * 1.8f;
        GUI.backgroundColor = new Color(222, 222, 222);
    }
    
    // 设定选中颜色
    private void SetOnSelectedBackgroundColor()
    {
        GUI.backgroundColor = Color.gray * 1.8f;
    }
    
    // 设定是否可操作
    private Boolean SetIfCurrentActionEnableBySelected()
    {
        return null != m_SelectedPrefab;
    }
    
    // 添加路径到当前选择中
    private Boolean AddNewSVNToolPath(String[] paths)
    {
        if (null == m_SelectedPrefab)
        {
            return false;
        }
        
        for (int i = 0; i < paths.Length; i++)
        {
            if (!DragAndDrop.paths[i].StartsWith("Assets"))
            {
                ShowNotification(new GUIContent("选择的文件不在项目路径下"));
                return false;
            }
        }
        
        for (int i = 0; i < paths.Length; i++)
        {
            m_SelectedPrefab.AddNewSVNToolPath(paths[i]);
        }
        
        return true;
    }
}