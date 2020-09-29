//* SVN同步工具工具类

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SVNToolUtil
{
    public static Boolean Syncing = false;
    private static List<SVNToolFolder> folders = new List<SVNToolFolder>();
    private static List<SVNToolFile> files = new List<SVNToolFile>();
    
    public static void GetSVNToolObjStateJobHandle(List<SVNToolFolder> folders, List<SVNToolFile> files)
    {
        SVNToolUtil.files = files;
        SVNToolUtil.folders = folders;
        
        ThreadStart childRef = new ThreadStart(GetSVNToolObjStateJob);
        Thread childThread = new Thread(childRef);
        childThread.Start();
    }

    private static void GetSVNToolObjStateJob()
    {
        Syncing = true;
        foreach (SVNToolFile file in files)
        {
            Boolean ifCanSync = UESvnOperation.GetSvnOperation().Status(file.path);
            Debug.Log(ifCanSync);
        }
        Syncing = false;
    }
}