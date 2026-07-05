using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;

public static class PrintFormHelper
{
    const int SignatureBoxWidth = 420;
    const int SignatureBoxHeight = 165;

    public static void PrintGrid(Form owner, string title, DataGridView grid)
    {
        if (grid.Rows.Count == 0)
        {
            MessageBox.Show(owner, "There is no visible data to print yet.", "Print Form");
            return;
        }

        using var doc = IsReplySlip(title, grid) ? ReplySlipDocument(title, grid)
            : IsDeliveryReport(title, grid) ? DeliveryDocument(title, grid)
            : IsCustomerInvoice(title, grid) ? CustomerInvoiceDocument(title, grid)
            : IsPaymentRecord(title, grid) ? PaymentDocument(title, grid)
            : GridDocument(title, grid);

        using var preview = new PrintPreviewDialog { Document = doc, Width = 1200, Height = 850, Text = "Print Preview - " + title };
        preview.ShowDialog(owner);
    }

    static bool IsReplySlip(string title, DataGridView grid) => title.Contains("Reply", StringComparison.OrdinalIgnoreCase) && grid.Columns.Contains("SignatureRef");
    static bool IsDeliveryReport(string title, DataGridView grid) => (title.Contains("Delivery", StringComparison.OrdinalIgnoreCase) || title.Contains("Shipping", StringComparison.OrdinalIgnoreCase)) && grid.Columns.Contains("FromAddress") && grid.Columns.Contains("ToAddress");
    static bool IsCustomerInvoice(string title, DataGridView grid) => title.Contains("Invoice", StringComparison.OrdinalIgnoreCase) && grid.Columns.Contains("InvoiceNo") && grid.Columns.Contains("Amount");
    static bool IsPaymentRecord(string title, DataGridView grid) => title.Contains("Payment", StringComparison.OrdinalIgnoreCase) && grid.Columns.Contains("PaymentNo");

    // ==========================================
    // 專屬列印面板：客戶發票 (Customer Invoice)
    // 只會在 Customer Invoices 模組使用，不影響其他 Print Form。
    // ==========================================
    static PrintDocument CustomerInvoiceDocument(string title, DataGridView grid)
    {
        int index = 0;
        var doc = new PrintDocument { DocumentName = title + " Customer Invoice" };
        doc.DefaultPageSettings.Margins = new Margins(55, 55, 45, 45);
        doc.DefaultPageSettings.Landscape = false;
        doc.BeginPrint += (_, _) => index = 0;
        doc.PrintPage += (_, e) =>
        {
            var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            if (index >= rows.Count) { e.HasMorePages = false; return; }

            var row = rows[index];
            var g = e.Graphics!;
            var b = e.MarginBounds;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var companyFont = new Font("Segoe UI", 20, FontStyle.Bold);
            using var invoiceTitleFont = new Font("Segoe UI", 12, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 9, FontStyle.Bold);
            using var textFont = new Font("Segoe UI", 9);
            using var tableHeaderFont = new Font("Segoe UI", 9, FontStyle.Bold);
            using var balanceFont = new Font("Segoe UI", 12, FontStyle.Bold);
            using var footerFont = new Font("Segoe UI", 8, FontStyle.Italic);
            using var darkBlue = new SolidBrush(Color.Navy);
            using var darkText = new SolidBrush(Color.Black);
            using var greyText = new SolidBrush(Color.DimGray);
            using var redText = new SolidBrush(Color.Red);
            using var headerBack = new SolidBrush(Color.FromArgb(238, 238, 238));
            using var linePen = new Pen(Color.LightGray);
            using var borderPen = new Pen(Color.Gray);

            string invoiceNo = Cell(row, "InvoiceNo");
            string orderId = Cell(row, "OrderId");
            string customerId = Cell(row, "CustomerId");
            string amountRaw = Cell(row, "Amount");
            string status = Cell(row, "PaymentStatus");
            string invoiceDate = CleanDate(Cell(row, "InvoiceDate"));
            string dueDate = CleanDate(Cell(row, "DueDate"));
            string currency = string.IsNullOrWhiteSpace(Cell(row, "Currency")) ? "HKD" : Cell(row, "Currency");
            decimal amount = decimal.TryParse(amountRaw, out var parsedAmount) ? parsedAmount : 0m;
            string money = amount > 0 ? currency + " $" + amount.ToString("0.00") : currency + " $" + amountRaw;

            int y = b.Top + 25;
            int left = b.Left + 15;
            int right = b.Right - 15;
            int width = right - left;

            g.DrawString("Premium Living Furniture", companyFont, darkBlue, left, y);
            g.DrawString("CUSTOMER INVOICE", invoiceTitleFont, greyText, right - 185, y + 9);
            y += 42;
            g.DrawLine(linePen, left, y, right, y);
            y += 18;

            g.DrawString("Invoice No : " + invoiceNo, labelFont, darkText, left, y);
            g.DrawString("Date : " + invoiceDate, textFont, darkText, left + 405, y);
            y += 21;
            g.DrawString("Order Ref : #" + orderId, textFont, darkText, left, y);
            g.DrawString("Due Date : " + dueDate, textFont, darkText, left + 405, y);
            y += 21;
            g.DrawString("Customer ID: " + customerId, textFont, darkText, left, y);
            y += 42;

            int tableX = left;
            int tableW = width;
            int descW = (int)(tableW * 0.735);
            int amountW = tableW - descW;
            int headerH = 36;
            int rowH = 68;

            var headerRect = new Rectangle(tableX, y, tableW, headerH);
            g.FillRectangle(headerBack, headerRect);
            g.DrawRectangle(borderPen, headerRect);
            g.DrawString("Description / Transaction Summary", tableHeaderFont, darkText, tableX + 14, y + 11);
            g.DrawString("Total Amount", tableHeaderFont, darkText, tableX + descW + 14, y + 11);
            y += headerH;

            var bodyRect = new Rectangle(tableX, y, tableW, rowH);
            g.DrawRectangle(borderPen, bodyRect);
            g.DrawString("Furniture ERP Sales Transaction Summary (" + invoiceNo + ")", textFont, darkText, new RectangleF(tableX + 14, y + 19, descW - 28, rowH - 22));
            g.DrawString(money, textFont, darkText, tableX + descW + 14, y + 19);
            y += rowH + 22;

            g.DrawString("Payment Status : " + status, labelFont, redText, left, y);
            g.DrawString("Total Balance Due: " + money, balanceFont, darkBlue, right - 275, y - 2);
            y += 44;

            g.DrawLine(linePen, left, y, right, y);
            y += 18;
            g.DrawString("Thank you for choosing Premium Living Furniture. For invoices inquiries, please contact finance@premiumliving.com", footerFont, greyText, left, y);

            index++;
            e.HasMorePages = index < rows.Count;
        };
        return doc;
    }

