using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using System.Configuration;

namespace max_edi
{
    public class ExcelGenerator
    {
        public Findings Findings { get; set; }
        public string ExcelFindingsFilePath { get; set; }

        public ExcelGenerator(Findings findings, string excelFindingsFilePath)
        {
            this.Findings = findings;
            this.ExcelFindingsFilePath = excelFindingsFilePath;
        }

        public void GenerateGrouped()
        {
            List<ReportData> report = this.Findings.Report;
            string excelSheetName = ConfigurationManager.AppSettings["ExcelSheetName"];

            if (report.Count > 0)
            {
                FileInfo file = new FileInfo(this.ExcelFindingsFilePath);
                using (var package = new ExcelPackage())
                {
                    // Add a new worksheet to the empty workbook
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(excelSheetName);
                    //Add the headers
                    worksheet.Cells[1, 1].Value = "PO";
                    worksheet.Cells[1, 2].Value = "Status";
                    worksheet.Cells[1, 3].Value = "Findings";
                    worksheet.Cells[1, 4].Value = "EDI Date";
                    worksheet.Cells[1, 5].Value = "Etail Date";
                    worksheet.Cells[1, 6].Value = "SKU";
                    worksheet.Cells[1, 7].Value = "EDI Measurement";
                    worksheet.Cells[1, 8].Value = "Etail Measurement";
                    worksheet.Cells[1, 9].Value = "Etail UOM Quantity";
                    worksheet.Cells[1, 10].Value = "EDI Qty";
                    worksheet.Cells[1, 11].Value = "Etail Received";
                    worksheet.Cells[1, 12].Value = "Etail Remaining";
                    worksheet.Cells[1, 13].Value = "Etail Qty";
                    worksheet.Cells[1, 14].Value = "EDI Unit Price";
                    worksheet.Cells[1, 15].Value = "Etail Unit Price";
                    worksheet.Cells[1, 16].Value = "EDI Amount";
                    worksheet.Cells[1, 17].Value = "Etail Real Amount";
                    worksheet.Cells[1, 18].Value = "Difference";
                    worksheet.Cells[1, 19].Value = "Difference %";
                    worksheet.Cells[1, 20].Value = "EDI Files";


                    int i = 2;
                    foreach (ReportData data in report)
                    {
                        worksheet.Cells[i, 1].Value = data.PoNumber;
                        worksheet.Cells[i, 2].Value = data.Status;
                        worksheet.Cells[i, 3].Value = data.Finding;
                        worksheet.Cells[i, 4].Value = data.EDIDate;
                        worksheet.Cells[i, 5].Value = data.PODate;
                        worksheet.Cells[i, 6].Value = data.SKU;
                        worksheet.Cells[i, 7].Value = data.EDIMeasureUnit;
                        worksheet.Cells[i, 8].Value = data.EtailMeasureUnit;
                        worksheet.Cells[i, 9].Value = data.EtailUOMQty;
                        worksheet.Cells[i, 10].Value = data.EDIQty;
                        worksheet.Cells[i, 11].Value = data.EtailReceived;
                        worksheet.Cells[i, 12].Value = data.EtailRemaining;
                        worksheet.Cells[i, 13].Value = data.EtailQty;
                        worksheet.Cells[i, 14].Value = data.EDIUnitPrice;
                        worksheet.Cells[i, 15].Value = data.EtailUnitPrice;
                        worksheet.Cells[i, 16].Value = data.EDITotal;
                        worksheet.Cells[i, 17].Value = data.PORealTotal;
                        worksheet.Cells[i, 18].Value = data.Diff;
                        worksheet.Cells[i, 19].Value = data.Diff_pct;
                        worksheet.Cells[i, 20].Value = data.File;
                        i++;
                    }
                    i--;
                    //int FromRow, int FromCol, int ToRow, int ToCol
                    worksheet.Cells[1, 1, 1, 19].Style.Font.Bold = true;
                    worksheet.Cells[2, 4, i, 5].Style.Numberformat.Format = "dd/mm/yyyy";
                    worksheet.Cells[2, 9, i, 13].Style.Numberformat.Format = "_-* #,##0_-;-* #,##0_-;_-* \" - \" ?? _-;_-@_-";
                    worksheet.Cells[2, 14, i, 18].Style.Numberformat.Format = "_-* #,##0.00_-;-* #,##0.00_-;_-* \" - \" ?? _-;_-@_-";
                    worksheet.Cells[2, 19, i, 19].Style.Numberformat.Format = "0.0 %";
                    worksheet.Cells.AutoFitColumns(0);

                    package.Workbook.Properties.Title = "FindingsReport";
                    package.Workbook.Properties.Author = "Luis Galvez";
                    package.SaveAs(file);
                    return;
                }
            }
            Logguer.Log("There were found ZERO inconsistencies.");
        }
    }
}
