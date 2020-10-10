using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SVNToolExecuteWindow : EditorWindow
{
    private String m_SVNToolSourcePath = "/Editor/Source/SVNTool/";
    private String globalFilePath;
    private Boolean checkedFile = false;
    
    private SVNToolPrefabWrap _prefabWrap = new SVNToolPrefabWrap(5);
    private List<SVNToolPrefab> storedPrefabs = new List<SVNToolPrefab>();
    
    [MenuItem("SVN/SVN快速同步 #&M")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SVNToolExecuteWindow));
    }

    public void OnEnable()
    {
        // 读取当前设置的工号
        globalFilePath = String.Concat(Application.dataPath, m_SVNToolSourcePath, "Global.json");
    }
    
    /// <summary>
    /// Unity 绘制
    /// </summary>
    void OnGUI()
    {
        if (!checkedFile)
        {
            checkedFile = true;
            if (!File.Exists(globalFilePath))
            {
                Close();
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
        StringBuilder pathBuilder = new StringBuilder();
        // 提交所有文件
        foreach (SVNToolPrefab prefab in storedPrefabs)
        {
            foreach (SVNToolFile file in prefab.contentFilePath)
            {
                if (SVNToolUtil.GetSVNToolFileStateJob(file.path))
                {
                    pathBuilder.Append(file.path.Trim()).Append(" ");
                }
            }
        }
        
        if (!String.IsNullOrEmpty(pathBuilder.ToString()))
            UESvnOperation.GetSvnOperation().CommitFile(pathBuilder.ToString());
        
        // 提交所有文件夹
        foreach (SVNToolPrefab prefab in storedPrefabs)
        {
            foreach (SVNToolFolder folder in prefab.contentFolderPath)
            {
                folder.SetSVNToolFolderNeedSyncFoldersAndFiles(SVNToolUtil.GetNeedCommitSVNToolFileList(folder.path));

                if (folder.existFildOrFolderHasStatus)
                {
                    UESvnOperation.GetSvnOperation().CommitFile(folder.path);
                }
            }
        }

        Close();
    }
}

