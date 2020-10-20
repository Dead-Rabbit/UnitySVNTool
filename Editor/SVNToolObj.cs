using System;

[Serializable]
public class SVNToolObj
{
	public String path;	// 路径
	public String name;	// 名称

	public SVNToolObj(String path)
	{
		name = path.Substring(path.LastIndexOf("/") + 1);
	}

}