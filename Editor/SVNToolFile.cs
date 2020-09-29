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

    [NonSerialized] public Boolean CanBeCommit = false;

    public Boolean ifSelected = true;    // 是否被选中
    
    public SVNToolFile(String path) : base(path)
    {
        this.path = path;
        Debug.Log(path);
    }

    public EnumSVNToolFileNeedSyncState GetSVNToolFileCurrentSyncState()
    {
        if (CanBeCommit && ifSelected)
        {
            return EnumSVNToolFileNeedSyncState.SELECTED_NEED_COMMIT;
        }

        if (CanBeCommit && !ifSelected)
        {
            return EnumSVNToolFileNeedSyncState.NEED_COMMIT_WITHOUT_SEELCTED;
        }

        return EnumSVNToolFileNeedSyncState.CANNOT_BE_COMMIT;
    }
}
