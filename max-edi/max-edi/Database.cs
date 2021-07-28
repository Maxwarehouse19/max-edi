using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Data.SqlClient;

namespace max_edi
{
    public class Database
    {
        public SqlConnection Conn;
        public Database()
        {
            string conn_str = ConfigurationManager.AppSettings["ConectionString"];
            this.Conn = new SqlConnection(conn_str);
        }
        public int EscalarQuery(SqlCommand command)
        {
            try
            {
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    int count = (Int32)result;
                    return count;
                }
                return 0;
            }
            catch (Exception e)
            {
                Logguer.Log("Exception Occre while creating table:" + e.Message + "\t" + e.GetType());
                return 0;
            }
        }

        public HashSet<string> QueryFilesInTable(string table)
        {
            string query = String.Format("select filename from [MaxWarehouse].[dbo].[{0}] group by filename", table);
            HashSet<string> files = new HashSet<string>();
            SqlCommand command = new SqlCommand(query, this.Conn);
            this.Conn.Open();
            SqlDataReader reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    files.Add(reader["filename"].ToString());
                }
            }
            finally
            {
                reader.Close();
                this.Conn.Close();
            }
            return files;
        }

        public List<ReportData> QueryFindings()
        {
            // obtiene la ultima fecha cargada en el sistema
            // ---------------------------------------------
            string querydate = @"(select max(date) date from received_items_po)";
            SqlCommand commanddate = new SqlCommand(querydate, this.Conn);
            commanddate.CommandTimeout = 600;
            this.Conn.Open();
            SqlDataReader readerdate = commanddate.ExecuteReader();
            string Fechaproceso = "";
            while (readerdate.Read())
            {
                Fechaproceso = readerdate["date"].ToString();
            }

            readerdate.Close();
            this.Conn.Close();

            string query = @"
            select * from (
                select 
				edi.po_number
	                , po.status
	                , case 
		                when po.po_number is null then 'NOT FOUND ON ETAIL'
		                when po.status = 'Cancelled' and edi.total != 0 then 'CANCELLED WITH INVOICE'
                        when Cantidad > 1 then 'DUPLICATE PO NUMBER'
		                when (po.quantity * po.uom_quantity != edi.quantity  and po.rev_quantity* po.unit_price != edi.total ) then CASE WHEN   edi.quantity < po.quantity * po.uom_quantity THEN 'PARTIAL FULFILLED - SHORTAGE' ELSE 'PARTIAL FULFILLED - OVERAGE' END 
		                --when (edi.quantity != (po.rev_quantity * po.uom_quantity) /*se agrega la unidad de medida EAVD*/ and po.rev_quantity* po.unit_price != edi.total ) then 'UNMATCHED QUANTITY'
		                when abs((po.unit_price*po.quantity  - (edi.unit_price * edi.quantity /*se agrega la unidad de medida EAVD*/)) / (edi.unit_price  * edi.quantity /*se agrega la unidad de medida EAVD*/)) >= 0.03 then 'PRICE DIFFERENCE'
		                when po.rev_quantity = po.quantity and edi.quantity = po.quantity and po.remaining < 0 then 'NEGATIVE REMAINING'
		                when po.sku = 'Deleted' then 'DELETED SKU ON ETAIL'
		                else ''
		                end as finding
	                , format(edi.date, 'yyy-MM-dd') as edi_date
	                , format(po.date, 'yyy-MM-dd') as po_date
	                , edi.sku
				    , edi.measure_unit edi_measure_unit
				    , po.measure_unit po_measure_unit
				    , po.uom_quantity po_uom_quantity
	                , edi.quantity edi_qty
	                , po.received po_received
	                , po.remaining po_remaining
	                , po.quantity po_qty
	                , edi.unit_price edi_unit_price
	                , po.unit_price po_unit_price
	                , edi.total as edi_total
	                , po.real_total as po_real_total
	                , round(abs(edi.total - po.real_total) / po.real_total,3) as diff_pct
	                , round(edi.total - po.real_total, 2) as diff
	                , files.filename
                from (
	                select po_number
		                , sku
		                , date
					    , measure_unit
		                , sum(quantity) quantity
		                , avg(unit_price) unit_price
		                , round(sum(quantity * unit_price),2) total
                        , COUNT(*) Cantidad
	                from [MaxWarehouse].[dbo].edi_invoice
	                where doc_type = 'DI'
	                and lower(po_number) != 'christmas'
	                and sku != '0' ";

            string EsFinMes = ConfigurationManager.AppSettings["FinMes"];
            
            if (EsFinMes == "Y")
            {
                query = query + @" and date >= substring(cast(cast(DATEADD(MM, -1,GETUTCDATE()) As Date) as char),1,8)+'01' ";
            }
            else
            {
                query = query + @" and date >= dateadd(day,-5, cast(GETUTCDATE() As Date)) ";
            }
            query = query + @" group by po_number, sku, date, measure_unit
					
                ) edi
                left outer join (
	                select po_number
		                , status
		                , sku
		                , date
					    , measure_unit
		                , max(uom_quantity) uom_quantity
		                , sum(received) received
		                , sum(remaining) remaining
						, sum(received + remaining) rev_quantity
		                , sum(quantity) quantity
		                , avg(unit_price) unit_price
		                , round(sum(quantity * unit_price),2) total
		                , round(sum((received + remaining) * unit_price),2) real_total
	                from [MaxWarehouse].[dbo].received_items_po
	                where sku != '0'
	                group by po_number, status, sku, date, measure_unit
                ) po on (edi.po_number = po.po_number and (edi.sku = po.sku or 'Deleted' = po.sku))
                left outer join (
	                select edi.po_number,
	                left(edi.filenames, Len(edi.filenames)-1) as filename
	                from
	                (
		                select distinct edi2.po_number, 
			                (
				                select edi1.filename + ', ' AS [text()]
				                from (
					                select po_number, filename
					                from [MaxWarehouse].[dbo].edi_invoice
					                group by po_number, filename
				                ) edi1
				                where edi1.po_number = edi2.po_number
				                order by edi1.po_number
				                for xml path ('')
			                ) [filenames]
		                from [MaxWarehouse].[dbo].edi_invoice edi2 
	                ) edi
                ) files on (edi.po_number = files.po_number)
                where ( edi.quantity != (po.rev_quantity * po.uom_quantity) /*se agrega la unidad de medida EAVD*/
				or (edi.quantity = (po.rev_quantity * po.uom_quantity) and (po.quantity * po.uom_quantity != edi.quantity))
				or (edi.quantity = (po.rev_quantity * po.uom_quantity) and abs((po.unit_price  - (edi.unit_price * po.uom_quantity /*se agrega la unidad de medida EAVD*/)) / edi.unit_price) >= 0.03)
                or (po.po_number is null and edi.date < dateadd(day,-1,cast(GETUTCDATE() As Date)))
			    or (po.status = 'Cancelled' and edi.total != 0))
            ) a
            where finding != '' ";

            if (EsFinMes == "Y")
            {
                query = query + @" order by edi_date, po_number";
            }
            else
            {
                query = query + @" and edi_date = " + "format(cast('" + Fechaproceso + "' as datetime), 'yyy-MM-dd') " + "order by edi_date, po_number";
            }

            SqlCommand command = new SqlCommand(query, this.Conn);
            command.CommandTimeout = 600;
            this.Conn.Open();
            SqlDataReader reader = command.ExecuteReader();
            List<ReportData> report = new List<ReportData>();
            try
            {
                while (reader.Read())
                {
                    ReportData data = new ReportData();
                    data.PoNumber = reader["po_number"].ToString();
                    data.Finding = reader["finding"].ToString();
                    data.EDIDate = DateTime.ParseExact(reader["edi_date"].ToString(), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture);
                    data.SKU = reader["sku"].ToString();
                    data.EDIMeasureUnit = reader["edi_measure_unit"].ToString();
                    data.EDIQty = (int)reader["edi_qty"];
                    data.EDIUnitPrice = (double)reader["edi_unit_price"];
                    data.EDITotal = (double)reader["edi_total"];
                    data.File = reader["filename"].ToString();
                    if (reader["status"].ToString().Length != 0)
                    {
                        data.Status = reader["status"].ToString();
                        data.PODate = DateTime.ParseExact(reader["po_date"].ToString(), "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture);
                        data.EtailMeasureUnit = reader["po_measure_unit"].ToString();
                        data.EtailReceived = (int)reader["po_received"];
                        data.EtailRemaining = (int)reader["po_remaining"];
                        data.EtailQty = (int)reader["po_qty"];
                        data.EtailUOMQty = (int)reader["po_uom_quantity"];
                        data.EtailUnitPrice = (double)reader["po_unit_price"];
                        data.PORealTotal = (double)reader["po_real_total"];
                        data.Diff = (double)reader["diff"];
                        data.Diff_pct = (double)reader["diff_pct"];
                    }
                    report.Add(data);
                }
            }
            catch (Exception e)
            {
                Logguer.Log("Exception while creating ReportData:" + e.Message + "\t" + e.GetType());
            }
            finally
            {
                reader.Close();
                this.Conn.Close();
            }
            return report;
        }

        public void Insert(string command)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(command, this.Conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logguer.Log(command);
                Logguer.Log("Exception Occre while creating table:" + e.Message + "\t" + e.GetType());

            }
        }
    }
}
