/*
 * CREATED:     2020.9.26
 * PURPOSE:		SVN工具预设体
 * AUTHOR:      WangZixiao
 */

using System;
using System.Collections.Generic;

[Serializable]
public sealed class SVNToolPrefab
{
	private Int32 m_Id;		// 唯一ID
	private String m_Name;	// 预设名称
	private Int32 m_Order;  // 预设排序
	private Boolean m_IsGlobal;	// 是否是全局预设
	private List<SVNToolObj> m_ContentPath = new List<SVNToolObj>();	// 预设中包含的文件、文件夹路径

	// TODO 拿到用户预制体后，读入json并序列化为intance放入Editor中
	private void InitSVNToolPrefabByLocalConfig()
	{
		//if (string.IsNullOrEmpty(whiteListFileName)) { return; }

		//OpenFileAtPath(Application.dataPath.Replace("\\", "/") + mWhiteListPath + "/" + whiteListFileName);

		//string line;
		//while ((line = mStreamReader.ReadLine()) != null)
		//{
		//	whiteList.Add(line);
		//}
		//CLoseFile();
	}

	// TODO 写入当前用户设定内容到对应配置中

}