    // ==========================================
    // 專屬列印面板：付款收據 (Payment Receipt)
    // ==========================================
    static PrintDocument PaymentDocument(string title, DataGridView grid)
    {
        int index = 0;
        var doc = new PrintDocument { DocumentName = title + " Receipt" };
        doc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
        doc.DefaultPageSettings.Landscape = false;

        doc.PrintPage += (s, e) =>
        {
            var g = e.Graphics!;
            var row = grid.Rows[index];

            using var fontTitle = new Font("Segoe UI", 24, FontStyle.Bold);
            using var fontSubTitle = new Font("Segoe UI", 16, FontStyle.Bold);
            using var fontHeader = new Font("Segoe UI", 11, FontStyle.Bold);
            using var fontNormal = new Font("Segoe UI", 11);
            using var fontMuted = new Font("Segoe UI", 9, FontStyle.Italic);
            using var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42));
            using var brushMuted = new SolidBrush(Color.DimGray);

            int y = 60;
            int marginL = 60;
            int marginR = e.PageBounds.Width - 60;
            int width = marginR - marginL;

            g.DrawString("Premium Living Furniture", fontTitle, brushDark, marginL, y);
            y += 45;
            g.DrawString("OFFICIAL RECEIPT", fontSubTitle, brushMuted, marginL, y);
            y += 40;
            g.DrawLine(Pens.LightGray, marginL, y, marginR, y);
            y += 30;

            string payNo = Cell(row, "PaymentNo");
            string invId = Cell(row, "InvoiceId");
            string amt = Cell(row, "Amount");
            string method = Cell(row, "PaymentMethod");
            string payDate = CleanDate(Cell(row, "PaymentDate"));
            string refNo = Cell(row, "ReferenceNo");

            g.DrawString("Receipt No:", fontHeader, brushDark, marginL, y);
            g.DrawString(payNo, fontNormal, brushDark, marginL + 120, y);

