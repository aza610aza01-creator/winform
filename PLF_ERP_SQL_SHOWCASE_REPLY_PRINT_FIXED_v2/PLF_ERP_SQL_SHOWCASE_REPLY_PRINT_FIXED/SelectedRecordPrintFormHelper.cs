using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;

public static class SelectedRecordPrintFormHelper
{
    static readonly HashSet<string> HiddenColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "OrderId", "ProductId", "CustomerId", "SupplierId", "SupplierItemId",
        "RelatedOrderId", "AssignedUserId", "DeliveryNoteId", "StockId", "InvoiceId"
    };

    static readonly Dictionary<string, string[]> ModuleFieldOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DeliveryNotes"] = new[] { "DeliveryNoteNo", "OrderNo", "Warehouse", "DeliveryMethod", "DriverOrCourier", "Status", "DispatchDate", "FromAddress", "ToAddress", "RouteNotes" },
        ["ReplySlips"] = new[] { "ReplySlipNo", "DeliveryNoteNo", "CustomerName", "ContactPerson", "ResponseType", "SatisfactionRating", "FollowUpRequired", "ReceivedBy", "Status", "ReturnedDate", "Remarks", "SignatureRef" },
        ["Invoices"] = new[] { "InvoiceNo", "OrderNo", "CustomerName", "Amount", "Currency", "PaymentStatus", "InvoiceDate", "DueDate" },
        ["Payments"] = new[] { "PaymentNo", "InvoiceNo", "Amount", "PaymentMethod", "PaymentDate", "ReferenceNo" }
    };

    static readonly Dictionary<string, string> ModuleDocumentTitle = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DeliveryNotes"] = "Shipping / Delivery Note",
        ["ReplySlips"] = "Customer Reply Slip",
        ["Invoices"] = "Customer Invoice",
        ["Payments"] = "Payment Receipt"
    };

    public static void PrintSelectedRecordForm(Form owner, string moduleTitle, string tableName, DataGridView grid)
    {
        if (grid.CurrentRow == null)
        {
            MessageBox.Show("Please select one record first, then click Print Form.", "Print Form");
            return;
        }

        var fields = GetPrintableFields(grid.CurrentRow, tableName);
        if (fields.Count == 0)
        {
            MessageBox.Show("No printable fields found for the selected record.", "Print Form");
            return;
        }

        using var doc = new PrintDocument();
        string documentTitle = ModuleDocumentTitle.TryGetValue(tableName, out var customTitle) ? customTitle : moduleTitle + " Form";
        doc.DocumentName = documentTitle;
        doc.DefaultPageSettings.Landscape = false;
        doc.DefaultPageSettings.Margins = new Margins(45, 45, 45, 45);
        doc.PrintPage += (_, e) => DrawModernForm(e, documentTitle, tableName, fields);

        using var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 1100,
            Height = 800,
            StartPosition = FormStartPosition.CenterParent,
            Text = documentTitle + " Preview"
        };
        preview.ShowDialog(owner);
    }


    public static void PrintSelectedRecordForms(Form owner, string moduleTitle, string tableName, DataGridView grid, bool selectedOnly)
    {
        var rows = selectedOnly
            ? grid.SelectedRows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Reverse().ToList()
            : grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
        if (rows.Count == 0 && grid.CurrentRow != null && !grid.CurrentRow.IsNewRow) rows.Add(grid.CurrentRow);
        if (rows.Count == 0) { MessageBox.Show("Please select at least one record first.", "Print Form"); return; }
        var pages = rows.Select(r => GetPrintableFields(r, tableName)).Where(f => f.Count > 0).ToList();
        if (pages.Count == 0) { MessageBox.Show("No printable fields found for the selected record(s).", "Print Form"); return; }
        using var doc = new PrintDocument();
        string documentTitle = ModuleDocumentTitle.TryGetValue(tableName, out var customTitle) ? customTitle : moduleTitle + " Form";
        doc.DocumentName = documentTitle;
        doc.DefaultPageSettings.Landscape = false;
        doc.DefaultPageSettings.Margins = new Margins(45, 45, 45, 45);
        int pageIndex = 0;
        doc.PrintPage += (_, e) => { DrawModernForm(e, documentTitle, tableName, pages[pageIndex]); pageIndex++; e.HasMorePages = pageIndex < pages.Count; };
        using var preview = new PrintPreviewDialog { Document = doc, Width = 1100, Height = 800, StartPosition = FormStartPosition.CenterParent, Text = documentTitle + " Preview" };
        preview.ShowDialog(owner);
    }

    static List<(string Name, string Label, string Value)> GetPrintableFields(DataGridViewRow row, string tableName)
    {
        var byName = new Dictionary<string, (string Label, string Value)>(StringComparer.OrdinalIgnoreCase);

        foreach (DataGridViewCell cell in row.Cells)
        {
            if (cell.OwningColumn == null) continue;
            var column = cell.OwningColumn;
            if (!column.Visible && !ShouldForceShow(column.Name)) continue;
            if (HiddenColumns.Contains(column.Name)) continue;

            string label = string.IsNullOrWhiteSpace(column.HeaderText) ? Split(column.Name) : column.HeaderText;
            object? raw = cell.Value;
            string value = raw == null || raw == DBNull.Value ? "" : Convert.ToString(raw) ?? "";
            byName[column.Name] = (label, value);
        }

        var result = new List<(string Name, string Label, string Value)>();
        if (ModuleFieldOrder.TryGetValue(tableName, out var preferred))
        {
            foreach (string key in preferred)
            {
                var match = byName.FirstOrDefault(x =>
                    x.Key.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                    x.Value.Label.Replace(" ", "").Equals(key, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(match.Key))
                {
                    result.Add((match.Key, match.Value.Label, match.Value.Value));
                    byName.Remove(match.Key);
                }
            }
        }

        foreach (var item in byName)
            result.Add((item.Key, item.Value.Label, item.Value.Value));

        return result;
    }

    static bool ShouldForceShow(string columnName)
    {
        return columnName.Equals("Id", StringComparison.OrdinalIgnoreCase)
            || columnName.EndsWith("No", StringComparison.OrdinalIgnoreCase)
            || columnName.EndsWith("Code", StringComparison.OrdinalIgnoreCase)
            || columnName.Equals("SignatureRef", StringComparison.OrdinalIgnoreCase);
    }

    static void DrawModernForm(PrintPageEventArgs e, string documentTitle, string tableName, List<(string Name, string Label, string Value)> fields)
    {
        Graphics g = e.Graphics;
        Rectangle bounds = e.MarginBounds;
        int x = bounds.Left;
        int y = bounds.Top;
        int width = bounds.Width;

        Color primary = GetModuleColor(tableName);
        Color primaryDark = ControlPaint.Dark(primary);
        Color light = Color.FromArgb(248, 250, 252);
        Color fieldBg = Color.FromArgb(239, 246, 255);
        Color border = Color.FromArgb(203, 213, 225);
        Color dark = Color.FromArgb(15, 23, 42);
        Color muted = Color.FromArgb(100, 116, 139);

        using var titleFont = new Font("Segoe UI", 21, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 9);
        using var sectionFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 8, FontStyle.Bold);
        using var valueFont = new Font("Segoe UI", 10);
        using var smallFont = new Font("Segoe UI", 8);
        using var whiteBrush = new SolidBrush(Color.White);
        using var darkBrush = new SolidBrush(dark);
        using var mutedBrush = new SolidBrush(muted);
        using var primaryBrush = new SolidBrush(primary);
        using var primaryDarkBrush = new SolidBrush(primaryDark);
        using var lightBrush = new SolidBrush(light);
        using var fieldBrush = new SolidBrush(fieldBg);
        using var borderPen = new Pen(border, 1);

        var header = new Rectangle(x, y, width, 100);
        g.FillRectangle(primaryBrush, header);
        g.FillRectangle(primaryDarkBrush, new Rectangle(x, y, 14, 100));
        g.DrawString("Premium Living Furniture", titleFont, whiteBrush, x + 30, y + 16);
        g.DrawString(documentTitle, subtitleFont, whiteBrush, x + 32, y + 64);
        g.DrawString(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), subtitleFont, whiteBrush, x + width - 165, y + 64);
        y += 122;

        var key = GetPrimaryKey(fields, tableName);
        var status = GetValue(fields, "Status");
        var summary = new Rectangle(x, y, width, 58);
        g.FillRectangle(lightBrush, summary);
        g.DrawRectangle(borderPen, summary);
        g.DrawString("Record", labelFont, mutedBrush, x + 18, y + 10);
        using (var recordFont = new Font("Segoe UI", 13, FontStyle.Bold))
            g.DrawString(string.IsNullOrWhiteSpace(key) ? "Selected Record" : key, recordFont, darkBrush, x + 18, y + 28);
        DrawStatusBadge(g, status, x + width - 180, y + 17, 150, 26, smallFont, primaryBrush, whiteBrush);
        y += 86;

        if (tableName.Equals("DeliveryNotes", StringComparison.OrdinalIgnoreCase))
        {
            DrawDeliveryAddressSection(g, fields, x, ref y, width, labelFont, valueFont, sectionFont, mutedBrush, darkBrush, borderPen, lightBrush, fieldBrush);
        }

        g.DrawString("DETAILS", sectionFont, darkBrush, x, y);
        y += 32;

        var fieldsToDraw = fields
            .Where(f => !f.Name.Equals("Status", StringComparison.OrdinalIgnoreCase))
            .Where(f => !(tableName.Equals("DeliveryNotes", StringComparison.OrdinalIgnoreCase) && IsOneOf(f.Name, "FromAddress", "ToAddress")))
            .Where(f => !(tableName.Equals("ReplySlips", StringComparison.OrdinalIgnoreCase) && f.Name.Equals("SignatureRef", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        int colGap = 18;
        int colW = (width - colGap) / 2;
        int rowH = 56;
        int leftX = x;
        int rightX = x + colW + colGap;
        int startY = y;
        int printed = 0;

        foreach (var field in fieldsToDraw)
        {
            int colX = printed % 2 == 0 ? leftX : rightX;
            int rowY = startY + (printed / 2) * (rowH + 10);
            if (rowY + rowH > bounds.Bottom - 245)
            {
                g.DrawString("Additional fields are available in the ERP grid. This form prints the selected record summary only.", smallFont, mutedBrush, x, bounds.Bottom - 230);
                break;
            }

            DrawField(g, field.Label, HumanizeValue(field.Name, field.Value), colX, rowY, colW, rowH, labelFont, valueFont, mutedBrush, darkBrush, borderPen, fieldBrush);
            printed++;
        }

        // Reply Slip signature block is shown above the normal approval signatures.
        int bottomReserve = tableName.Equals("ReplySlips", StringComparison.OrdinalIgnoreCase) ? 220 : 126;
        int sigY = bounds.Bottom - 126;

        if (tableName.Equals("ReplySlips", StringComparison.OrdinalIgnoreCase))
        {
            int signatureY = bounds.Bottom - 236;
            DrawCustomerSignature(g, GetValue(fields, "SignatureRef"), x, signatureY, width, labelFont, smallFont, mutedBrush, darkBrush, borderPen, lightBrush);
        }

        int notesY = tableName.Equals("ReplySlips", StringComparison.OrdinalIgnoreCase) ? bounds.Bottom - 326 : sigY - 96;
        var notes = new Rectangle(x, notesY, width, 72);
        g.FillRectangle(lightBrush, notes);
        g.DrawRectangle(borderPen, notes);
        g.DrawString("Document Notes", labelFont, mutedBrush, notes.Left + 14, notes.Top + 10);
        g.DrawString(GetModuleNote(tableName), smallFont, mutedBrush, new RectangleF(notes.Left + 14, notes.Top + 31, notes.Width - 28, 32));

        int sigW = (width - 30) / 2;
        DrawSignatureBox(g, "Prepared By", x, sigY, sigW, labelFont, mutedBrush, borderPen);
        DrawSignatureBox(g, "Approved By", x + sigW + 30, sigY, sigW, labelFont, mutedBrush, borderPen);

        g.DrawString("Premium Living Furniture ERP · " + documentTitle, smallFont, mutedBrush, x, bounds.Bottom - 24);
        g.DrawString("Selected record", smallFont, mutedBrush, x + width - 95, bounds.Bottom - 24);
    }

    static void DrawDeliveryAddressSection(Graphics g, List<(string Name, string Label, string Value)> fields, int x, ref int y, int width, Font labelFont, Font valueFont, Font sectionFont, Brush mutedBrush, Brush darkBrush, Pen borderPen, Brush lightBrush, Brush fieldBrush)
    {
        string from = GetValue(fields, "FromAddress");
        string to = GetValue(fields, "ToAddress");

        g.DrawString("ADDRESS INFORMATION", sectionFont, darkBrush, x, y);
        y += 30;

        int gap = 18;
        int boxW = (width - gap) / 2;
        int boxH = 88;
        DrawMultilineBox(g, "From Address", from, x, y, boxW, boxH, labelFont, valueFont, mutedBrush, darkBrush, borderPen, fieldBrush);
        DrawMultilineBox(g, "To Address", to, x + boxW + gap, y, boxW, boxH, labelFont, valueFont, mutedBrush, darkBrush, borderPen, fieldBrush);
        y += boxH + 30;
    }

    static void DrawCustomerSignature(Graphics g, string signatureRef, int x, int y, int width, Font labelFont, Font smallFont, Brush mutedBrush, Brush darkBrush, Pen borderPen, Brush lightBrush)
    {
        var outer = new Rectangle(x, y, width, 82);
        g.FillRectangle(lightBrush, outer);
        g.DrawRectangle(borderPen, outer);
        g.DrawString("Customer Signature", labelFont, mutedBrush, outer.Left + 14, outer.Top + 10);

        var sigBox = new Rectangle(outer.Left + 150, outer.Top + 10, outer.Width - 170, outer.Height - 20);
        string fullPath = ResolvePath(signatureRef);
        if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
        {
            try
            {
                using var img = Image.FromFile(fullPath);
                Rectangle target = FitImage(img, sigBox, 4);
                g.DrawImage(img, target);
                return;
            }
            catch { }
        }

        string fallbackPath = ResolvePath("sig_customer_reply_slip.png");
        if (File.Exists(fallbackPath))
        {
            try
            {
                using var fallback = Image.FromFile(fallbackPath);
                Rectangle fallbackTarget = FitImage(fallback, sigBox, 4);
                g.DrawImage(fallback, fallbackTarget);
                return;
            }
            catch { }
        }
        g.DrawString(string.IsNullOrWhiteSpace(signatureRef) ? "No signature image" : "Signature image not found", smallFont, mutedBrush, sigBox);
    }

    static void DrawField(Graphics g, string label, string value, int x, int y, int w, int h, Font labelFont, Font valueFont, Brush mutedBrush, Brush darkBrush, Pen borderPen, Brush fillBrush)
    {
        var rect = new Rectangle(x, y, w, h);
        g.FillRectangle(fillBrush, rect);
        g.DrawRectangle(borderPen, rect);
        g.DrawString(label, labelFont, mutedBrush, x + 11, y + 7);
        string text = string.IsNullOrWhiteSpace(value) ? "-" : value;
        g.DrawString(text, valueFont, darkBrush, new RectangleF(x + 11, y + 25, w - 22, h - 27));
    }

    static void DrawMultilineBox(Graphics g, string label, string value, int x, int y, int w, int h, Font labelFont, Font valueFont, Brush mutedBrush, Brush darkBrush, Pen borderPen, Brush fillBrush)
    {
        var rect = new Rectangle(x, y, w, h);
        g.FillRectangle(fillBrush, rect);
        g.DrawRectangle(borderPen, rect);
        g.DrawString(label, labelFont, mutedBrush, x + 11, y + 7);
        string text = string.IsNullOrWhiteSpace(value) ? "-" : value;
        g.DrawString(text, valueFont, darkBrush, new RectangleF(x + 11, y + 27, w - 22, h - 32));
    }

    static void DrawStatusBadge(Graphics g, string status, int x, int y, int w, int h, Font font, Brush primaryBrush, Brush whiteBrush)
    {
        string text = string.IsNullOrWhiteSpace(status) ? "Selected" : status;
        g.FillRectangle(primaryBrush, new Rectangle(x, y, w, h));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, whiteBrush, new RectangleF(x, y, w, h), sf);
    }

    static void DrawSignatureBox(Graphics g, string label, int x, int y, int width, Font labelFont, Brush mutedBrush, Pen borderPen)
    {
        var box = new Rectangle(x, y, width, 75);
        g.DrawRectangle(borderPen, box);
        g.DrawString(label, labelFont, mutedBrush, x + 12, y + 10);
        g.DrawLine(borderPen, x + 12, y + 55, x + width - 12, y + 55);
    }

    static Color GetModuleColor(string tableName)
    {
        return tableName switch
        {
            "DeliveryNotes" => Color.FromArgb(30, 64, 175),
            "ReplySlips" => Color.FromArgb(5, 150, 105),
            "Invoices" => Color.FromArgb(124, 58, 237),
            "Payments" => Color.FromArgb(217, 119, 6),
            _ => Color.FromArgb(30, 64, 175)
        };
    }

    static string GetModuleNote(string tableName)
    {
        return tableName switch
        {
            "DeliveryNotes" => "Delivery form for warehouse dispatch, full address reference, route notes and delivery status confirmation.",
            "ReplySlips" => "Customer response form with displayed signature image, satisfaction feedback and follow-up tracking.",
            "Invoices" => "Invoice form showing customer billing information, payable amount, due date and payment status.",
            "Payments" => "Payment receipt form showing settlement details, method, amount and reference number.",
            _ => "This form prints the currently selected ERP record only. Select another row and click Print Form again to print another record."
        };
    }

    static string GetPrimaryKey(List<(string Name, string Label, string Value)> fields, string tableName)
    {
        string[] candidates = tableName switch
        {
            "DeliveryNotes" => new[] { "DeliveryNoteNo", "Id" },
            "ReplySlips" => new[] { "ReplySlipNo", "Id" },
            "Invoices" => new[] { "InvoiceNo", "Id" },
            "Payments" => new[] { "PaymentNo", "Id" },
            _ => new[] { "Id" }
        };
        foreach (string name in candidates)
        {
            string value = GetValue(fields, name);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return "";
    }

    static string GetValue(List<(string Name, string Label, string Value)> fields, string name)
    {
        var field = fields.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || f.Label.Replace(" ", "").Equals(name, StringComparison.OrdinalIgnoreCase));
        return field.Value ?? "";
    }

    static string HumanizeValue(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        if (name.Contains("Amount", StringComparison.OrdinalIgnoreCase) || name.Contains("Cost", StringComparison.OrdinalIgnoreCase) || name.Contains("Price", StringComparison.OrdinalIgnoreCase))
        {
            if (decimal.TryParse(value, out decimal d)) return d.ToString("N2");
        }
        if (DateTime.TryParse(value, out DateTime dt) && name.Contains("Date", StringComparison.OrdinalIgnoreCase)) return dt.ToString("yyyy-MM-dd");
        return value;
    }

    static bool IsOneOf(string value, params string[] options) => options.Any(o => value.Equals(o, StringComparison.OrdinalIgnoreCase));

    static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        string normalized = path.Replace("/", Path.DirectorySeparatorChar.ToString());
        if (Path.IsPathRooted(normalized)) return normalized;
        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, normalized),
            Path.Combine(Environment.CurrentDirectory, normalized),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages", normalized),
            Path.Combine(Environment.CurrentDirectory, "ProductImages", normalized)
        };
        foreach (string candidate in candidates)
            if (File.Exists(candidate)) return candidate;
        return candidates[0];
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

    static string Split(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
    }
}
