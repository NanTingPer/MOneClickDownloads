using Avalonia.Controls;
using Avalonia.Interactivity;
using MOneClickDownloads.DataModel.Enums;
using MOneClickDownloads.Service.Models;

namespace MOneClickDownloads.App.Views
{
    /// <summary>
    /// 模组冲突对话框，用于在下载前检测到本地已有同ID模组时，让用户选择处理方式。<br />
    /// <br />
    /// 支持三种操作：<br />
    /// - 跳过（Skip）：不下载，保留本地版本<br />
    /// - 替换（Replace）：删除本地版本，下载新版本<br />
    /// - 保留两者（KeepBoth）：两个版本都保留<br />
    /// </summary>
    public partial class ModConflictDialog : Window
    {
        /// <summary>
        /// 用户选择的冲突解决策略。默认为 Skip。
        /// </summary>
        public ModConflictResolution Result { get; private set; } = ModConflictResolution.Skip;

        public ModConflictDialog()
        {
            InitializeComponent();

            SkipButton.Click += OnSkip;
            ReplaceButton.Click += OnReplace;
            KeepBothButton.Click += OnKeepBoth;
        }

        /// <summary>
        /// 用冲突信息初始化对话框内容。
        /// </summary>
        /// <param name="conflict">冲突详情</param>
        public void SetConflictInfo(ModConflictInfo conflict)
        {
            var conflictTitleText = conflict.ConflictType switch
            {
                ModConflictType.SameModExists => "模组已存在（相同版本）",
                ModConflictType.HigherVersionExists => "本地已有更高版本",
                ModConflictType.LowerVersionExists => "本地已有更低版本（可升级）",
                _ => "模组冲突"
            };

            ConflictTitle.Text = conflictTitleText;
            ConflictDescription.Text = $"模组 \"{conflict.ModName}\"（ID: {conflict.ModId}）在本地已安装。";
            ExistingVersionText.Text = $"📁 本地版本: {conflict.ExistingVersion}";
            NewVersionText.Text = $"📦 待下载版本: {conflict.NewVersion}";
        }

        private void OnSkip(object? sender, RoutedEventArgs e)
        {
            Result = ModConflictResolution.Skip;
            Close(Result);
        }

        private void OnReplace(object? sender, RoutedEventArgs e)
        {
            Result = ModConflictResolution.Replace;
            Close(Result);
        }

        private void OnKeepBoth(object? sender, RoutedEventArgs e)
        {
            Result = ModConflictResolution.KeepBoth;
            Close(Result);
        }
    }
}