            g.DrawString("Date:", fontHeader, brushDark, marginR - 220, y);
            g.DrawString(payDate, fontNormal, brushDark, marginR - 120, y);
            y += 30;
            g.DrawString("Invoice Ref:", fontHeader, brushDark, marginL, y);
            g.DrawString(invId, fontNormal, brushDark, marginL + 120, y);
            y += 30;
            g.DrawString("Reference No:", fontHeader, brushDark, marginL, y);
            g.DrawString(string.IsNullOrWhiteSpace(refNo) ? "N/A" : refNo, fontNormal, brushDark, marginL + 120, y);
            y += 50;

            Rectangle boxRect = new Rectangle(marginL, y, width, 140);
            g.FillRectangle(Brushes.WhiteSmoke, boxRect);
            g.DrawRectangle(Pens.Gray, boxRect);

            y += 20;
            g.DrawString("Payment Details", fontHeader, brushMuted, marginL + 20, y);
            y += 35;
            g.DrawString("Payment Method:", fontHeader, brushDark, marginL + 20, y);
            g.DrawString(method, fontNormal, brushDark, marginL + 160, y);
            y += 40;
            g.DrawString("Amount Paid:", fontSubTitle, brushDark, marginL + 20, y);

            string amtStr = decimal.TryParse(amt, out decimal parsedAmt) ? $"HKD ${parsedAmt:N2}" : $"HKD ${amt}";
            g.DrawString(amtStr, new Font("Segoe UI", 20, FontStyle.Bold), Brushes.DarkGreen, marginL + 180, y - 4);
            y += 90;

            g.DrawLine(Pens.LightGray, marginL, y, marginR, y);
            y += 20;
            g.DrawString("Thank you for your payment. This receipt is automatically generated and does not require a signature.", fontMuted, brushMuted, marginL, y);

