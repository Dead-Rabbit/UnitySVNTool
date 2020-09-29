// CREATED:    2020.9.26
// PURPOSE:    SVN工具中的文件Obj
// AUTHOR:    WangZixiao

using System;
using Editor;
using UnityEngine;

[Serializable]
public class SVNToolFile : SVNToolObj
{
    [NonSerialized] public EnumSVNToolFileNeedSyncState fileSyncState = EnumSVNToolFileNeedSyncState.NONE;
    
    public SVNToolFile(String path)
    {
        this.path = path;
    }
    
    /// <summary>
    /// 刷新同步状态
    /// </summary>
    public void RefreshSVNToolFileNeedSyncState()
    {
        Boolean ifCanSync = UESvnOperation.GetSvnOperation().Status(path);
    }
}
