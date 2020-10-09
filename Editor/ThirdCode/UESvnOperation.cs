using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
public class SECURITY_ATTRIBUTES
{
    public int nLength;
    public string lpSecurityDescriptor;
    public bool bInheritHandle;
}

[StructLayout(LayoutKind.Sequential)]
public struct STARTUPINFO
{
    public int cb;
    public string lpReserved;
    public string lpDesktop;
    public int lpTitle;
    public int dwX;
    public int dwY;
    public int dwXSize;
    public int dwYSize;
    public int dwXCountChars;
    public int dwYCountChars;
    public int dwFillAttribute;
    public int dwFlags;
    public int wShowWindow;
    public int cbReserved2;
    public byte lpReserved2;
    public IntPtr hStdInput;
    public IntPtr hStdOutput;
    public IntPtr hStdError;
}

[StructLayout(LayoutKind.Sequential)]
public struct PROCESS_INFORMATION
{
    public IntPtr hProcess;
    public IntPtr hThread;
    public int dwProcessId;
    public int dwThreadId;
}


public class UESvnOperation
{
    public string WorkFolder { get; set; }
    public string ServerPath { get; set; }
    public string RootWorkFolder { get; set; }
    public string RootServerPath { get; set; }
    public static string SVNExePath { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string OutputResult { get; set; }
    public bool Initialized { get; private set; }
    
    public char[] msPathTrim = new char[] { '\\', '/' };

    private static UESvnOperation Instance = null;

    public static UESvnOperation GetSvnOperation()
    {
        if (null == Instance)
        {
            Instance = new UESvnOperation();
        }

        return Instance;
    }

    private UESvnOperation()
    {
        if (!Initialized) InitSvn();
    }
    public bool InitSvn()
    {
        if (Initialized)
            return true;

#if UNITY_IOS
		SVNExePath = "/usr/local/Cellar/subversion17/1.7.14_1/bin/svn";
#else
        string path = GetSvnPath();
        if (string.IsNullOrEmpty(path))
        {
            UnityEditor.EditorUtility.DisplayDialog("SVN", "No SVN Client Installed!", "OK");
            Initialized = false;
            return false;
        }

        SVNExePath = path + "bin\\svn.exe";
        if (!System.IO.File.Exists(SVNExePath))
        {
            UnityEditor.EditorUtility.DisplayDialog("SVN", "No SVN Commandline Component Found!", "OK");
            Initialized = false;
            return false;
        }

        RegistryKey key = Registry.CurrentUser;
        RegistryKey miscellanyKey = key.CreateSubKey("Software\\Tigris.org\\Subversion\\Config\\miscellany");
        if (miscellanyKey != null)
        {
            miscellanyKey.SetValue("enable-auto-props", "yes");
        }
        key.CreateSubKey("Software\\Tigris.org\\Subversion\\Config\\auto-props");
        if (miscellanyKey != null)
        {
            if (miscellanyKey.GetValue("*.*") != null)
                miscellanyKey.DeleteValue("*.*");
            //miscellanyKey.SetValue("*.*", "svn:needs-lock = ");
        }
#endif

        if (!TestSVNConnect())
        {
            UnityEditor.EditorUtility.DisplayDialog("SVN", "connect SVN failed!" + OutputResult, "OK");
            return false;
        }

        Initialized = true;
        return true;
    }

    public void ChangeSvnUserName()
    {
        Initialized = false;
    }

    public string GetSvnPath()
    {
        RegistryKey key = Registry.LocalMachine;
        RegistryKey svnkey = key.OpenSubKey("Software\\TortoiseSVN\\");
        if (svnkey == null)
        {
            return string.Empty;
        }

        return svnkey.GetValue("Directory").ToString();
    }

    public string GetAuthenCmd()
    {
        //return string.Format(" --username {0} --password {1}", UserName, Password);// --non-interactive
        return string.Empty;
    }

    public bool CreateProject(string path)
    {
        if (!Initialized)
            return false;

        string commandline = string.Format(" mkdir -m mkdir \"{0}\" ", path) + GetAuthenCmd();
        ProcessCommand(commandline);

        return true;
    }

    public bool SetProjectPath(string path)
    {
        string workfolder = RootWorkFolder;
        workfolder.TrimEnd('\\');
        workfolder += ("\\" + path);

        if (!System.IO.Directory.Exists(workfolder))
        {
            return false;
        }

        string url = RootServerPath;
        url.TrimEnd('/');
        url += ("/" + path);
        url.Replace("\\", "/");

        WorkFolder = workfolder;
        ServerPath = url;

        return true;
    }

    public bool Update(string path = "")
    {
        if (!Initialized)
            return false;

        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = " update --accept tf";
        }
        else
        {
            commandline = string.Format(" update \"{0}\" --accept tf", path);
        }

        commandline += GetAuthenCmd();
        OutputResult = ProcessCommand(commandline);

        if (OutputResult.Contains("Error"))
        {
            return false;
        }

		ShowSvnError (OutputResult);

        return true;
    }

