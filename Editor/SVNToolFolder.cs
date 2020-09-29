// CREATED:    2020.9.26
// PURPOSE:    SVN工具中的文件夹Obj
// AUTHOR:    WangZixiao

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SVNToolFolder : SVNToolObj
{
    [NonSerialized]
    // 当前文件夹下所需同步的文件
    public List<SVNToolFile> contentNeedSyncFiles = new List<SVNToolFile>();

    [NonSerialized] public Boolean ifSelectAll = true;
    
    public SVNToolFolder(String path) : base(path)
    {
        this.path = path;
    }

    /// <summary>
    /// 刷新同步状态
    /// </summary>
    public void RefreshSVNToolFolderNeedSyncFiles()
    {
        UESvnOperation.GetSvnOperation().FolderStatus(path);
    }
}
