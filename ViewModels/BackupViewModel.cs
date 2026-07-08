using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SIDESA.Net.ViewModels;

public partial class BackupViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "";

    public void ExecuteBackup(string destinationFolder)
    {
        try
        {
            var dbSource = Path.Combine(Directory.GetCurrentDirectory(), "database", "surat.db");
            var configSource = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

            if (File.Exists(dbSource))
            {
                var dbDest = Path.Combine(destinationFolder, $"surat_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(dbSource, dbDest, true);
            }

            if (File.Exists(configSource))
            {
                var configDest = Path.Combine(destinationFolder, $"config_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.Copy(configSource, configDest, true);
            }

            StatusMessage = $"Backup berhasil disimpan di: {destinationFolder}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saat backup: {ex.Message}";
        }
    }
}