    public bool Status(string path = "")
    {
        if (!Initialized)
            return false;

        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = " status -u";
        }
        else
        {
            commandline = string.Format(" status {0} -u", path);
        }

        commandline += GetAuthenCmd();
        ProcessCommand(commandline);
        
        if (string.IsNullOrEmpty(OutputResult))
        {
            return true;
        }

		ShowSvnError (OutputResult);

        return true;
    }

    public bool FileStatus(string path = "")
    {
        if (!Initialized)
            return false;

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string commandline;
        String output = "";
        commandline = string.Format(" status {0}", path);
        commandline += GetAuthenCmd();
        ProcessCommandShowOutput(commandline, out output);

        if (String.IsNullOrEmpty(output))
        {
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// 获取文件夹下的svn状态(获取文件夹下可提交的文件)
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public String FolderStatus(string path = "")
    {
        if (!Initialized)
            return null;

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        String output = "";
        string commandline = string.Format(" status {0}", path);
        commandline += GetAuthenCmd();
        
        ProcessCommandShowOutput(commandline, out output);
        
        if (String.IsNullOrEmpty(output))
        {
            return null;
        }

        return output;
    }
    
    /// <summary>
    /// 获取文件在SVN中的相对路径
    /// </summary>
    /// <param name="paths"></param>
    /// <returns></returns>
    public String ShowFileUrl(String path)
    {
        if (!Initialized)
            return null;

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        String output = "";
        string commandline = string.Format(" info {0} ", path);
        commandline += GetAuthenCmd();
        
        ProcessCommandShowOutput(commandline, out output);
        
        if (String.IsNullOrEmpty(output))
        {
            return null;
        }

        String beginStr = "Relative URL: ^";
        StringBuilder resultBuilder = new StringBuilder();
        foreach (string s in output.Split('\n'))
        {
            if (s.StartsWith(beginStr))
            {
                resultBuilder.AppendLine(s.Substring(beginStr.Length));
            }
        }

        return resultBuilder.ToString();
    }

    public bool StatusShowLog(out string StandLog, string path = "")
    {
        StandLog = "";
        if (!Initialized)
            return false;

        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = " status";
        }
        else
        {
            commandline = string.Format(" status {0}", path);
        }

        commandline += GetAuthenCmd();
        ProcessCommandShowOutput(commandline, out StandLog);

		ShowSvnError (OutputResult);

        return true;
    }

    public bool LockDirectoryFile(string filename)
    {
        if (!Initialized)
            return false;

        string commandline = string.Format(" lock \"{0}\"\\* ", filename);
        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        if (OutputResult.Contains("is already locked by"))
        {
            int pos = 0;
            pos = OutputResult.IndexOf('\'', pos + 1);
            pos = OutputResult.IndexOf('\'', pos + 1);
            int start = OutputResult.IndexOf('\'', pos + 1);
            int end = OutputResult.IndexOf('\'', start + 1);
            string name = OutputResult.Substring(start + 1, end - start - 1);
            if (name != UserName)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", OutputResult, "OK");
                return false;
            }
        }

        return true;
    }

    public bool Lock(string filename)
    {
        if (!Initialized)
            return false;

        string commandline = string.Format(" lock {0} ", filename);
        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        if (OutputResult.Contains("is already locked by"))
        {
            int pos = 0;
            pos = OutputResult.IndexOf('\'', pos + 1);
            pos = OutputResult.IndexOf('\'', pos + 1);
            int start = OutputResult.IndexOf('\'', pos + 1);
            int end = OutputResult.IndexOf('\'', start + 1);
            string name = OutputResult.Substring(start + 1, end - start - 1);
            if (name != UserName)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", OutputResult, "OK");
                return false;
            }
        }

		OutputResult = "";

        return true;
    }

    public bool Unlock(string filename)
    {
        if (!Initialized)
            return false;

        string commandline = string.Format(" unlock \"{0}\" ", filename);
        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

		OutputResult = "";
        return true;
    }

    public bool Commit(string path = "", string message = "autoCommit")
    {
        if (!Initialized)
            return false;

        string messagestr = "[提交类型][修改说明]" + message + "[相关禅道][所属版本][验证情况]";
        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = string.Format(" commit -m \"{0}\"", messagestr);
        }
        else
        {
            commandline = string.Format(" commit \"{0}\" -m \"{1}\"", path, messagestr);
        }

        //commandline += GetAuthenCmd();
        ProcessCommand(commandline);
		if (OutputResult.Contains("Commit failed"))
        {
            if (OutputResult.Contains("child"))
            {
                string parent = path.Remove(path.LastIndexOf('\\'));
                commandline = string.Format(" commit \"{0}\" -m \"{1}\"", parent, messagestr);
                ProcessCommand(commandline);
                if (OutputResult.Contains("Commit failed"))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

		ShowSvnError (OutputResult);

        return true;
    }

	private void ShowSvnError(string OutputResult)
    {
        if (OutputResult.Contains("Write error"))
        {
            return;
        }
		if (OutputResult.Contains ("svn upgrade")) 
		{
//            "版本错误，这个目录不能使用该版本的svn，请先使用手动方式操作svn " + OutputResult
		}
		else if(OutputResult.Contains("svn: E"))
		{
//			UEEngine.UELogMan.LogMsg("svn其他错误，不处理 " + OutputResult);
		}
        else
        {
            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.ShowNotification(new UnityEngine.GUIContent("提交成功"));
            }
            else if (EditorWindow.mouseOverWindow != null)
            {
                EditorWindow.mouseOverWindow.ShowNotification(new UnityEngine.GUIContent("提交成功"));
            }
        }
	}

    public bool CommitForceAdd(string path = "", string message = "autoCommit")
    {
        if (!Initialized)
            return false;

        string messagestr = "[提交类型][修改说明]" + message + "[相关禅道][所属版本][验证情况]";
        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = string.Format(" commit -m \"{0}\"", messagestr);
        }
        else
        {
            commandline = string.Format(" commit \"{0}\" -m \"{1}\"", path, messagestr);
        }

        commandline += GetAuthenCmd();
        ProcessCommand(commandline);
        if (OutputResult.Contains("Commit failed"))
        {
            if (OutputResult.Contains("not under version control"))
            {
                AddFile(path);
                return Commit(path, message);
            }
            return false;
        }

        return true;
    }

    public bool AddFile(string path = "")
    {
        if (!Initialized)
            return false;

        string commandline = string.Empty;
        if (!string.IsNullOrEmpty(path))
        {
            commandline = string.Format(" add \"{0}\" --force", path);
        }

        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        if (OutputResult.Contains("Can't find parent"))
        {
            string parent = path.Remove(path.LastIndexOf('\\'));
            AddFile(parent);
        }

        return true;
    }

    public bool Delete(string path)
    {
        if (!Initialized)
            return false;

        string commandline = string.Format(" delete \"{0}\" ", path);
        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        return true;
    }

    public bool Revert(string path = "")
    {
        if (!Initialized)
            return false;

        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = " revert * \"\" -R";
        }
        else
        {
            commandline = string.Format(" revert \"{0}\" --depth=infinity", path);//-R 
        }

        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        return true;
    }

    public bool Checkout(string url, string path = "")
    {
        if (!Initialized)
            return false;

        string svnUrl = GetSVNPathByPath(url);

        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = string.Format(" checkout \"{0}\" \"\" --force ", svnUrl);
        }
        else
        {
            commandline = string.Format(" checkout \"{0}\" \"{1}\" --force ", svnUrl, path);
        }

        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        return true;
    }

    public bool Cleanup(string path = "")
    {
        if (!Initialized)
            return false;

        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = " cleanup ";
        }
        else
        {
            if (!System.IO.Directory.Exists(path))
                return false;

            commandline = string.Format(" cleanup \"{0}\" ", path);
        }

        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        return true;
    }

    public bool PropSet_NeedLock(string path)
    {
        if (!Initialized)
            return false;

        string commandline;
        if (string.IsNullOrEmpty(path))
        {
            commandline = " propset svn:needs-lock \"\" \"\" -R -q";
        }
        else
        {
            commandline = string.Format(" propset svn:needs-lock \"\" \"{0}\" -R -q", path);
        }

        commandline += GetAuthenCmd();
        ProcessCommand(commandline);

        return true;
    }

    public bool IsFileExist(string filename)
    {
        if (!Initialized)
            return false;

        if (string.IsNullOrEmpty(filename))
            return false;

        string commandline = string.Format(" lock \"{0}\" ", filename);
        commandline += GetAuthenCmd();
        ProcessCommand(commandline);
        if (OutputResult.Contains("is not under version control"))
        {
            return false;
        }

        return true;
    }

    public bool TestSVNConnect()
    {
        //if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
        //    return false;

        string commandline = " up dummy";
        string authen = string.Format(" --non-interactive --username {0} --password {1}", UserName, Password);
        commandline += authen;
        OutputResult = ProcessCommand(commandline);
        //if (string.IsNullOrEmpty(OutputResult))
        //{
        //    return false;
        //}

        if (OutputResult.Contains("No repository found") ||
            OutputResult.Contains("Authentication realm") ||
            OutputResult.Contains("Authentication error") ||
            OutputResult.Contains("Can't connect to host"))
        {
//            UEEngine.UELogMan.LogError("TestSVNConnect error: " + OutputResult);
            return false;
        }

        return true;
    }

    public static string ProcessCommand(string command, string svnpath = "", bool bUseStandOut = false)
    {
        // create pipe here
        if (svnpath == "")
        {
            svnpath = SVNExePath;
        }

        //ProcessStartInfo psi = new ProcessStartInfo(svnpath, command);
        //psi.RedirectStandardOutput = true;
        //psi.CreateNoWindow = true;
        //psi.WindowStyle = ProcessWindowStyle.Hidden;
        //psi.UseShellExecute = false;
        //psi.RedirectStandardOutput = true;
        //psi.RedirectStandardError = true;
        //psi.ErrorDialog = true;
        string strs = "";
        Process p = new Process();
        p.StartInfo.FileName = svnpath;
        p.StartInfo.Arguments = command;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.RedirectStandardOutput = bUseStandOut;
        p.StartInfo.CreateNoWindow = false;
        p.EnableRaisingEvents = true;

        if (p.Start())
        {
            strs = p.StandardError.ReadToEnd();
            if(bUseStandOut)
            {
                p.StandardOutput.ReadToEnd();
            }
        }
        
        p.WaitForExit();
        p.Close();

        return strs;
    }

    string ProcessCommandShowOutput(string command, out string StandardOutput)
    {
        StandardOutput = "";

        Process p = new Process();
        p.StartInfo.FileName = SVNExePath;
        p.StartInfo.Arguments = command;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardError = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.CreateNoWindow = true;
        p.EnableRaisingEvents = true;
        
//        Debug.Log(p.);
        
        if (p.Start())
        {
            StandardOutput = p.StandardOutput.ReadToEnd();
        }

        p.WaitForExit();

        p.Close();

        return OutputResult;
    }


    public void StatusFile(string path)
    {
        string commandline = string.Format("/c TortoiseProc.exe /command:status /path:\"{0}\" /closeonend:2", path);
        ProcessCommand(commandline, "cmd.exe");
    }

    public void LockFile(string path)
    {
        string commandline = string.Format("/c TortoiseProc.exe /command:lock /path:\"{0}\" /closeonend:2", path);
        string log = ProcessCommand(commandline, "cmd.exe");
    }

    public void UnLockFile(string path)
    {
        string commandline = string.Format("/c TortoiseProc.exe /command:unlock /path:\"{0}\" /closeonend:2", path);
        ProcessCommand(commandline, "cmd.exe");
    }

    public void UpdateFile(string path)
    {
        string commandline = string.Format("/c TortoiseProc.exe /command:update --accept tf --force /path:\"{0}\" /closeonend:2", path);
        ProcessCommand(commandline, "cmd.exe");
    }

    public void CommitFile(string path)
    {
        string commandline = string.Format("/c TortoiseProc.exe /command:commit /path:\"{0}\" /closeonend:0", path);
        ProcessCommand(commandline, "cmd.exe");
    }
    
    public void DiffFile(string path)
    {
        string commandline = string.Format("/c TortoiseProc.exe /command:diff /path:\"{0}\" /closeonend:0", path);
        ProcessCommand(commandline, "cmd.exe");
    }
    
    public string GetSVNPathByPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "";
        }

        return path.Replace(msPathTrim[0], msPathTrim[1]);
    }
}

