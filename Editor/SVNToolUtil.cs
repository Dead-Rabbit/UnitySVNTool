//* SVN同步工具工具类

using System;
using System.Collections.Generic;
using System.Threading;
using Editor;
using UnityEngine;
using Object = System.Object;

public static class SVNToolUtil
{
    public static Boolean Syncing = false;
    private static List<SVNToolFolder> folders = new List<SVNToolFolder>();
    private static List<SVNToolFile> files = new List<SVNToolFile>();
    private static readonly Object lockObj = new object();
    
    /// <summary>
    /// 利用多线程更新一个Prefab
    /// </summary>
    /// <param name="prefab"></param>
    public static void GetSVNToolObjStateJobHandle(SVNToolPrefab prefab)
    {
        files = prefab.contentFilePath;
        folders = prefab.contentFolderPath;
        
        // 同步文件
        ThreadStart fileSyncJob = GetSVNToolObjStateJob;
        Thread fileThread = new Thread(fileSyncJob);
        fileThread.Start();
        
        // 同步文件夹
        ThreadStart folderSyncJob = GetSVNToolFolderStateJob;
        Thread folderThread = new Thread(folderSyncJob);
        folderThread.Start();
    }

    /// <summary>
    /// 获取文件同步状态
    /// </summary>
    private static void GetSVNToolObjStateJob()
    {
        lock (lockObj)
        {
            Syncing = true;
            foreach (SVNToolFile file in files)
            {
                file.CanBeCommit = GetSVNToolFileStateJob(file.path);
            }
            Syncing = false;
        }
    }

    /// <summary>
    /// 获取文件夹状态
    /// </summary>
    private static void GetSVNToolFolderStateJob()
    {
        lock (lockObj)
        {
            Syncing = true;
            foreach (SVNToolFolder folder in folders)
            {
                folder.contentNeedSyncFiles = GetNeedCommitSVNToolFileList(folder.path);
            }
            Syncing = false;
        }
    }

    /// <summary>
    /// 检查路径下文件/文件夹是否可被Commit
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static Boolean GetSVNToolFileStateJob(String path)
    {
        return UESvnOperation.GetSvnOperation().FileStatus(path);
    }

    /// <summary>
    /// 获取文件夹路径下所有可提交的文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static List<SVNToolFile> GetNeedCommitSVNToolFileList(String path)
    {
        List<SVNToolFile> res = new List<SVNToolFile>();
        String commandRes = UESvnOperation.GetSvnOperation().FolderStatus(path);
        if (String.IsNullOrEmpty(commandRes))
        {
            return null;
        }

        // 拆解SVN结果
        String[] resList = commandRes.Split('\n');
        foreach (string s in resList)
        {
            if (s.Length > 8) {
                res.Add(new SVNToolFile(s.Substring(8)));
            }
        }
        
        return res;
    }
}