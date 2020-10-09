//* SVN同步工具工具类

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = System.Object;

public static class SVNToolUtil
{
    public static Boolean Syncing = false;
//    private static List<SVNToolFolder> folders = new List<SVNToolFolder>();
//    private static List<SVNToolFile> files = new List<SVNToolFile>();

    public static readonly Object lockObj = new object();
    
    /// <summary>
    /// 利用多线程更新一个Prefab
    /// </summary>
    /// <param name="prefab"></param>
    public static void GetSVNToolObjStateJobHandle(List<SVNToolPrefab> prefabs)
    {
        SVNToolThreadWithState prefabThread = new SVNToolThreadWithState(prefabs);
        Thread dealWithPrefabThread = new Thread(prefabThread.ThreadProc);
        dealWithPrefabThread.Start();
    }
    
    public static void GetSVNToolObjStateJobHandle(SVNToolPrefab prefab)
    {
        List<SVNToolPrefab> prefabs = new List<SVNToolPrefab>();
        prefabs.Add(prefab);
        SVNToolThreadWithState prefabThread = new SVNToolThreadWithState(prefabs);
        Thread dealWithPrefabThread = new Thread(prefabThread.ThreadProc);
        dealWithPrefabThread.Start();
    }
    
    /// <summary>
    /// 检查路径下文件/文件夹是否可被Commit
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Boolean GetSVNToolFileStateJob(String path)
    {
        return UESvnOperation.GetSvnOperation().FileStatus(path);
    }

    /// <summary>
    /// 获取文件夹路径下所有可提交的文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static List<SVNToolFile> GetNeedCommitSVNToolFileList(String path)
    {
        List<SVNToolFile> res = new List<SVNToolFile>();
        String commandRes = UESvnOperation.GetSvnOperation().FolderStatus(path);
        if (String.IsNullOrEmpty(commandRes))
        {
            return res;
        }

        // 拆解SVN结果
        String[] resList = commandRes.Split('\n');
        foreach (string s in resList)
        {
            if (s.Length > 8) {
                res.Add(new SVNToolFile(s.Substring(8).Replace('\\', '/')));
            }
        }
        
        return res;
    }
}


public class SVNToolThreadWithState 
{
    List<SVNToolPrefab> _prefabs = new List<SVNToolPrefab>();
    
    public SVNToolThreadWithState(List<SVNToolPrefab> prefabs) 
    {
        _prefabs = prefabs;
    }

    /// <summary>
    /// 执行Thread方法
    /// </summary>
    public void ThreadProc() 
    {
        lock (SVNToolUtil.lockObj)
        {
            SVNToolUtil.Syncing = true;

            foreach (SVNToolPrefab prefab in _prefabs)
            {
                GetSVNToolObjStateJob(prefab);
                GetSVNToolFolderStateJob(prefab);
                prefab.initedFileStatus = true;
            }
            
            SVNToolUtil.Syncing = false;
        }
    }
    
    /// <summary>
    /// 获取文件同步状态
    /// </summary>
    private void GetSVNToolObjStateJob(SVNToolPrefab prefab)
    {
        List<SVNToolFile> files = prefab.contentFilePath;
        for(Int32 i = 0; i < files.Count; i++)
        {
            SVNToolFile file = files[i];
            file.CanBeCommit = SVNToolUtil.GetSVNToolFileStateJob(file.path);
        }
    }

    /// <summary>
    /// 获取文件夹状态
    /// </summary>
    private void GetSVNToolFolderStateJob(SVNToolPrefab prefab)
    {
        List<SVNToolFolder> folders = prefab.contentFolderPath;
        for (Int32 i = 0; i < folders.Count; i++)
        {
            SVNToolFolder folder = folders[i];
            folder.SetSVNToolFolderNeedSyncFiles(SVNToolUtil.GetNeedCommitSVNToolFileList(folder.path));
        }
    }

}
