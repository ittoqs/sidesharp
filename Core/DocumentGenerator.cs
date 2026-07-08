using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SIDESA.Net.Core;

public static class DocumentGenerator
{
    private static readonly string TemplateDir = "templates";

    public static void BootstrapTemplates()
    {
        // Simple bootstrap checking
        if (!Directory.Exists(TemplateDir))
        {
            Directory.CreateDirectory(TemplateDir);
        }
        
        // In C#, generating complex docx from scratch with OpenXml is verbose.
        // For migration purposes, if the templates from the Python app exist in the same directory,
        // we can just copy them, or rely on the user having run the Python app once.
        // We will assume the templates "Domisili.docx", "SKTM.docx", etc. are placed in "templates/".
    }

    private static void ReplaceTextInElement(OpenXmlElement element, Dictionary<string, string> placeholders)
    {
        foreach (var textElement in element.Descendants<Text>())
        {
            var text = textElement.Text;
            bool changed = false;
            foreach (var kvp in placeholders)
            {
                var tag = $"{{{{{kvp.Key}}}}}";
                if (text.Contains(tag))
                {
                    text = text.Replace(tag, kvp.Value);
                    changed = true;
                }
            }
            if (changed)
            {
                textElement.Text = text;
            }
        }
    }

    public static (string? OutputPath, string? ErrorMessage) GenerateDocument(string templateFilename, Database.Penduduk resident, string letterNumber)
    {
        var templatePath = Path.Combine(TemplateDir, templateFilename);
        if (!File.Exists(templatePath))
        {
            return (null, $"Berkas template '{templateFilename}' tidak ditemukan.");
        }

        var config = Database.LoadConfig();
        var instansi = config.TryGetValue("instansi", out var inst) ? inst : default;
        var signatory = config.TryGetValue("penandatangan", out var sign) ? sign : default;
        var storage = config.TryGetValue("penyimpanan", out var stor) ? stor : default;

        string docxOutputDir = "output/docx";
        if (storage.ValueKind == JsonValueKind.Object && storage.TryGetProperty("folder_docx", out var fDocx))
        {
            docxOutputDir = fDocx.GetString() ?? docxOutputDir;
        }

        if (!Directory.Exists(docxOutputDir)) Directory.CreateDirectory(docxOutputDir);

        var today = DateTime.Today.ToString("dd-MM-yyyy");

        // Helper to get nested json string
        string GetConfigStr(JsonElement el, string key, string def = "")
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(key, out var prop))
                return prop.GetString() ?? def;
            return def;
        }

        var placeholders = new Dictionary<string, string>
        {
            {"DESA", GetConfigStr(instansi, "desa", "Kampokku Jaya")},
            {"KECAMATAN", GetConfigStr(instansi, "kecamatan", "Pakkampong")},
            {"KABUPATEN", GetConfigStr(instansi, "kabupaten", "Limpo")},
            {"PROVINSI", GetConfigStr(instansi, "provinsi", "Limpo Toddang")},
            {"ALAMAT_INSTANSI", GetConfigStr(instansi, "alamat", "")},
            {"TELEPON_INSTANSI", GetConfigStr(instansi, "telepon", "")},
            {"NOMOR_SURAT", letterNumber},
            {"TANGGAL_SEKARANG", today},
            {"NAMA_KADES", GetConfigStr(signatory, "nama", "")},
            {"NIP_KADES", GetConfigStr(signatory, "nip", "")},
            {"JABATAN_KADES", GetConfigStr(signatory, "jabatan", "Kepala Desa")},
            {"NIK", resident.Nik},
            {"KK", resident.Kk},
            {"NAMA", resident.Nama},
            {"TEMPAT_LAHIR", resident.Tempat_Lahir},
            {"TANGGAL_LAHIR", resident.Tanggal_Lahir},
            {"JENIS_KELAMIN", resident.Jenis_Kelamin},
            {"AGAMA", resident.Agama},
            {"STATUS_PERKAWINAN", resident.Status_Perkawinan},
            {"PEKERJAAN", resident.Pekerjaan},
            {"ALAMAT", resident.Alamat},
            {"RT", resident.Rt},
            {"RW", resident.Rw},
            {"KEWARGANEGARAAN", resident.Kewarganegaraan}
        };

        try
        {
            var cleanNumber = letterNumber.Replace("/", "_").Replace("\\", "_");
            var outputFilename = $"{Path.GetFileNameWithoutExtension(templateFilename)}_{resident.Nik}_{cleanNumber}.docx";
            var outputPath = Path.Combine(docxOutputDir, outputFilename);

            File.Copy(templatePath, outputPath, true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                if (doc.MainDocumentPart?.Document != null)
                {
                    ReplaceTextInElement(doc.MainDocumentPart.Document, placeholders);
                    doc.MainDocumentPart.Document.Save();
                }
            }

            return (outputPath, null);
        }
        catch (Exception ex)
        {
            return (null, $"Gagal membuat dokumen Word: {ex.Message}");
        }
    }

    public static (string? OutputPath, string? ErrorMessage) ConvertToPdf(string docxPath, string libreofficePath = "")
    {
        var config = Database.LoadConfig();
        var storage = config.TryGetValue("penyimpanan", out var stor) ? stor : default;
        
        string pdfOutputDir = "output/pdf";
        if (storage.ValueKind == JsonValueKind.Object && storage.TryGetProperty("folder_pdf", out var fPdf))
        {
            pdfOutputDir = fPdf.GetString() ?? pdfOutputDir;
        }

        if (!Directory.Exists(pdfOutputDir)) Directory.CreateDirectory(pdfOutputDir);

        var outputPdfPath = Path.Combine(pdfOutputDir, Path.GetFileNameWithoutExtension(docxPath) + ".pdf");

        // Try LibreOffice Headless via Process
        string sofficeExec = "soffice";
        if (!string.IsNullOrEmpty(libreofficePath) && File.Exists(libreofficePath))
        {
            sofficeExec = libreofficePath;
        }
        else if (OperatingSystem.IsWindows())
        {
            var paths = new[] {
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
            };
            foreach (var p in paths)
            {
                if (File.Exists(p)) { sofficeExec = p; break; }
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            sofficeExec = "/Applications/LibreOffice.app/Contents/MacOS/soffice";
        }
        else if (OperatingSystem.IsLinux())
        {
            sofficeExec = "libreoffice";
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = sofficeExec,
                    Arguments = $"--headless --convert-to pdf --outdir \"{pdfOutputDir}\" \"{docxPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            process.WaitForExit(30000); // 30 seconds timeout

            if (File.Exists(outputPdfPath))
            {
                return (outputPdfPath, null);
            }
            else
            {
                var err = process.StandardError.ReadToEnd();
                return (null, $"Konversi PDF gagal. Pastikan LibreOffice terinstal.\nDetail: {err}");
            }
        }
        catch (Exception ex)
        {
            return (null, $"Kesalahan konversi PDF: {ex.Message}");
        }
    }
}
