//* SVN同步工具工具类

using System;
using System.Collections.Generic;
using System.Threading;
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
    public static List<String> GetNeedCommitSVNToolFileList(String path)
    {
        List<String> res = new List<String>();
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
            // NOTE: 当前展示的内容为一下，当前排除文件/文件夹的 创建、删除
            if (s.Length > 8 && (
                s.StartsWith("M ")       // 内容发生修改
                || s.StartsWith("C ")    // 发生冲突
                || s.StartsWith("A ")    // 预订加入到版本库
                || s.StartsWith("K ")    // 被锁定
                )) {
                res.Add(s.Substring(8).Replace('\\', '/').Trim());
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
        for(Int32 i = 0; i < files.Count; i++)
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
