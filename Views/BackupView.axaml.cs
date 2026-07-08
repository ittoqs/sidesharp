using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SIDESA.Net.ViewModels;

namespace SIDESA.Net.Views;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();
    }

    private async void BtnPilihFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Pilih Folder Tujuan Backup",
                AllowMultiple = false
            });

            if (result != null && result.Count > 0 && DataContext is BackupViewModel vm)
            {
                vm.ExecuteBackup(result[0].Path.LocalPath);
            }
        }
    }
}
