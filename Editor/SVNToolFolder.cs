// CREATED:    2020.9.26
// PURPOSE:    SVN工具中的文件夹Obj
// AUTHOR:    WangZixiao

using System;
using System.Collections.Generic;
using Editor;
using UnityEngine;

[Serializable]
public class SVNToolFolder : SVNToolObj
{
    // 当前文件夹下所需同步的文件
    [NonSerialized] public List<SVNToolFile> contentNeedSyncFiles = new List<SVNToolFile>();
    
    [NonSerialized] public Boolean openFolder = true;
    
    public SVNToolFolder(String path) : base(path)
    {
        this.path = path;
    }

    public void InitSVNToolFolder()
    {
        contentNeedSyncFiles = new List<SVNToolFile>();
        openFolder = true;
    }

    public void SetSVNToolFolderNeedSyncFiles(List<SVNToolFile> files)
    {
        contentNeedSyncFiles = files;
        foreach (SVNToolFile file in contentNeedSyncFiles)
        {
            file.CanBeCommit = true;
        }
    }
    
    /// <summary>
    /// 获取当前选择的文件数量
    /// </summary>
    /// <returns></returns>
    public List<SVNToolFile> GetTotalSelectedSVNToolFiles()
    {
        List<SVNToolFile> selectedFiles = new List<SVNToolFile>();
        foreach (SVNToolFile file in contentNeedSyncFiles)
        {
            if (file.ifSelected)
            {
                selectedFiles.Add(file);
            }
        }

        return selectedFiles;
    }

    /// <summary>
    /// 刷新同步状态
    /// </summary>
    public void RefreshSVNToolFolderNeedSyncFiles()
    {
        UESvnOperation.GetSvnOperation().FolderStatus(path);
    }
    
    public EnumSVNToolFolderNeedSyncState GetSVNToolFileCurrentSyncState()
    {
        if (contentNeedSyncFiles.Count > 0)
        {
            if (GetTotalSelectedSVNToolFiles().Count == contentNeedSyncFiles.Count)
            {
                return EnumSVNToolFolderNeedSyncState.SELECTED_ALL;
            }
        
            return EnumSVNToolFolderNeedSyncState.SELECTED_PART;
        }
        
        return EnumSVNToolFolderNeedSyncState.NONE;
    }
}
