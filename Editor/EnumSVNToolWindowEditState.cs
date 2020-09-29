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
        SELECTED_NEED_COMMIT,    // 已被选中，未提交
        NEED_COMMIT_WITHOUT_SEELCTED,    // 可提交，位被选中
        CANNOT_BE_COMMIT,    // 不可提交
    }
}