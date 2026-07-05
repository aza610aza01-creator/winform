using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;

public static class ProductPrintFormHelper
{
    public static void PrintProductForm(Form owner, DataGridView grid)
    {
        if (grid.CurrentRow == null)
        {
            MessageBox.Show("Please select a product row first.", "Print Product Form");
            return;
        }

        var row = grid.CurrentRow;
        string productCode = GetCell(row, "ProductCode");
        string productName = GetCell(row, "ProductName");
        string category = GetCell(row, "Category");
        string unitPrice = GetCell(row, "UnitPrice");
        string status = GetCell(row, "Status");
        string imagePath = GetCell(row, "ImagePath");

        using var doc = new PrintDocument();
        doc.DocumentName = string.IsNullOrWhiteSpace(productName) ? "Product Form" : "Product Form - " + productName;
        doc.DefaultPageSettings.Landscape = false;
        doc.DefaultPageSettings.Margins = new Margins(45, 45, 45, 45);
        doc.PrintPage += (_, e) => DrawProductPage(e, productCode, productName, category, unitPrice, status, imagePath);

        using var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 1050,
            Height = 780,
            StartPosition = FormStartPosition.CenterParent,
            Text = "Product Print Preview"
        };
        preview.ShowDialog(owner);
    }

    static void DrawProductPage(PrintPageEventArgs e, string code, string name, string category, string price, string status, string imagePath)
    {
        Graphics g = e.Graphics;
        Rectangle bounds = e.MarginBounds;
        int x = bounds.Left;
        int y = bounds.Top;
        int width = bounds.Width;

        using var titleFont = new Font("Segoe UI", 22, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 9);
        using var sectionFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var valueFont = new Font("Segoe UI", 11);
        using var smallFont = new Font("Segoe UI", 8);
        using var darkBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var mutedBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var whiteBrush = new SolidBrush(Color.White);
        using var primaryBrush = new SolidBrush(Color.FromArgb(37, 99, 235));
        using var softBlueBrush = new SolidBrush(Color.FromArgb(219, 234, 254));
        using var lightPanelBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
        using var borderPen = new Pen(Color.FromArgb(203, 213, 225), 1);
        using var primaryPen = new Pen(Color.FromArgb(37, 99, 235), 2);

        var header = new Rectangle(x, y, width, 95);
        g.FillRectangle(primaryBrush, header);
        g.DrawString("Premium Living Furniture", titleFont, whiteBrush, x + 24, y + 16);
        g.DrawString("Product Specification Form", subtitleFont, whiteBrush, x + 28, y + 62);
        g.DrawString(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), subtitleFont, whiteBrush, x + width - 165, y + 62);
        y += 120;

        var card = new Rectangle(x, y, width, 430);
        g.FillRectangle(whiteBrush, card);
        g.DrawRectangle(borderPen, card);

        int imageBoxW = 300;
        int imageBoxH = 270;
        var imageBox = new Rectangle(x + 24, y + 34, imageBoxW, imageBoxH);
        g.FillRectangle(lightPanelBrush, imageBox);
        g.DrawRectangle(primaryPen, imageBox);
        DrawProductImage(g, imagePath, imageBox, mutedBrush, smallFont);

        int infoX = imageBox.Right + 36;
        int infoY = y + 34;
        int infoW = width - imageBoxW - 84;

        g.DrawString("PRODUCT DETAILS", sectionFont, darkBrush, infoX, infoY);
        infoY += 36;
        DrawField(g, "Product Code", code, infoX, ref infoY, infoW, labelFont, valueFont, mutedBrush, darkBrush, borderPen, softBlueBrush);
        DrawField(g, "Product Name", name, infoX, ref infoY, infoW, labelFont, valueFont, mutedBrush, darkBrush, borderPen, softBlueBrush);
        DrawField(g, "Category", category, infoX, ref infoY, infoW, labelFont, valueFont, mutedBrush, darkBrush, borderPen, softBlueBrush);
        DrawField(g, "Unit Price", FormatPrice(price), infoX, ref infoY, infoW, labelFont, valueFont, mutedBrush, darkBrush, borderPen, softBlueBrush);
        DrawField(g, "Status", status, infoX, ref infoY, infoW, labelFont, valueFont, mutedBrush, darkBrush, borderPen, softBlueBrush);

        y = card.Bottom + 28;
        var notes = new Rectangle(x, y, width, 120);
        g.FillRectangle(lightPanelBrush, notes);
        g.DrawRectangle(borderPen, notes);
        g.DrawString("Notes", sectionFont, darkBrush, x + 20, y + 16);
        g.DrawString("This product form is generated from the ERP product catalogue. Please verify product image, pricing and status before external use.", smallFont, mutedBrush, new RectangleF(x + 20, y + 48, width - 40, 45));

        y = notes.Bottom + 30;
        int sigW = (width - 30) / 2;
        DrawSignatureBox(g, "Prepared By", x, y, sigW, labelFont, mutedBrush, borderPen);
        DrawSignatureBox(g, "Approved By", x + sigW + 30, y, sigW, labelFont, mutedBrush, borderPen);

        g.DrawString("Premium Living Furniture ERP · Product Form", smallFont, mutedBrush, x, bounds.Bottom - 24);
        g.DrawString("Page 1", smallFont, mutedBrush, x + width - 50, bounds.Bottom - 24);
    }

    static void DrawField(Graphics g, string label, string value, int x, ref int y, int width, Font labelFont, Font valueFont, Brush mutedBrush, Brush darkBrush, Pen borderPen, Brush bgBrush)
    {
        var fieldRect = new Rectangle(x, y, width, 48);
        g.FillRectangle(bgBrush, fieldRect);
        g.DrawRectangle(borderPen, fieldRect);
        g.DrawString(label, labelFont, mutedBrush, x + 12, y + 6);
        g.DrawString(string.IsNullOrWhiteSpace(value) ? "-" : value, valueFont, darkBrush, x + 12, y + 24);
        y += 58;
    }

    static void DrawSignatureBox(Graphics g, string label, int x, int y, int width, Font labelFont, Brush mutedBrush, Pen borderPen)
    {
        var box = new Rectangle(x, y, width, 75);
        g.DrawRectangle(borderPen, box);
        g.DrawString(label, labelFont, mutedBrush, x + 12, y + 10);
        g.DrawLine(borderPen, x + 12, y + 55, x + width - 12, y + 55);
    }

    static void DrawProductImage(Graphics g, string imagePath, Rectangle box, Brush mutedBrush, Font smallFont)
    {
        string fullPath = ResolveImagePath(imagePath);
        if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
        {
            try
            {
                using var img = Image.FromFile(fullPath);
                Rectangle target = FitImage(img, box, 12);
                g.DrawImage(img, target);
                return;
            }
            catch { }
        }
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(string.IsNullOrWhiteSpace(imagePath) ? "No product image" : "Image not found", smallFont, mutedBrush, box, sf);
    }

    static Rectangle FitImage(Image img, Rectangle box, int padding)
    {
        int maxW = box.Width - padding * 2;
        int maxH = box.Height - padding * 2;
        double ratio = Math.Min((double)maxW / img.Width, (double)maxH / img.Height);
        int w = Math.Max(1, (int)(img.Width * ratio));
        int h = Math.Max(1, (int)(img.Height * ratio));
        int x = box.Left + (box.Width - w) / 2;
        int y = box.Top + (box.Height - h) / 2;
        return new Rectangle(x, y, w, h);
    }

    static string ResolveImagePath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return "";
        if (Path.IsPathRooted(imagePath)) return imagePath;
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    static string GetCell(DataGridViewRow row, string column)
    {
        if (!row.DataGridView.Columns.Contains(column)) return "";
        object? value = row.Cells[column].Value;
        return value == null || value == DBNull.Value ? "" : Convert.ToString(value) ?? "";
    }

    static string FormatPrice(string price)
    {
        if (decimal.TryParse(price, out decimal p)) return p.ToString("N2");
        return string.IsNullOrWhiteSpace(price) ? "-" : price;
    }
}
