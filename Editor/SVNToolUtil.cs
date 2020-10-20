//* SVN同步工具工具类

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = System.Object;

public static class SVNToolUtil
{
    public static Boolean Syncing = false;

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
    public static List<SVNToolPath> GetNeedCommitSVNToolFileList(String path)
    {
        List<SVNToolPath> res = new List<SVNToolPath>();
        String commandRes = UESvnOperation.GetSvnOperation().FolderStatus(path);
        if (String.IsNullOrEmpty(commandRes))
        {
            return res;
        }

        // 拆解SVN结果
        String[] resList = commandRes.Split('\n');
        foreach (string s in resList)
        {
            // 如果字符串中的数据为修改后的
            if (s.Length > 8)
            {

                EnumSVNToolPathType pathType = EnumSVNToolPathType.NO_CONTROL;
                if (s.StartsWith("?"))
                {
                    pathType = EnumSVNToolPathType.NO_CONTROL;
                }
                else if (s.StartsWith("!"))
                {
                    pathType = EnumSVNToolPathType.DEL;
                }
                else
                {
                    pathType = EnumSVNToolPathType.MODIFY;
                }
                res.Add(new SVNToolPath(s.Substring(8).Replace('\\', '/').Trim(), pathType));
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
                prefab.FinishSVNToolPrefabSyncSVNStatus();
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
        for (Int32 i = 0; i < files.Count; i++)
        {
            SVNToolFile file = files[i];
            file.CanBeCommit = SVNToolUtil.GetSVNToolFileStateJob(file.path);
            // 获取文件的svn url
            file.SyncFileSVNURL();
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
            folder.SetSVNToolFolderNeedSyncFoldersAndFiles(SVNToolUtil.GetNeedCommitSVNToolFileList(folder.path));
        }
    }

}
