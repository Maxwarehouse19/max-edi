using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace max_edi
{
    public class Invoice
    {
        // Auto-Initialized properties 
        public string File { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string PoNumber { get; set; }
        public string DocType { get; set; }
        public List<InvoiceItem> Items { get; set; }

        public Invoice(string[] bigValues, string line)
        {
            try
            {
                this.InvoiceNumber = bigValues[2];
                this.Date = DateTime.ParseExact(bigValues[1], "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture);
                this.PoNumber = bigValues[4];
                this.DocType = bigValues[7];
                this.Items = new List<InvoiceItem> { };
            }
            catch (Exception ex)
            {
                Logguer.Log("Line: " + line);
                Logguer.Log("Exception: " + ex.ToString());
            }
        }

        public void Insert(Database db)
        {
            foreach (InvoiceItem item in Items)
            {
                db.Insert(String.Format("insert into dbo.edi_invoice values('{0}','{1}','{2}','{3:yyyy-MM-dd}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');",
                    this.File, this.PoNumber, this.InvoiceNumber, this.Date, this.DocType,
                    item.Quantity, item.UnitPrice, Math.Round(item.Quantity * item.UnitPrice,2), item.Description.Replace("\'",""), item.Sku, item.MeasureUnit));
            }
        }

        public void CheckInsertDB(Database db)
        {
            SqlCommand command = new SqlCommand("SELECT COUNT(*) from dbo.edi_invoice where filename like @file", db.Conn);
            command.Parameters.AddWithValue("@file", this.File);
            int exist = db.EscalarQuery(command);
            if (exist == 0)
            {
                this.Insert(db);
            }
        }
    }
}