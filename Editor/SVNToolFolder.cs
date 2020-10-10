// CREATED:    2020.9.26
// PURPOSE:    SVN工具中的文件夹Obj
// AUTHOR:    WangZixiao

using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class SVNToolFolder : SVNToolObj
{
    // 当前文件夹下所需同步的文件夹
    [NonSerialized] public List<SVNToolFolder> contentNeedSyncFolders = new List<SVNToolFolder>();
    
    // 当前文件夹下所需同步的文件
    [NonSerialized] public List<SVNToolFile> contentNeedSyncFiles = new List<SVNToolFile>();
    
    [NonSerialized] public Boolean openFolder = true;

    [NonSerialized] public Boolean existNewFileOrFolder;
    
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
    public void SetSVNToolFolderNeedSyncFoldersAndFiles(List<SVNToolPath> paths)
    {
        if (paths.Count == 0)
            return;

        List<SVNToolFile> newFiles = new List<SVNToolFile>();
        List<SVNToolFolder> newFolders = new List<SVNToolFolder>();

        existNewFileOrFolder = false;

        // 判断路径是文件还是文件夹
        foreach (SVNToolPath toolPath in paths)
        {
            if (toolPath.pathType == EnumSVNToolPathType.NO_CONTROL)
            {
                existNewFileOrFolder = true;
                continue;
            }
            
            String s = toolPath.path;
            if (s.IndexOf('.', s.LastIndexOf('/')) > -1) {
                newFiles.Add(new SVNToolFile(s));
            } else {
                existNewFileOrFolder = true;
                newFolders.Add(new SVNToolFolder(s));
            }
        }

        contentNeedSyncFiles = newFiles;
        contentNeedSyncFolders = newFolders;
        
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

public class SVNToolPath
{
    public String path;

    public EnumSVNToolPathType pathType;
    
    public SVNToolPath(String path, EnumSVNToolPathType pathType)
    {
        this.path = path;
        this.pathType = pathType;
    }
}