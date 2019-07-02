using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using System.Drawing;

namespace EPPlusInAction
{
	// This is the Second Release of the Article:
	// http://www.codeproject.com/Articles/680421/Create-Read-Edit-Advance-Excel-2007-2010-Report-in
	// Sample2 shows you the following:
	/*
	 * 1. Open Sample 1 and Add two new rows
	 * 2. After adding new rows, a Pie Chart will be add based on the value
	 */
	class Sample2
	{
		public static string RunSample2(DirectoryInfo outputDir)
		{
			// Taking the file produced by the Sample 1 i.e. 'Sample1.xlsx'. Here 'Sample1.xlsx' is treated as template file
			FileInfo templateFile = new FileInfo(outputDir.FullName + @"\Sample1.xlsx");
			// Making a new file 'Sample2.xlsx'
			FileInfo newFile = new FileInfo(outputDir.FullName + @"\Sample2.xlsx");

			// If there is any file having same name as 'Sample2.xlsx', then delete it first
			if (newFile.Exists)
			{
				newFile.Delete();
				newFile = new FileInfo(outputDir.FullName + @"\Sample2.xlsx");
			}

			using (ExcelPackage package = new ExcelPackage(newFile, templateFile))
			{
				// Openning first Worksheet of the template file i.e. 'Sample1.xlsx'
				ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
				// I'm adding 5th & 6th rows as 1st to 4th rows are filled up with values in 'Sample1.xlsx'
				worksheet.InsertRow(5, 2);

				// Inserting values in the 5th row
				worksheet.Cells["A5"].Value = "12010";
				worksheet.Cells["B5"].Value = "Drill";
				worksheet.Cells["C5"].Value = 20;
				worksheet.Cells["D5"].Value = 8;

				// Inserting values in the 6th row
				worksheet.Cells["A6"].Value = "12011";
				worksheet.Cells["B6"].Value = "Crowbar";
				worksheet.Cells["C6"].Value = 7;
				worksheet.Cells["D6"].Value = 23.48;

				// Adding formula for the 'E' column i.e. 'Value' column
				// Now see how you write R1C1 formula
				// About FORMULA R1C1: http://msdn.microsoft.com/en-us/library/office/bb213527%28v=office.12%29.aspx
				worksheet.Cells["E2:E6"].FormulaR1C1 = "RC[-2]*RC[-1]";

				// Now adding SUBTOTAL() function as was in Sample 1
				// But here you'll see how to add 'Named Range'
				var name = worksheet.Names.Add("SubTotalName", worksheet.Cells["C7:E7"]);
				name.Formula = "SUBTOTAL(9, C2:C6)";

				// Formatting newly added rows i.e. Row 5th and 6th
				worksheet.Cells["C5:C6"].Style.Numberformat.Format = "#,##0";
				worksheet.Cells["D5:E6"].Style.Numberformat.Format = "#,##0.00";

				// Now we are going to create the Pie Chart
				// Read about Pie Chart: http://office.microsoft.com/en-in/excel-help/present-your-data-in-a-pie-chart-HA010211848.aspx
				var chart = (worksheet.Drawings.AddChart("PieChart", OfficeOpenXml.Drawing.Chart.eChartType.Pie3D) as ExcelPieChart);

				// Setting title text of the Chart
				chart.Title.Text = "Total";

				// Setting 5 pixel offset from 5th column of the first row
				chart.SetPosition(0, 0, 5, 5);

				// Setting width & height of the chart area
				chart.SetSize(600, 300);

				 //In the Pie Chart value will come from 'Value' column
				 //and show in the form of percentage
				 //Now I'll show you how to take values from the 'Value' column
				ExcelAddress valueAddress = new ExcelAddress(2, 5, 6, 5);
				// Setting Series and XSeries of the chart
				// Product name will be the item of the Chart
				var ser = (chart.Series.Add(valueAddress.Address, "B2:B6") as ExcelPieChartSerie);
				
				// Setting chart properties
				chart.DataLabel.ShowCategory = true;
				chart.DataLabel.ShowPercent = true;
				// Formatting Looks of the Chart
				chart.Legend.Border.LineStyle = eLineStyle.Solid;
				chart.Legend.Border.Fill.Style = eFillStyle.SolidFill;
				chart.Legend.Border.Fill.Color = Color.DarkBlue;

				// Switch the page layout view back to the normal
				worksheet.View.PageLayoutView = false;

				// Save our new workbook, we are done
				package.Save();
			}

			return newFile.FullName;
		}
	}
}
