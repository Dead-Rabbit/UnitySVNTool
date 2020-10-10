using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    private String testClipStr = "C:/Users/Jack/Documents/MGame/MGame_Trunk/MProject/";    // Application.dataPath
    
    List<SVNToolFile> showResult = new List<SVNToolFile>();
    List<SVNToolFile> hideResult = new List<SVNToolFile>();

    private Boolean ifChooseDataDirty = true;
    private String showChooseResult = "";
    private String hideChooseResult = "";

    #endregion

    #region 选择状态相关

    private EnumSVNToolWindowEditState m_CurrentEditState = EnumSVNToolWindowEditState.VIEW;
    private SVNToolPrefab m_SelectedPrefab = null;
    private Boolean m_ShowFolding = false;

    #endregion

    #region 展示配置

    private Single m_RightPathTextLength = 300;
    
    #endregion

    [MenuItem("SVN/SVN同步工具 #&M")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SVNToolWindow));
    }

    public void OnEnable()
    {
        // 读取当前设置的工号
        m_SvnToolUserID = PlayerPrefs.GetString(SVN_TOOL_USER_ID_PREF_NAME);
        
        minSize = new Vector2(850, 600);
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
        // 判断是否读取存储的配置数据
        if (!m_FinishReadData)
        {
            filePath = String.Concat(Application.dataPath, m_SVNToolSourcePath, m_SvnToolUserID, ".json");
            ReadSVNToolPrefabsFromJson(filePath);
            m_CurrentEditState = EnumSVNToolWindowEditState.VIEW;
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
            string dataAsJson = File.ReadAllText(filePath);     //读取所有数据送到json格式的字符串里面。
            _prefabWrap = JsonUtility.FromJson<SVNToolPrefabWrap>(dataAsJson);
            storedPrefabs = _prefabWrap.prefabs;
            
            // 初始同步内容
//            List<SVNToolPrefab> initPrefabs = new List<SVNToolPrefab>();
//            foreach (SVNToolPrefab prefab in storedPrefabs)
//            {
//                // Prefab 初始化
//                prefab.InitSVNToolPrefabFileAndFolderInfo();
//                // 同步Prefab的内容
//                if (prefab.ifSelected)
//                {
//                    initPrefabs.Add(prefab);
//                }
//            }
            InitSyncSVNToolPrefabStatus();
            
            // 设定默认选择
            if (null == m_SelectedPrefab && storedPrefabs.Count > 0)
            {
                SelectCurrentSVNToolPrefab(storedPrefabs[0]);
            }
        }
    }

    /// <summary>
    /// 显示预设内容
    /// </summary>
    /// TODO 加入排序功能
    Vector2 leftScrollPos = Vector2.zero;
    Vector2 rightScrollPos = Vector2.zero;
    private Vector2 showScrollPos = Vector2.zero;
    private Vector2 hideScrollPos = Vector2.zero;
    private void SetSVNToolPrefabGUI()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(500));
        
        #region 选择预设

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
        
        // 顶部按钮
        EditorGUILayout.BeginHorizontal(GUILayout.Width(270));

        EditorGUILayout.LabelField("预设选择", GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        if (m_CurrentEditState == EnumSVNToolWindowEditState.EDIT)
        {
            if (GUILayout.Button("注销", GUILayout.Width(50)))
            {
                PlayerPrefs.DeleteKey(SVN_TOOL_USER_ID_PREF_NAME);
                m_SvnToolUserID = null;
                m_FinishReadData = false;
            }
            
            if (GUILayout.Button("新增", GUILayout.Width(50)))
            {
                Int32 largeID = 1;
                // 拿到最大的ID
                foreach (SVNToolPrefab storedPrefab in storedPrefabs)
                {
                    largeID = Math.Max(largeID, storedPrefab.ID);
                }

                m_SelectedPrefab = new SVNToolPrefab(largeID + 1);
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
                SaveSVNToolPrefabs();
                // 刷新状态
                InitSyncSVNToolPrefabStatus();
            }
        } else if (m_CurrentEditState == EnumSVNToolWindowEditState.VIEW)
        {
            if (GUILayout.Button("开启配置", GUILayout.Width(70)))
            {
                m_CurrentEditState = EnumSVNToolWindowEditState.EDIT;
                if (null != m_SelectedPrefab)
                {
                    foreach (SVNToolFolder folder in m_SelectedPrefab.contentFolderPath)
                    {
                        folder.openFolder = false;
                    }
                }
            }
        }

        EditorGUILayout.EndHorizontal();
        
        // 展示预设内容列表
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUI.skin.box);
        {
            for (int i = 0; i < storedPrefabs.Count; i++)
            {
                SVNToolPrefab currentPrefab = storedPrefabs[i];
                if (null != m_SelectedPrefab && m_SelectedPrefab.ID == currentPrefab.ID)
                {
                    SetOnSelectedBackgroundColor();
                }
                
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                {
                    GUI.backgroundColor = Color.white;

                    Boolean preSelected = currentPrefab.ifSelected;
                    Boolean ifChanged = false;
                    currentPrefab.ifSelected = GUILayout.Toggle(currentPrefab.ifSelected, "同步", GUILayout.Width(40));
                    if (preSelected != currentPrefab.ifSelected)
                    {
                        // 如果选择了不同的同步规则，则保存
                        SaveSVNToolPrefabs();
                        // 刷新已选Data
                        ifChooseDataDirty = true;
                        ifChanged = true;
                    }
                    
                    // 预设名
                    if (m_CurrentEditState == EnumSVNToolWindowEditState.EDIT)
                    {
                        currentPrefab.name = EditorGUILayout.TextField(currentPrefab.name, GUILayout.Width(120));
                    } else if (m_CurrentEditState == EnumSVNToolWindowEditState.VIEW)
                    {
                        EditorGUILayout.LabelField(currentPrefab.name, GUILayout.Width(120));
                    }
                    
                    if (ifChanged)
                    {
                        GUI.FocusControl("zero");
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("查看", GUILayout.Width(40)))
                    {
//                        DoSyncSVNToolPrefabStatus(currentPrefab);
                        SelectCurrentSVNToolPrefab(currentPrefab);
                    }
                }
                
                EditorGUILayout.EndHorizontal();

                SetDefaultSVNToolBackgroundColor();
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        #endregion

        #region 预设与配置主体

        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        EditorGUILayout.BeginHorizontal();
        
        GUIStyle redGUISkin = new GUIStyle();
        redGUISkin.fontSize = 14;
        redGUISkin.normal.textColor = Color.red;
        if (m_CurrentEditState == EnumSVNToolWindowEditState.EDIT)
        {
            EditorGUILayout.LabelField("预设配置");
            GUILayout.Label(null == m_SelectedPrefab ? "需要左侧选择目标预设" : "拖拽文件/文件夹到下方即可添加", redGUISkin);
        } else if (m_CurrentEditState == EnumSVNToolWindowEditState.VIEW)
        {
            EditorGUILayout.LabelField("配置文件");
            GUILayout.FlexibleSpace();
            if (CheckIfSelectedPrefabSyncing())
            {
                GUILayout.Label("同步中...", redGUISkin);
            }
            
            GUI.enabled = !CheckIfSelectedPrefabSyncing();
            if (GUILayout.Button("刷新同步"))
            {
                m_SelectedPrefab.initedFileStatus = false;
                DoSyncSVNToolPrefabStatus(m_SelectedPrefab);
            }

            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();
        
        // 设定ScrollView为一个Control对象
        GUI.SetNextControlName("text:");
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos, GUI.skin.box);
        {
            if (null != m_SelectedPrefab)
            {
                // 当前是否在进行同步
                if (CheckIfSelectedPrefabSyncing())
                {
                    GUILayout.Label("加载中");
                } else {
                    
                    #region 显示文件夹
                    
                    for (int i = 0; i < m_SelectedPrefab.contentFolderPath.Count; i++)
                    {
                        SVNToolFolder folder = m_SelectedPrefab.contentFolderPath[i];
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                // 配置状态
                                if (m_CurrentEditState == EnumSVNToolWindowEditState.EDIT)
                                {
                                    EditorGUILayout.LabelField("【文件夹】\t" + folder.name, GUILayout.Width(m_RightPathTextLength));
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("Del", GUILayout.Width(30)))
                                    {
                                        m_SelectedPrefab.contentFolderPath.Remove(folder);
                                    }
                                }
                                else if (m_CurrentEditState == EnumSVNToolWindowEditState.VIEW) {
                                    
                                    GUI.enabled = folder.contentNeedSyncFiles.Count > 0;
                                    if (folder.existNewFileOrFolder)
                                    {
                                        SetSVNToolFolderSelectedParyColor();
                                        if (GUILayout.Button("存在新建，以及文件夹操作，请点击此处", GUILayout.Width(220)))
                                        {
                                            UESvnOperation.GetSvnOperation().CommitFile(folder.path);
                                            Debug.Log("finish commit");
                                            InitSyncSVNToolPrefabStatus();
                                        }
                                        SetDefaultSVNToolBackgroundColor();
                                    }

                                    // 浏览模式 - 仅展示需同步文件
                                    if (folder.GetSVNToolFileCurrentSyncState() == EnumSVNToolFolderNeedSyncState.SELECTED_ALL)
                                        SetSVNToolFileCanBeCommitColor();
                                    else if (folder.GetSVNToolFileCurrentSyncState() == EnumSVNToolFolderNeedSyncState.SELECTED_PART)
                                        SetSVNToolFolderSelectedParyColor();

                                    Int32 selectedFolderFileCount = folder.GetTotalSelectedSVNToolFiles().Count;
                                    
                                    if (GUILayout.Button(selectedFolderFileCount == 0 ? "全选" : "取消选择", GUILayout.Width(60)))
                                    {
                                        // 全选
                                        if (selectedFolderFileCount == 0)
                                            foreach (SVNToolFile file in folder.contentNeedSyncFiles)
                                                file.ifSelected = true;
                                        else
                                            foreach (SVNToolFile file in folder.contentNeedSyncFiles)
                                                file.ifSelected = false;
                                        
                                        ifChooseDataDirty = true;
                                        GUI.FocusControl("zero");    // 取消聚焦，防止选中“当前已选择”后不刷新
                                    }
                                    GUI.enabled = true;
                                    
                                    EditorGUILayout.LabelField("已选 " + selectedFolderFileCount + "/" + folder.contentNeedSyncFiles.Count, GUILayout.Width(70));
                                    SetDefaultSVNToolBackgroundColor();
                                    folder.openFolder = EditorGUILayout.Foldout(folder.openFolder, folder.name, true);
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            if (folder.openFolder)
                            {
                                EditorGUILayout.BeginVertical();
                                {
                                    //折叠开关开启时需要显示的内容
                                    foreach (SVNToolFile file in folder.contentNeedSyncFiles)
                                    {
                                        EditorGUILayout.BeginHorizontal(GUI.skin.box);
                                        {
                                            SVNToolWindowWriteFileField(file);
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                }
                                EditorGUILayout.EndVertical();
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }

                    #endregion
                    
                    #region 显示文件

                    for (int i = 0; i < m_SelectedPrefab.contentFilePath.Count; i++)
                    {
                        SVNToolFile file = m_SelectedPrefab.contentFilePath[i];
                        if (m_CurrentEditState == EnumSVNToolWindowEditState.VIEW && !file.CanBeCommit)
                            continue;
                        
                        EditorGUILayout.BeginHorizontal(GUI.skin.box);
                        {
                            // 编辑模式
                            if (m_CurrentEditState == EnumSVNToolWindowEditState.EDIT)
                            {
                                EditorGUILayout.LabelField("【 文 件 】\t" + file.name, GUILayout.Width(m_RightPathTextLength));
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Del", GUILayout.Width(30)))
                                {
                                    m_SelectedPrefab.contentFilePath.Remove(file);
                                }
                            }
                            // 浏览模式
                            else if (m_CurrentEditState == EnumSVNToolWindowEditState.VIEW) {
                                SVNToolWindowWriteFileField(file);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    #endregion
                }
            }
        }
        EditorGUILayout.EndScrollView();

        #region 添加拖拽事件
        
        var rect = GUILayoutUtility.GetLastRect();
        //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内  
        if ((Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            && m_CurrentEditState == EnumSVNToolWindowEditState.EDIT
            && null != m_SelectedPrefab
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
                // 保存
//                SaveSVNToolPrefabs();
            }
        }

        #endregion
        
        EditorGUILayout.EndVertical();

        #endregion

        EditorGUILayout.EndHorizontal();

        #region 结果列表

        if (CheckIfSelectedPrefabSyncing())
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("加载中");
            }
            EditorGUILayout.EndVertical();
        } else if (m_CurrentEditState == EnumSVNToolWindowEditState.EDIT)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("编辑中");
            }
            EditorGUILayout.EndVertical();
            
        } else {
            // TODO 刷新值修改为 - 当做了选择后进行的刷新
            if (ifChooseDataDirty)
            {
                RefreshAllSelectedSVNToolFiles();    // 刷新值
            }

            #region 未选择结果列表

            GUILayout.Label("配置文件未选择");

            hideScrollPos = EditorGUILayout.BeginScrollView(hideScrollPos, GUI.skin.box, GUILayout.MaxHeight(100));
            {
                EditorGUILayout.TextArea(hideChooseResult, GUILayout.Height(100));
            }
            EditorGUILayout.EndScrollView();
            
            #endregion

            #region 已选结果列表

            GUILayout.Label("当前已选择");
            
            showScrollPos = EditorGUILayout.BeginScrollView(showScrollPos, GUI.skin.box, GUILayout.MaxHeight(300));
            {
                EditorGUILayout.TextArea(showChooseResult, GUILayout.Height(300));
            }
            EditorGUILayout.EndScrollView();
            
            #endregion
        }

        #endregion

        #region 操作组

        // 控制提交按钮的可控
        GUI.enabled = m_CurrentEditState != EnumSVNToolWindowEditState.EDIT && !CheckIfSelectedPrefabSyncing();
        
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("提交"))
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (SVNToolFile file in showResult)
                {
                    stringBuilder.Append(file.path).Append("*");
                }
                UESvnOperation.GetSvnOperation().CommitFile(stringBuilder.ToString());
            }
        }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        #endregion

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
        if (GUILayout.Button("确定", GUILayout.Width(70f)))
        {
            if ("" != m_InputSvnToolUserID)
            {
                m_SvnToolUserID = m_InputSvnToolUserID;
                PlayerPrefs.SetString(SVN_TOOL_USER_ID_PREF_NAME, m_InputSvnToolUserID);
            }
            else
            {
                ShowNotification(new GUIContent("工号不能为空"));
            }
        }

        GUILayout.EndHorizontal();
    }

    #endregion

    #region 右侧文件写入

    private void SVNToolWindowWriteFileField(SVNToolFile file)
    {
        // 浏览模式 - 仅展示需同步文件
        if (file.GetSVNToolFileCurrentSyncState() == EnumSVNToolFileNeedSyncState.SELECTED_NEED_COMMIT)
            SetSVNToolFileCanBeCommitColor();
        else if (file.GetSVNToolFileCurrentSyncState() == EnumSVNToolFileNeedSyncState.NEED_COMMIT_WITHOUT_SEELCTED)
            SetSVNToolFileNeedSelectedColor();

        Boolean preSelected = file.ifSelected;
        file.ifSelected = GUILayout.Toggle(file.ifSelected, "", GUILayout.Width(20));
        if (file.ifSelected != preSelected)
        {
            ifChooseDataDirty = true;
            OnChangeSelectSVNToolFile(file);
            SaveSVNToolPrefabs();               // 保存配置
            GUI.FocusControl("zero");    // 取消聚焦，防止选中“当前已选择”后不刷新
        }
        
        EditorGUILayout.LabelField(file.name, GUILayout.MinWidth(m_RightPathTextLength));
        GUILayout.FlexibleSpace();
                            
        if (GUILayout.Button("Find", GUILayout.Width(40)))
        {
            UESvnOperation.GetSvnOperation().ShowFileInExplorer(file.path);
        }
        if (GUILayout.Button("完整路径", GUILayout.Width(70)))
        {
            ShowNotification(new GUIContent(file.svnURL));
        }
        if (GUILayout.Button("查看差异", GUILayout.Width(70)))
        {
            UESvnOperation.GetSvnOperation().DiffFile(file.path);
        }

        SetDefaultSVNToolBackgroundColor();
    }

    #endregion

    #region 颜色设定

    // 设定回归默认颜色
    private void SetDefaultSVNToolBackgroundColor()
    {
//        GUI.backgroundColor = Color.gray * 1.8f;
        GUI.backgroundColor = new Color(222, 222, 222);
    }
    
    // 设定预设选中颜色
    private void SetOnSelectedBackgroundColor()
    {
        GUI.backgroundColor = Color.gray * 1.8f;
    }
    
    // 设定文件可同步的颜色 
    private void SetSVNToolFileNeedSelectedColor()
    {
        GUI.backgroundColor = Color.yellow;
    }
    
    // 设定文件夹未全选的颜色 
    private void SetSVNToolFolderSelectedParyColor()
    {
        GUI.backgroundColor = Color.yellow;
    }
    
    // 设定文件需要选择的颜色
    private void SetSVNToolFileCanBeCommitColor()
    {
        GUI.backgroundColor = Color.green;
    }
    
    #endregion
    
    // 设定是否可操作
    private Boolean SetIfCurrentActionEnableBySelected()
    {
        return null != m_SelectedPrefab;
    }
    
    // 获取是否在更新中
    private Boolean CheckIfSelectedPrefabSyncing()
    {
        return SVNToolUtil.Syncing;
    }

    // 添加路径到当前选择中
    private Boolean AddNewSVNToolPath(String[] paths)
    {
        if (null == m_SelectedPrefab)
        {
            return false;
        }
        
        // TODO 检查是否为本项目工程 - 待开启
//        for (int i = 0; i < paths.Length; i++)
//        {
//            if (!DragAndDrop.paths[i].StartsWith("Assets"))
//            {
//                ShowNotification(new GUIContent("选择的文件不在项目路径下"));
//                return false;
//            }
//        }
        
        for (int i = 0; i < paths.Length; i++)
        {
            m_SelectedPrefab.AddNewSVNToolPath(paths[i]);
        }
        
        return true;
    }
    
    // 保存预设
    private void SaveSVNToolPrefabs()
    {
        _prefabWrap.prefabs = storedPrefabs;
        File.WriteAllText(filePath, JsonUtility.ToJson(_prefabWrap));

        if (null != m_SelectedPrefab && m_CurrentEditState != EnumSVNToolWindowEditState.VIEW) {
            foreach (SVNToolFolder folder in m_SelectedPrefab.contentFolderPath)
            {
                folder.openFolder = true;
            }
        }
        m_CurrentEditState = EnumSVNToolWindowEditState.VIEW;
    }
    
    // 设定选中的预设
    private void SelectCurrentSVNToolPrefab(SVNToolPrefab prefab)
    {
        m_SelectedPrefab = prefab;
        // 点击查看后检查是否可更新状态
//        DoSyncSVNToolPrefabStatus(m_SelectedPrefab);
    }
    
    // 更新预设中文件状态
    private void DoSyncSVNToolPrefabStatus(SVNToolPrefab prefab)
    {
        if (null != prefab && !prefab.initedFileStatus)
        {
            SVNToolUtil.GetSVNToolObjStateJobHandle(prefab);
            ifChooseDataDirty = true;
        }
    }

    private void InitSyncSVNToolPrefabStatus()
    {
        List<SVNToolPrefab> initPrefabs = new List<SVNToolPrefab>();
        foreach (SVNToolPrefab prefab in storedPrefabs)
        {
            // Prefab 初始化
            prefab.InitSVNToolPrefabFileAndFolderInfo();
            // 同步Prefab的内容
            if (prefab.ifSelected)
            {
                initPrefabs.Add(prefab);
            }
        }

        if (initPrefabs.Count <= 0) return;
        
        SVNToolUtil.GetSVNToolObjStateJobHandle(initPrefabs);
        ifChooseDataDirty = true;
    }

    /// <summary>
    /// 更改文件勾选状态
    /// </summary>
    private void OnChangeSelectSVNToolFile(SVNToolFile targetFile)
    {
        String targetPath = targetFile.path;
        Boolean targetSelect = targetFile.ifSelected;
        
        foreach (SVNToolPrefab prefab in storedPrefabs)
        {
            if (!prefab.ifSelected)
                continue;

            foreach (SVNToolFile file in prefab.contentFilePath)
            {
                if (file != targetFile && file.path.Equals(targetPath))
                {
                    file.ifSelected = targetSelect;
                }
            }

            foreach (SVNToolFolder folder in prefab.contentFolderPath)
            {
                foreach (SVNToolFile file in folder.contentNeedSyncFiles)
                {
                    if (file != targetFile && file.path.Equals(targetPath))
                    {
                        file.ifSelected = targetSelect;
                    }
                }
            }
        }
    }

    // 获取所有当前选择的文件路径
    private void RefreshAllSelectedSVNToolFiles()
    {
        ifChooseDataDirty = false;
        
        showResult.Clear();
        hideResult.Clear();
        foreach (SVNToolPrefab prefab in storedPrefabs)
        {
            if (!prefab.ifSelected)
                continue;

            // 文件夹下所有已选文件
            foreach (SVNToolFolder folder in prefab.contentFolderPath)
            {
                foreach (SVNToolFile file in folder.contentNeedSyncFiles)
                {
                    if (file.ifSelected)
                    {
                        AddSVNToolFileIntoShowResult(file);
                    }
                    else
                    {
                        AddSVNToolFileIntoHideResult(file);
                    }
                }
            }

            foreach (SVNToolFile file in prefab.contentFilePath)
            {
                if (!file.CanBeCommit)
                    continue;
                
                if (file.ifSelected)
                {
                    AddSVNToolFileIntoShowResult(file);
                }
                else
                {
                    AddSVNToolFileIntoHideResult(file);
                }
            }
        }
        
        StringBuilder showResultStringBuilder = new StringBuilder();
        foreach (SVNToolFile file in showResult)
        {
            showResultStringBuilder.AppendLine(file.svnURL);
        }
        showChooseResult = showResultStringBuilder.ToString();
        
        StringBuilder hideResultStringBuilder = new StringBuilder();
        foreach (SVNToolFile file in hideResult)
        {
            hideResultStringBuilder.AppendLine(file.svnURL);
        }
        hideChooseResult = hideResultStringBuilder.ToString();
    }
    
    // 添加路径到展示路径中
    private void AddSVNToolFileIntoShowResult(SVNToolFile file)
    {
        Boolean ifExist = false;
        foreach (SVNToolFile toolFile in showResult)
        {
            if (toolFile.path.Equals(file.path))
            {
                ifExist = true;
                break;
            }
        }
        if (!ifExist)
            showResult.Add(file);
    }
    
    /// <summary>
    /// 添加路径到未选择路径中
    /// </summary>
    /// <param name="file"></param>
    private void AddSVNToolFileIntoHideResult(SVNToolFile file)
    {
        Boolean ifExist = false;
        foreach (SVNToolFile toolFile in hideResult)
        {
            if (toolFile.path.Equals(file.path))
            {
                ifExist = true;
                break;
            }
        }
        if (!ifExist)
            hideResult.Add(file);
    }
    
    public static string HandleCopyPaste(int controlID)
    {
        if (controlID == GUIUtility.keyboardControl)
        {
            if (Event.current.type == UnityEngine.EventType.KeyUp && (Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command))
            {
                if (Event.current.keyCode == KeyCode.C)
                {
                    Event.current.Use();
                    TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    editor.Copy();
                }
                else if (Event.current.keyCode == KeyCode.V)
                {
                    Event.current.Use();
                    TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    editor.Paste();
#if UNITY_5_3_OR_NEWER || UNITY_5_3
                    return editor.text; //以及更高的unity版本中editor.content.text已经被废弃，需使用editor.text代替
#else
                    return editor.content.text;
#endif
                }
                else if (Event.current.keyCode == KeyCode.A)
                {
                    Event.current.Use();
                    TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    editor.SelectAll();
                }
            }
        }
        return null;
    }
    
}