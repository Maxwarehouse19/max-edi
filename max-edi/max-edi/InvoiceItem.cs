using System;

namespace max_edi
{
    public class InvoiceItem
    {
        public string File { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string PoNumber { get; set; }
        public string LineNumber { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public string MeasureUnit { get; set; }
        public double UnitPrice { get; set; }
        public string Description { get; set; }

        public string ItemDetails()
        {
            return string.Format("{0} is an author of {1}. Price: ${2}",
                Sku, Quantity, MeasureUnit);
        }

        public static InvoiceItem FromArrays(string[] bigValues, string[] it1Values, string[] pidValues, string line)
        {
            InvoiceItem item = new InvoiceItem();
            try
            {
                item.InvoiceNumber = bigValues[2];
                item.Date = DateTime.ParseExact(bigValues[1], "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture);
                item.PoNumber = bigValues[4];
                item.LineNumber = it1Values[1];
                item.Sku = it1Values[7];
                item.MeasureUnit = it1Values[3];
                item.Quantity = Convert.ToInt32(it1Values[2]);
                item.UnitPrice = 0;
                if (it1Values[4] != "") {
                    item.UnitPrice = Convert.ToDouble(it1Values[4]);
                }
                item.Description = pidValues[5];
                return item;
            }
            catch (Exception ex)
            {
                Logguer.Log("Line: " + line);
                Logguer.Log("Exception: " + ex.ToString());
                return item;
            }
        }
    }
}