            index++;
            e.HasMorePages = index < grid.Rows.Count;
        };
        return doc;
    }

    // ==========================================
    // 專屬列印面板：顧客回條 (Reply Slips) - 已修改為一頁一張
    // ==========================================
    static PrintDocument ReplySlipDocument(string title, DataGridView grid)
    {
        int index = 0;
        var doc = new PrintDocument { DocumentName = title + " Reply Slips" };
        doc.DefaultPageSettings.Margins = new Margins(50, 50, 45, 45);
        doc.BeginPrint += (_, _) => index = 0;
        doc.PrintPage += (_, e) =>
        {
            var g = e.Graphics!;
            var b = e.MarginBounds;
            using var titleFont = new Font("Segoe UI", 17, FontStyle.Bold);
            using var sectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 9, FontStyle.Bold);
            using var textFont = new Font("Segoe UI", 9);
            using var pen = new Pen(Color.FromArgb(200, 200, 200));
            using var brush = new SolidBrush(Color.Black);

            // 尋找下一筆有效的資料列（跳過新空白列）
            DataGridViewRow? row = null;
            while (index < grid.Rows.Count)
            {
                var r = grid.Rows[index++];
                if (!r.IsNewRow)
                {
                    row = r;
                    break;
                }
            }

            // 如果已經沒有有效資料，結束列印
            if (row == null)
            {
                e.HasMorePages = false;
                return;
            }

            int y = b.Top;
            g.DrawString("Customer Reply Slip", titleFont, brush, b.Left, y); y += 34;
            g.DrawLine(pen, b.Left, y, b.Right, y); y += 14;

            Field(g, "Reply Slip No", Cell(row, "ReplySlipNo"), b.Left, y, labelFont, textFont);
            Field(g, "Delivery Note", Cell(row, "DeliveryNoteNo"), b.Left + 285, y, labelFont, textFont); y += 27;
            Field(g, "Customer", Cell(row, "CustomerName"), b.Left, y, labelFont, textFont);
            Field(g, "Contact Person", Cell(row, "ContactPerson"), b.Left + 285, y, labelFont, textFont); y += 27;
            Field(g, "Response Type", Cell(row, "ResponseType"), b.Left, y, labelFont, textFont);
            Field(g, "Satisfaction", Cell(row, "SatisfactionRating"), b.Left + 285, y, labelFont, textFont); y += 27;
            Field(g, "Follow Up", Cell(row, "FollowUpRequired"), b.Left, y, labelFont, textFont);
            Field(g, "Status", Cell(row, "Status"), b.Left + 285, y, labelFont, textFont); y += 27;
            Field(g, "Returned Date", CleanDate(Cell(row, "ReturnedDate")), b.Left, y, labelFont, textFont);
            Field(g, "Received By", Cell(row, "ReceivedBy"), b.Left + 285, y, labelFont, textFont); y += 34;

            g.DrawString("Remarks", sectionFont, brush, b.Left, y); y += 22;
            g.DrawString(Cell(row, "Remarks"), textFont, brush, new RectangleF(b.Left, y, b.Width, 45)); y += 58;

            g.DrawString("Customer Signature", sectionFont, brush, b.Left, y); y += 24;
            var box = new Rectangle(b.Left, y, SignatureBoxWidth, SignatureBoxHeight);
            g.DrawRectangle(pen, box);
            DrawSignature(g, Cell(row, "SignatureRef"), box, textFont, brush);

            // 檢查後面是否還有其他「非新列」的有效資料需要列印
            bool hasMore = false;
            for (int i = index; i < grid.Rows.Count; i++)
            {
                if (!grid.Rows[i].IsNewRow)
                {
                    hasMore = true;
                    break;
                }
            }

            // 設定是否要渲染下一頁
            e.HasMorePages = hasMore;
        };
        return doc;
    }

    // ==========================================
    // 專屬列印面板：配送報告 (Delivery Manifest)
    // ==========================================
    static PrintDocument DeliveryDocument(string title, DataGridView grid)
    {
        int index = 0, pageNo = 0;
        var doc = new PrintDocument { DocumentName = title + " Manifest" };
        doc.DefaultPageSettings.Landscape = false;
        doc.DefaultPageSettings.Margins = new Margins(55, 55, 50, 55);
        doc.BeginPrint += (_, _) => { index = 0; pageNo = 0; };
        doc.PrintPage += (_, e) =>
        {
            var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            if (index >= rows.Count) { e.HasMorePages = false; return; }
            pageNo++;
            var row = rows[index];
            var g = e.Graphics!;
            var b = e.MarginBounds;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var titleFont = new Font("Segoe UI", 20, FontStyle.Bold);
            using var noteFont = new Font("Segoe UI", 14, FontStyle.Bold);
            using var sectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 8, FontStyle.Bold);
            using var textFont = new Font("Segoe UI", 10);
            using var smallFont = new Font("Segoe UI", 8);
            using var pen = new Pen(Color.FromArgb(190, 202, 218));
            using var strongPen = new Pen(Color.FromArgb(71, 85, 105));
            using var blueBrush = new SolidBrush(Color.FromArgb(37, 99, 235));
            using var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
            using var textBrush = new SolidBrush(Color.Black);
            using var mutedBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
            using var headerBrush = new SolidBrush(Color.FromArgb(219, 234, 254));
            using var lightBrush = new SolidBrush(Color.FromArgb(248, 250, 252));

            int y = b.Top;
            g.DrawString("Shipping / Delivery Manifest", titleFont, titleBrush, b.Left, y);
            g.DrawString("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "   Page " + pageNo, smallFont, mutedBrush, b.Right - 240, y + 14);
            y += 50;
            g.DrawLine(strongPen, b.Left, y, b.Right, y);
            y += 22;

            var header = new Rectangle(b.Left, y, b.Width, 48);
            g.FillRectangle(headerBrush, header);
            g.DrawRectangle(pen, header);
            g.DrawString("Delivery Note: " + Cell(row, "DeliveryNoteNo"), noteFont, blueBrush, header.Left + 14, header.Top + 13);
            g.DrawString("Status: " + Cell(row, "Status"), sectionFont, titleBrush, header.Right - 210, header.Top + 16);
            y += 65;

            int gap = 14;
            int half = (b.Width - gap) / 2;
            DrawFieldBox(g, "Warehouse", Cell(row, "Warehouse"), b.Left, y, half, 48, labelFont, textFont, textBrush, mutedBrush, lightBrush, pen);
            DrawFieldBox(g, "Dispatch Date", CleanDate(Cell(row, "DispatchDate")), b.Left + half + gap, y, half, 48, labelFont, textFont, textBrush, mutedBrush, lightBrush, pen);
            y += 62;
            DrawFieldBox(g, "Delivery Method", Cell(row, "DeliveryMethod"), b.Left, y, half, 48, labelFont, textFont, textBrush, mutedBrush, lightBrush, pen);
            DrawFieldBox(g, "Driver / Courier", Cell(row, "DriverOrCourier"), b.Left + half + gap, y, half, 48, labelFont, textFont, textBrush, mutedBrush, lightBrush, pen);
            y += 70;

            g.DrawString("Route Addresses", sectionFont, titleBrush, b.Left, y);
            y += 24;
            DrawLargeBox(g, "From Address", Cell(row, "FromAddress"), b.Left, y, b.Width, 82, labelFont, textFont, textBrush, mutedBrush, pen);
            y += 96;
            DrawLargeBox(g, "To Address", Cell(row, "ToAddress"), b.Left, y, b.Width, 82, labelFont, textFont, textBrush, mutedBrush, pen);
            y += 100;
            g.DrawString("Route Notes", sectionFont, titleBrush, b.Left, y);
            y += 24;
            DrawLargeBox(g, "Notes", Cell(row, "RouteNotes"), b.Left, y, b.Width, 78, labelFont, textFont, textBrush, mutedBrush, pen);

            g.DrawLine(pen, b.Left, b.Bottom - 28, b.Right, b.Bottom - 28);
            g.DrawString("Printed from Premium Living Furniture ERP", smallFont, mutedBrush, b.Left, b.Bottom - 22);
            g.DrawString((index + 1) + " / " + rows.Count, smallFont, mutedBrush, b.Right - 55, b.Bottom - 22);

            index++;
            e.HasMorePages = index < rows.Count;
        };
        return doc;
    }

    // ==========================================
    // 通用資料表列印 (Grid Report)
    // ==========================================
    static PrintDocument GridDocument(string title, DataGridView grid)
    {
        int rowIndex = 0, pageNo = 0;
        var doc = new PrintDocument { DocumentName = title + " Report" };
        doc.DefaultPageSettings.Landscape = true;
        doc.DefaultPageSettings.Margins = new Margins(35, 35, 35, 45);
        doc.BeginPrint += (_, _) => { rowIndex = 0; pageNo = 0; };
        doc.PrintPage += (_, e) =>
        {
            pageNo++;
            var g = e.Graphics!;
            var b = e.MarginBounds;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var titleFont = new Font("Segoe UI", 18, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 8);
            using var headFont = new Font("Segoe UI", 8, FontStyle.Bold);
            using var cellFont = new Font("Segoe UI", 8);
            using var pen = new Pen(Color.FromArgb(210, 210, 210));
            using var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
            using var textBrush = new SolidBrush(Color.Black);
            using var mutedBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
            using var headerBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
            using var altBrush = new SolidBrush(Color.FromArgb(248, 250, 252));

            int y = b.Top;
            g.DrawString(title, titleFont, titleBrush, b.Left, y);
            g.DrawString("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "   Page " + pageNo, subFont, mutedBrush, b.Right - 230, y + 8);
            y += 40;
            var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
            var widths = CalculateColumnWidths(g, grid, cols, b.Width, headFont, cellFont);
            int x = b.Left, headerHeight = 32;
            for (int i = 0; i < cols.Count; i++) { var rect = new Rectangle(x, y, widths[i], headerHeight); g.FillRectangle(headerBrush, rect); g.DrawRectangle(pen, rect); DrawWrapped(g, cols[i].HeaderText, headFont, textBrush, rect, StringAlignment.Near, StringAlignment.Center); x += widths[i]; }
            y += headerHeight;
            while (rowIndex < grid.Rows.Count)
            {
                var row = grid.Rows[rowIndex];
                if (row.IsNewRow) { rowIndex++; continue; }
                int rowHeight = CalculateRowHeight(g, row, cols, widths, cellFont, 30, 92);
                if (y + rowHeight > b.Bottom - 20) { e.HasMorePages = true; return; }
                x = b.Left;
                for (int i = 0; i < cols.Count; i++) { var rect = new Rectangle(x, y, widths[i], rowHeight); if (rowIndex % 2 == 0) g.FillRectangle(altBrush, rect); g.DrawRectangle(pen, rect); DrawWrapped(g, Cell(row, cols[i].Name), cellFont, textBrush, rect, StringAlignment.Near, StringAlignment.Near); x += widths[i]; }
                y += rowHeight;
                rowIndex++;
            }
            e.HasMorePages = false;
        };
        return doc;
    }

    // ==========================================
    // 底層排版與數據輔助工具方法
    // ==========================================
    static void DrawFieldBox(Graphics g, string label, string value, int x, int y, int w, int h, Font lf, Font tf, Brush text, Brush muted, Brush bg, Pen pen) { var rect = new Rectangle(x, y, w, h); g.FillRectangle(bg, rect); g.DrawRectangle(pen, rect); g.DrawString(label, lf, muted, x + 9, y + 6); DrawWrapped(g, value, tf, text, new Rectangle(x + 9, y + 23, w - 18, h - 27), StringAlignment.Near, StringAlignment.Near); }
    static void DrawLargeBox(Graphics g, string label, string value, int x, int y, int w, int h, Font lf, Font tf, Brush text, Brush muted, Pen pen) { var rect = new Rectangle(x, y, w, h); g.DrawRectangle(pen, rect); g.DrawString(label, lf, muted, x + 10, y + 7); DrawWrapped(g, value, tf, text, new Rectangle(x + 10, y + 28, w - 20, h - 34), StringAlignment.Near, StringAlignment.Near); }
    static List<int> CalculateColumnWidths(Graphics g, DataGridView grid, List<DataGridViewColumn> cols, int totalWidth, Font headFont, Font cellFont) { var weights = new List<float>(); foreach (var col in cols) { float max = g.MeasureString(col.HeaderText, headFont).Width + 18; foreach (DataGridViewRow row in grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Take(30)) max = Math.Max(max, Math.Min(220, g.MeasureString(Cell(row, col.Name), cellFont).Width + 18)); if (col.Name.Contains("Address") || col.Name.Contains("Notes")) max *= 1.45f; weights.Add(Math.Max(55, max)); } float sum = weights.Sum(); var widths = weights.Select(w => Math.Max(45, (int)(totalWidth * w / sum))).ToList(); int diff = totalWidth - widths.Sum(); if (widths.Count > 0) widths[widths.Count - 1] += diff; return widths; }
    static int CalculateRowHeight(Graphics g, DataGridViewRow row, List<DataGridViewColumn> cols, List<int> widths, Font font, int min, int max) { int h = min; for (int i = 0; i < cols.Count; i++) { var size = g.MeasureString(Cell(row, cols[i].Name), font, Math.Max(20, widths[i] - 10)); h = Math.Max(h, (int)size.Height + 14); } return Math.Min(max, h); }
    static void DrawWrapped(Graphics g, string text, Font font, Brush brush, Rectangle rect, StringAlignment horizontal, StringAlignment vertical) { using var sf = new StringFormat { Alignment = horizontal, LineAlignment = vertical, Trimming = StringTrimming.EllipsisWord, FormatFlags = 0 }; g.DrawString(text ?? "", font, brush, rect, sf); }
    static void DrawSignature(Graphics g, string reference, Rectangle box, Font font, Brush brush) { string path = ResolveSignaturePath(reference); if (!File.Exists(path)) { g.DrawString("Signature image not found: " + reference, font, brush, new RectangleF(box.X + 8, box.Y + 8, box.Width - 16, box.Height - 16)); return; } using var img = Image.FromFile(path); var target = FitImageInsideBox(img, box, 12); g.DrawImage(img, target); }
    static Rectangle FitImageInsideBox(Image img, Rectangle box, int pad) { var inner = new Rectangle(box.X + pad, box.Y + pad, box.Width - pad * 2, box.Height - pad * 2); double ratio = Math.Min((double)inner.Width / img.Width, (double)inner.Height / img.Height); int w = Math.Max(1, (int)(img.Width * ratio)), h = Math.Max(1, (int)(img.Height * ratio)); return new Rectangle(inner.X + (inner.Width - w) / 2, inner.Y + (inner.Height - h) / 2, w, h); }
    static string ResolveSignaturePath(string reference) { if (string.IsNullOrWhiteSpace(reference)) return ""; if (File.Exists(reference)) return reference; string baseDir = AppDomain.CurrentDomain.BaseDirectory; string direct = Path.Combine(baseDir, reference); if (File.Exists(direct)) return direct; return Path.Combine(baseDir, "Signatures", Path.GetFileName(reference)); }
    static string Cell(DataGridViewRow row, string col) => row.DataGridView?.Columns.Contains(col) == true ? Convert.ToString(row.Cells[col].Value) ?? "" : "";
    static string CleanDate(string value) => DateTime.TryParse(value, out var d) ? d.ToString("yyyy-MM-dd") : value;
    static void Field(Graphics g, string label, string value, int x, int y, Font lf, Font tf) { using var brush = new SolidBrush(Color.Black); g.DrawString(label + ":", lf, brush, x, y); g.DrawString(value, tf, brush, x + 112, y); }
}