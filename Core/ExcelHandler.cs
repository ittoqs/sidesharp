using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace SIDESA.Net.Core;

public static class ExcelHandler
{
    public static List<string> ReadHeaders(string filePath)
    {
        var headers = new List<string>();
        if (!File.Exists(filePath)) return headers;

        try
        {
            using var wb = new XLWorkbook(filePath);
            var sheet = wb.Worksheets.First();
            var firstRow = sheet.Row(1);
            
            int i = 1;
            foreach (var cell in firstRow.CellsUsed())
            {
                var val = cell.Value.ToString();
                headers.Add(string.IsNullOrWhiteSpace(val) ? $"Column {i}" : val.Trim());
                i++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading headers: {ex.Message}");
        }
        return headers;
    }

    public static List<List<string>> PreviewData(string filePath, int limit = 10)
    {
        var rows = new List<List<string>>();
        if (!File.Exists(filePath)) return rows;

        try
        {
            using var wb = new XLWorkbook(filePath);
            var sheet = wb.Worksheets.First();
            
            int count = 0;
            foreach (var row in sheet.RowsUsed().Skip(1)) // Skip header
            {
                if (count >= limit) break;
                
                var rowData = new List<string>();
                foreach (var cell in row.Cells())
                {
                    rowData.Add(cell.Value.ToString());
                }
                rows.Add(rowData);
                count++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error previewing data: {ex.Message}");
        }
        return rows;
    }

    public static (int SuccessCount, int ErrorCount, List<string> Logs) ImportExcel(string filePath, Dictionary<string, string> columnMapping)
    {
        if (!File.Exists(filePath))
            return (0, 0, new List<string> { "Berkas Excel tidak ditemukan." });

        int successCount = 0;
        int errorCount = 0;
        var logs = new List<string>();

        try
        {
            using var wb = new XLWorkbook(filePath);
            var sheet = wb.Worksheets.First();
            
            var firstRow = sheet.Row(1);
            var headerIndices = new Dictionary<string, int>();
            
            int colIdx = 1;
            foreach (var cell in firstRow.CellsUsed())
            {
                headerIndices[cell.Value.ToString().Trim()] = colIdx;
                colIdx++;
            }

            if (!headerIndices.Any())
                return (0, 0, new List<string> { "Berkas Excel kosong." });

            var dbFields = new[] {
                "nik", "kk", "nama", "tempat_lahir", "tanggal_lahir", "jenis_kelamin", "agama",
                "status_perkawinan", "pekerjaan", "alamat", "rt", "rw", "desa", "kecamatan",
                "kabupaten", "provinsi", "kewarganegaraan"
            };

            int rowNum = 1;
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                rowNum++;
                if (row.IsEmpty()) continue;

                var residentData = new Database.Penduduk();

                foreach (var field in dbFields)
                {
                    if (columnMapping.TryGetValue(field, out var mappedHeader) && headerIndices.TryGetValue(mappedHeader, out int cIdx))
                    {
                        var cellVal = row.Cell(cIdx).Value;
                        string valStr = "";
                        
                        if (cellVal.IsDateTime)
                        {
                            valStr = cellVal.GetDateTime().ToString("yyyy-MM-dd");
                        }
                        else if (cellVal.IsNumber)
                        {
                            valStr = Math.Floor(cellVal.GetNumber()).ToString();
                        }
                        else
                        {
                            valStr = cellVal.ToString().Trim();
                        }

                        // Set property value dynamically or via switch
                        SetPropertyValue(residentData, field, valStr);
                    }
                }

                // Validate NIK
                var nik = residentData.Nik;
                if (string.IsNullOrWhiteSpace(nik))
                {
                    errorCount++;
                    logs.Add($"Baris {rowNum}: Gagal - NIK kosong.");
                    continue;
                }

                var nikCleaned = new string(nik.Where(char.IsDigit).ToArray());
                if (nikCleaned.Length != 16)
                {
                    errorCount++;
                    logs.Add($"Baris {rowNum}: Gagal - NIK '{nik}' tidak valid (harus 16 digit angka).");
                    continue;
                }
                residentData.Nik = nikCleaned;

                if (string.IsNullOrWhiteSpace(residentData.Kk)) residentData.Kk = "0000000000000000";
                if (string.IsNullOrWhiteSpace(residentData.Nama)) residentData.Nama = "TANPA NAMA";
                if (string.IsNullOrWhiteSpace(residentData.Kewarganegaraan)) residentData.Kewarganegaraan = "WNI";

                var result = Database.AddPenduduk(residentData);
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                    logs.Add($"Baris {rowNum} (NIK {nikCleaned}): Gagal - {result.Message}");
                }
            }

            return (successCount, errorCount, logs);
        }
        catch (Exception ex)
        {
            return (0, 0, new List<string> { $"Terjadi kesalahan sistem: {ex.Message}" });
        }
    }

    private static void SetPropertyValue(Database.Penduduk penduduk, string field, string value)
    {
        switch (field)
        {
            case "nik": penduduk.Nik = value; break;
            case "kk": penduduk.Kk = value; break;
            case "nama": penduduk.Nama = value; break;
            case "tempat_lahir": penduduk.Tempat_Lahir = value; break;
            case "tanggal_lahir": penduduk.Tanggal_Lahir = value; break;
            case "jenis_kelamin": penduduk.Jenis_Kelamin = value; break;
            case "agama": penduduk.Agama = value; break;
            case "status_perkawinan": penduduk.Status_Perkawinan = value; break;
            case "pekerjaan": penduduk.Pekerjaan = value; break;
            case "alamat": penduduk.Alamat = value; break;
            case "rt": penduduk.Rt = value; break;
            case "rw": penduduk.Rw = value; break;
            case "desa": penduduk.Desa = value; break;
            case "kecamatan": penduduk.Kecamatan = value; break;
            case "kabupaten": penduduk.Kabupaten = value; break;
            case "provinsi": penduduk.Provinsi = value; break;
            case "kewarganegaraan": penduduk.Kewarganegaraan = value; break;
        }
    }

    public static (bool Success, string Message) GenerateTemplate(string outputPath)
    {
        try
        {
            string templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "xls", "TemplateDataPenduduk.xlsx");

            if (!File.Exists(templatePath))
            {
                return (false, "File template sumber (templates/xls/TemplateDataPenduduk.xlsx) tidak ditemukan.");
            }

            File.Copy(templatePath, outputPath, overwrite: true);
            return (true, "Template berhasil dibuat.");
        }
        catch (Exception ex)
        {
            return (false, $"Gagal menyalin template: {ex.Message}");
        }
    }
}
