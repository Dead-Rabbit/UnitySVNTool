using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class SVNToolWindow : EditorWindow
{
    #region 用户ID

    private String m_SvnToolUserID = null;      // 当前拿到的用户ID
    private const String SVN_TOOL_USER_ID_PREF_NAME = "svn_tool_user_id";
    private String m_InputSvnToolUserID = "";

    #endregion

    #region 存储配置相关

    private String m_SVNToolSourcePath = "/Editor/Source/SVNTool/";
    private Boolean m_FinishReadData = false;
    private List<SVNToolPrefab> storedPrefabs = new List<SVNToolPrefab>();

    #endregion

    [MenuItem("SVN/SVN同步工具")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow(typeof(SVNToolWindow));
    }

    public void OnEnable()
	{
        // 读取当前设置的工号
        m_SvnToolUserID = PlayerPrefs.GetString(SVN_TOOL_USER_ID_PREF_NAME);
	}

    /// <summary>
    /// Unity 绘制
    /// </summary>
	void OnGUI()
    {
        // 存在工号
        if (!String.IsNullOrEmpty(m_SvnToolUserID))
		{
            GUISVNToolOperation();
        }
        else
		{
            GUINeedPlayerInputID();
        }
    }

	#region 操作界面

    private void GUISVNToolOperation()
	{
        String filePath = String.Concat(Application.dataPath, m_SVNToolSourcePath, m_SvnToolUserID, ".json");
           
        // 判断是否读取存储的配置数据
        if (!m_FinishReadData)
        {
	        ReadSVNToolPrefabsFromJson(filePath);
	        m_FinishReadData = true;
        }
        
        // 展示预设列表
        

        // 写入数据用按钮
        if (GUILayout.Button("存储", GUILayout.Width(70f)))
        {
	        File.WriteAllText(filePath, JsonUtility.ToJson(storedPrefabs));
        }
	}

    /// <summary>
    /// 读入序列化的预设
    /// </summary>
    private void ReadSVNToolPrefabsFromJson(String filePath)
    {     
	    // 反序列化
	    if (File.Exists(filePath))
	    {
		    string dataAsJson = File.ReadAllText(filePath); //读取所有数据送到json格式的字符串里面。
		    storedPrefabs = JsonUtility.FromJson<List<SVNToolPrefab>>(dataAsJson);
	    }
    }

	#endregion

	#region 用户输入工号界面

	/// <summary>
	/// 用户输入工号界面
	/// </summary>
	private void GUINeedPlayerInputID()
	{
        // 不存在工号缓存，需要用户输入工号并确认
        GUILayout.Label("请输入工号", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        m_InputSvnToolUserID = EditorGUILayout.TextField("用户工号", m_InputSvnToolUserID);
        // TODO 添加针对工号的解释
        if (GUILayout.Button("确定", GUILayout.Width(70f)))
        {
            if ("" != m_InputSvnToolUserID)
            {
                m_SvnToolUserID = m_InputSvnToolUserID;
                PlayerPrefs.SetString(SVN_TOOL_USER_ID_PREF_NAME, "242");
            }
            else
            {
                ShowNotification(new GUIContent("工号不能为空"));
            }
        }
        GUILayout.EndHorizontal();
    }

	#endregion
}
