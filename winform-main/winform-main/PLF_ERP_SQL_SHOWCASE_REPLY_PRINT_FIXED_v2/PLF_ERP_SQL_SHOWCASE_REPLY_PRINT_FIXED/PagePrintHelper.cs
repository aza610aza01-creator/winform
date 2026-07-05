using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;
public static class PagePrintHelper
{
    public static void PrintGridContent(Form owner, string title, DataGridView grid)
    {
        var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
        if (rows.Count == 0) { MessageBox.Show(owner, "There is no data to print.", "Print Content"); return; }
        PrintRows(owner, title + " - Content", grid, rows, true);
    }
    public static void PrintSelectedRows(Form owner, string title, DataGridView grid)
    {
        var rows = grid.SelectedRows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Reverse().ToList();
        if (rows.Count == 0 && grid.CurrentRow != null && !grid.CurrentRow.IsNewRow) rows.Add(grid.CurrentRow);
        if (rows.Count == 0) { MessageBox.Show(owner, "Please select at least one row to print.", "Print Selected Rows"); return; }
        PrintRows(owner, title + " - Selected Rows", grid, rows, false);
    }
    static void PrintRows(Form owner, string title, DataGridView grid, System.Collections.Generic.List<DataGridViewRow> rows, bool compact)
    {
        var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && c.Name != "ImagePath").OrderBy(c => c.DisplayIndex).ToList();
        int rowIndex = 0;
        var doc = new PrintDocument { DocumentName = title };
        doc.DefaultPageSettings.Landscape = true;
        doc.PrintPage += (_, e) =>
        {
            int y = e.MarginBounds.Top;
            using var titleFont = new Font("Segoe UI", 15, FontStyle.Bold);
            using var headFont = new Font("Segoe UI", 8, FontStyle.Bold);
            using var cellFont = new Font("Segoe UI", 8);
            e.Graphics.DrawString(title, titleFont, Brushes.Black, e.MarginBounds.Left, y);
            y += 40;
            while (rowIndex < rows.Count)
            {
                var row = rows[rowIndex];
                int h = compact ? 118 : 150;
                if (y + h > e.MarginBounds.Bottom) { e.HasMorePages = true; return; }
                var card = new Rectangle(e.MarginBounds.Left, y, e.MarginBounds.Width, h - 10);
                e.Graphics.DrawRectangle(Pens.Gray, card);
                int x = card.Left + 10, textX = x;
                Image? picture = FindRowImage(row, grid);
                if (picture != null)
                {
                    var imgRect = new Rectangle(x, y + 14, compact ? 120 : 150, compact ? 76 : 95);
                    e.Graphics.DrawRectangle(Pens.LightGray, imgRect);
                    e.Graphics.DrawImage(picture, Fit(picture, imgRect));
                    textX += imgRect.Width + 15;
                }
                int lineY = y + 10, colCount = 0;
                foreach (var col in cols)
                {
                    if (col is DataGridViewImageColumn || col.Name == "ProductPicture" || col.Name == "ProductImage") continue;
                    string value = Convert.ToString(row.Cells[col.Name].Value) ?? "";
                    if (value.Length > 70) value = value[..70] + "...";
                    e.Graphics.DrawString(col.HeaderText + ":", headFont, Brushes.Black, textX, lineY);
                    e.Graphics.DrawString(value, cellFont, Brushes.Black, textX + 125, lineY);
                    lineY += 17; colCount++;
                    if (lineY > y + h - 25 || colCount > (compact ? 5 : 7)) break;
                }
                y += h; rowIndex++;
            }
            e.HasMorePages = false;
        };
        using var preview = new PrintPreviewDialog { Document = doc, Width = 1200, Height = 850, Text = "Print Preview - " + title };
        preview.ShowDialog(owner);
    }
    static Image? FindRowImage(DataGridViewRow row, DataGridView grid)
    {
        foreach (DataGridViewColumn c in grid.Columns)
            if (c is DataGridViewImageColumn && row.Cells[c.Name].Value is Image img) return img;
        if (grid.Columns.Contains("ImagePath")) return SalesOrderForm.LoadProductThumbnail(Convert.ToString(row.Cells["ImagePath"].Value) ?? "", 180, 110);
        return null;
    }
    static Rectangle Fit(Image img, Rectangle box)
    {
        double ratio = Math.Min((double)box.Width / img.Width, (double)box.Height / img.Height);
        int w = Math.Max(1, (int)(img.Width * ratio)), h = Math.Max(1, (int)(img.Height * ratio));
        return new Rectangle(box.Left + (box.Width - w) / 2, box.Top + (box.Height - h) / 2, w, h);
    }
}
