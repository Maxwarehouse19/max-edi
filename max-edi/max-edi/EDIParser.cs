using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace max_edi
{
    using InvoiceDict = Dictionary<string, List<Invoice>>;
    public class EDIParser
    {
        public IEnumerable<string> Files { get; set; }
        public string EDIPath { get; set; }
        public string EDIDonePath { get; set; }
        public int CountFiles { get; set; }

        Database db = new Database();

        public EDIParser(string ediPath, string filter)
        {
            this.EDIDonePath = ConfigurationManager.AppSettings["EDIDonePath"];
            InvoiceDict invoices = new InvoiceDict();
            this.EDIPath = ediPath;
            this.Files = Directory.EnumerateFiles(ediPath, filter);
            using (var progress = new ProgressBar())
            {
                int total = this.Files.Count();
                int i = 0;
                Logguer.Log(String.Format("Amount of EDI files: {0:n0}", total));
                HashSet<string> filesInDB = db.QueryFilesInTable("edi_invoice");

                db.Conn.Open();
                foreach (string file in this.Files)
                {
                    // Realiza conteo de archivos procesados
                    // -------------------------------------
                    CountFiles += 1;

                    string text = File.ReadAllText(file);
                    string filename = file.Substring(ediPath.Length + 1);

                    Invoice invoice = this.ParseFile(text, filename);
                    if (invoice != null)
                    {
                        if (!invoices.ContainsKey(invoice.PoNumber))
                        {
                            List<Invoice> ls = new List<Invoice>();
                            ls.Add(invoice);
                            invoices[invoice.PoNumber] = ls;
                        }
                        else
                        {
                            List<Invoice> ls = invoices[invoice.PoNumber];
                            ls.Add(invoice);
                            invoices[invoice.PoNumber] = ls;
                        }
                        if (!filesInDB.Contains(filename))
                        {
                            invoice.Insert(db);
                        }
                    }
                    this.copyToDoneOne(file);

                    i++;
                    progress.Report((double)i / total);
                }
                db.Conn.Close();
            }
        }

        public Invoice ParseFile(string text, string file)
        {
            text = text.Replace(System.Environment.NewLine, "");
            String[] lineSeparator = { "~" };
            String[] valueSeparator = { "*" };
            Int32 maxChunks = 8192;

            List<EDILine> ediFileLines = new List<EDILine> { };
            String[] strLines = text.Split(lineSeparator, maxChunks, StringSplitOptions.None);

            int lineNumber = 0;
            foreach (String line in strLines)
            {
                string[] values = line.Split(valueSeparator, maxChunks, StringSplitOptions.None);
                ediFileLines.Add(new EDILine { LineNumber = lineNumber, LineText = line, Type = values[0], Values = values });
                lineNumber++;
            }

            // It was tested that there is only one "BIG" line per file.
            // Event that, just in case, I created a list.
            List<EDILine> bigLines = ediFileLines.Where(line => line.Type == "BIG").ToList();
            List<EDILine> it1Lines = ediFileLines.Where(line => line.Type == "IT1").ToList();

            Invoice invoice = null;
            foreach (EDILine big in bigLines)
            {
                invoice =  new Invoice(big.Values, big.LineText);
                invoice.File = file;

                foreach (EDILine it1 in it1Lines)
                {
                    EDILine pid = ediFileLines[it1.LineNumber + 1];
                    if (big.LineNumber < it1.LineNumber)
                    {
                        InvoiceItem newItem = InvoiceItem.FromArrays(big.Values, it1.Values, pid.Values, it1.LineText);
                        newItem.File = file;
                        invoice.Items.Add(newItem);
                    }
                }
            }
            return invoice;
        }

        public void copyToDoneOne(string file)
        {
            string doneRelativePath = ConfigurationManager.AppSettings["DoneRelativePath"];
            string donePath = Path.Combine(this.EDIDonePath, doneRelativePath);

            Directory.CreateDirectory(donePath);
            string filename = file.Substring(this.EDIPath.Length + 1);
            string sourceFile = Path.Combine(this.EDIPath, filename);
            string targetFile = Path.Combine(donePath, filename);
            File.Move(sourceFile, targetFile, true);
        }
    }
}
