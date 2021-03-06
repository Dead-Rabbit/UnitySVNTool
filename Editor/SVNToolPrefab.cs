﻿/*
 * CREATED:     2020.9.26
 * PURPOSE:		SVN工具预设体
 * AUTHOR:      WangZixiao
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class SVNToolPrefabWrap
{
	public Int32 val = 10;
	public List<SVNToolPrefab> prefabs = new List<SVNToolPrefab>();

	public SVNToolPrefabWrap(Int32 num)
	{
		for (int i = 1; i < num; i++)
		{
			prefabs.Add(new SVNToolPrefab());
		}
	}
}

[Serializable]
public sealed class SVNToolPrefab
{
	public Int32 ID;        // 唯一ID

	public String name = ""; // 预设名称
	public Int32 order;  // 预设排序
	public Boolean isGlobal;    // 是否是全局预设
	public List<SVNToolFolder> contentFolderPath = new List<SVNToolFolder>();   // 预设中包含的文件夹路径
	public List<SVNToolFile> contentFilePath = new List<SVNToolFile>();         // 预设中包含的文件路径

	[NonSerialized] public Int32 differentCount = 0;    // 当前需要同步的文件数量
	[NonSerialized] public Int32 totalCount = 0;    // 总共的文件数量
	[NonSerialized] public Boolean initedFileStatus = false;    // 判断是否初始化过
	public Boolean ifSelected = true;

	/// <summary>
	/// 在其他预设文件中拷贝内容
	/// </summary>
	/// <param name="other"></param>
	public void CloneFromOtherSVNToolPrefab(SVNToolPrefab other)
	{
		name = other.name;
		order = other.order;
		isGlobal = other.isGlobal;
		contentFolderPath = other.contentFolderPath;
		contentFilePath = other.contentFilePath;
		ifSelected = other.ifSelected;
	}

	public SVNToolPrefab()
	{
	}

	public SVNToolPrefab(Int32 id)
	{
		ID = id;
	}

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

	/// <summary>
	/// 由配置计算基础信息
	/// </summary>
	public void InitSVNToolPrefabFileAndFolderInfo()
	{
		foreach (SVNToolFile file in contentFilePath)
		{
			file.name = file.path.Substring(file.path.LastIndexOf("/") + 1);
		}

		foreach (SVNToolFolder folder in contentFolderPath)
		{
			folder.name = folder.path.Substring(folder.path.LastIndexOf("/") + 1);
			folder.InitSVNToolFolder();
		}
	}

	/// <summary>
	/// 当完成SVN状态获取后调用
	/// </summary>
	public void FinishSVNToolPrefabSyncSVNStatus()
	{
		initedFileStatus = true;
		// 如果文件夹中的文件在本地缓存中，则设置为缓存的勾选状态
		foreach (SVNToolFolder folder in contentFolderPath)
		{
			foreach (SVNToolFile file in folder.contentNeedSyncFiles)
			{
				foreach (SVNToolFile prefabFile in contentFilePath)
				{
					if (prefabFile.path.Equals(file.path))
					{
						file.ifSelected = prefabFile.ifSelected;
					}
				}
			}
		}
	}

	/// <summary>
	/// 添加路径至配置中
	/// </summary>
	/// <param name="path"></param>
	public void AddNewSVNToolPath(String path)
	{
		if (Directory.Exists(path)) // 如果是文件夹
		{
			foreach (SVNToolFolder folder in contentFolderPath)
			{
				if (folder.path.Equals(path)) return;
			}

			SVNToolFolder newFolder = new SVNToolFolder(path);
			contentFolderPath.Add(newFolder);
		}
		else if (File.Exists(path)) // 如果是文件
		{
			foreach (SVNToolFile file in contentFilePath)
			{
				if (file.path.Equals(path)) return;
			}

			SVNToolFile newFile = new SVNToolFile(path);
			contentFilePath.Add(newFile);
		}
	}
}
