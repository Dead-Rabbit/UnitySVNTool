using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SVNToolExecuteWindow : EditorWindow
{
    private String m_SVNToolSourcePath = "/Tools/Editor/UESVNTool/SVNToolSource/";
    private String globalFilePath;
    private Boolean checkedFile = false;
    
    private SVNToolPrefabWrap _prefabWrap = new SVNToolPrefabWrap(5);
    private List<SVNToolPrefab> storedPrefabs = new List<SVNToolPrefab>();

    private String m_SvnToolUserID = "";

    [MenuItem("Tools/SVN/SVN快速同步 #&M")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SVNToolExecuteWindow));
    }

    /// <summary>
    /// Unity 绘制
    /// </summary>
    void OnGUI()
    {
        if (!checkedFile)
        {
            // 读取当前设置的工号
            m_SvnToolUserID = PlayerPrefs.GetString(SVNToolWindow.SVN_TOOL_USER_ID_PREF_NAME);

            if (String.IsNullOrEmpty(m_SvnToolUserID))
            {
                ShowNotification(new GUIContent("当前未选择工作组"));

            }

            globalFilePath = String.Concat(Application.dataPath, m_SVNToolSourcePath, m_SvnToolUserID, ".json");

            checkedFile = true;
            if (!File.Exists(globalFilePath))
            {
                ShowNotification(new GUIContent("不存在配置文件"));
                Close();
                return;
            }

            ReadSVNToolPrefabsFromJson();
            CommitFoldersAndFiles();
        }
    }
    /// <summary>
    /// 读入序列化的预设
    /// </summary>
    private void ReadSVNToolPrefabsFromJson()
    {
        // 反序列化
        string dataAsJson = File.ReadAllText(globalFilePath);     //读取所有数据送到json格式的字符串里面。
        _prefabWrap = JsonUtility.FromJson<SVNToolPrefabWrap>(dataAsJson);
        storedPrefabs = _prefabWrap.prefabs;
    }

    private void CommitFoldersAndFiles()
    {
        Boolean ifShowLog = false;

        StringBuilder pathBuilder = new StringBuilder();
        // 提交所有文件
        foreach (SVNToolPrefab prefab in storedPrefabs)
        {
            if (!prefab.ifSelected)
                continue;

            foreach (SVNToolFile file in prefab.contentFilePath)
            {
                if (SVNToolUtil.GetSVNToolFileStateJob(file.path))
                {
                    pathBuilder.Append(file.path.Trim()).Append(" ");
                }
            }
        }
        
        if (!String.IsNullOrEmpty(pathBuilder.ToString()))
        {
            ifShowLog = true;
            UESvnOperation.GetSvnOperation().CommitFile(pathBuilder.ToString());
        }
        
        // 提交所有文件夹
        foreach (SVNToolPrefab prefab in storedPrefabs)
        {
            if (!prefab.ifSelected)
                continue;

            foreach (SVNToolFolder folder in prefab.contentFolderPath)
            {
                folder.SetSVNToolFolderNeedSyncFoldersAndFiles(SVNToolUtil.GetNeedCommitSVNToolFileList(folder.path));

                if (folder.existFildOrFolderHasStatus)
                {
                    ifShowLog = true;
                    UESvnOperation.GetSvnOperation().CommitFile(folder.path);
                }
            }
        }

        // 如果存在可提交的内容，则展示日志
        if (ifShowLog)
        {
            UESvnOperation.GetSvnOperation().ShowCommitLog(Application.dataPath);
        }

        Close();
    }
}

