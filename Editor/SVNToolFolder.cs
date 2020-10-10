// CREATED:    2020.9.26
// PURPOSE:    SVN工具中的文件夹Obj
// AUTHOR:    WangZixiao

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class SVNToolFolder : SVNToolObj
{
    // 当前文件夹下所需同步的文件夹
    
    
    // 当前文件夹下所需同步的文件
    [NonSerialized] public List<SVNToolFile> contentNeedSyncFiles = new List<SVNToolFile>();
    
    [NonSerialized] public Boolean openFolder = true;
    
    public SVNToolFolder(String path) : base(path)
    {
        this.path = path.Trim();
    }

    public void InitSVNToolFolder()
    {
        contentNeedSyncFiles = new List<SVNToolFile>();
        openFolder = true;
    }

    /// <summary>
    /// 设置文件夹下所有的可提交的文件
    /// </summary>
    /// <param name="files"></param>
    public void SetSVNToolFolderNeedSyncFoldersAndFiles(List<String> paths)
    {
        if (paths.Count == 0)
            return;

        List<SVNToolFile> newFiles = new List<SVNToolFile>();
        List<SVNToolFolder> newFolders = new List<SVNToolFolder>();

        // 判断路径是文件还是文件夹
        foreach (string s in paths)
        {
            if (Directory.Exists(s))
            {
                Debug.Log("directory " + s);
                newFolders.Add(new SVNToolFolder(s));
            } else if (File.Exists(s))
            {
                Debug.Log("file " + s);
                newFiles.Add(new SVNToolFile(s));
            }
        }

        contentNeedSyncFiles = newFiles;
        
        if (contentNeedSyncFiles.Count > 0) 
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (SVNToolFile file in contentNeedSyncFiles)
            {
                file.CanBeCommit = true;
                stringBuilder.Append(file.path).Append(" ");
            }
            
            String[] resultFilesInfo = UESvnOperation.GetSvnOperation().ShowFileUrl(stringBuilder.ToString()).Split('\n');
            for (Int32 i = 0; i < contentNeedSyncFiles.Count; i++)
            {
                contentNeedSyncFiles[i].SyncFileSVNURL(resultFilesInfo[i]);
            }
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
