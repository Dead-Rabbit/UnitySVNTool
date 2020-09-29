namespace Editor
{
    public enum EnumSVNToolWindowEditState
    {
        VIEW,
        EDIT
    }

    // 文件同步状态
    public enum EnumSVNToolFileNeedSyncState
    {
        NONE,
        SELECTED_NEED_COMMIT,    // 可提交，已被选中
        NEED_COMMIT_WITHOUT_SEELCTED,    // 可提交，位被选中
        CANNOT_BE_COMMIT,    // 不可提交
    }
    
    // 文件夹同步状态
    public enum EnumSVNToolFolderNeedSyncState
    {
        NONE,
        SELECTED_ALL,    // 存在可提交，已全选
        SELECTED_PART,    // 存在可提交，未全选
    }
}