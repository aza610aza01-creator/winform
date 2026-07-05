using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;

public class AnalysisForm : UserControl
{
    private readonly FlowLayoutPanel kpis = new() { Dock = DockStyle.Fill, BackColor = Theme.AppBg, WrapContents = false, AutoScroll = true };
    private readonly BarChartPanel moduleChart = new() { Dock = DockStyle.Fill, Title = "Operational Volume by Module" };
    private readonly BarChartPanel moneyChart = new() { Dock = DockStyle.Fill, Title = "Financial Overview" };
    private readonly DataGridView grid = new();
    private readonly Label status = new();

    public AnalysisForm()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBg;
        Build();
        LoadAnalysis();
    }

    private void Build()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = Theme.AppBg, Padding = new Padding(30, 22, 30, 30) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.Controls.Add(new Label { Text = "Business Analysis Dashboard" + Environment.NewLine + "Live analysis with KPI cards, chart panels and summary table.", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Theme.Text, BackColor = Theme.AppBg }, 0, 0);
        root.Controls.Add(kpis, 0, 1);

        var charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Theme.AppBg };
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        charts.Controls.Add(Card(moduleChart), 0, 0);
        charts.Controls.Add(Card(moneyChart), 1, 0);
        root.Controls.Add(charts, 0, 2);

        Theme.Grid(grid);
        var bottom = Card(grid);
        status.Dock = DockStyle.Bottom;
        status.Height = 24;
        status.ForeColor = Theme.Muted;
        status.BackColor = Color.White;
        bottom.Controls.Add(status);
        root.Controls.Add(bottom, 0, 3);
        Controls.Add(root);
    }

    private static Panel Card(Control inner)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(0, 0, 14, 18) };
        panel.Controls.Add(inner);
        return panel;
    }

    private static Panel Kpi(string title, string value, string note)
    {
        var panel = new Panel { Width = 230, Height = 82, BackColor = Color.White, Padding = new Padding(14), Margin = new Padding(0, 0, 14, 0) };
        panel.Controls.Add(new Label { Text = value, Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Theme.Primary, BackColor = Color.White });
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Theme.Text, BackColor = Color.White });
        panel.Controls.Add(new Label { Text = note, Dock = DockStyle.Bottom, Height = 18, Font = new Font("Segoe UI", 8), ForeColor = Theme.Muted, BackColor = Color.White });
        return panel;
    }

    private void LoadAnalysis()
    {
        try
        {
            int orders = Count("Orders");
            int purchaseOrders = Count("PurchaseOrders");
            int complaints = Count("Complaints");
            int lowStock = LowStock();
            decimal sales = Sum("Orders", "TotalAmount");
            decimal purchasing = SumExpression(Database.GetTable("PurchaseOrders"), "Quantity", "UnitCost");
            decimal paid = Sum("Payments", "Amount");

            kpis.Controls.Clear();
            kpis.Controls.Add(Kpi("Sales Revenue", $"HK$ {sales:N0}", $"{orders} sales orders"));
            kpis.Controls.Add(Kpi("Purchasing", $"HK$ {purchasing:N0}", $"{purchaseOrders} purchase orders"));
            kpis.Controls.Add(Kpi("Payments", $"HK$ {paid:N0}", "received amount"));
            kpis.Controls.Add(Kpi("Low Stock", lowStock.ToString(), "items need attention"));
            kpis.Controls.Add(Kpi("Complaints", complaints.ToString(), "service cases"));

            moduleChart.SetData(new Dictionary<string, decimal> { ["Customers"] = Count("Customers"), ["Suppliers"] = Count("Suppliers"), ["Products"] = Count("Products"), ["Sales Orders"] = orders, ["Purchase Orders"] = purchaseOrders, ["Complaints"] = complaints, ["Stock Items"] = Count("Stock") });
            moneyChart.SetData(new Dictionary<string, decimal> { ["Sales"] = sales, ["Purchasing"] = purchasing, ["Payments"] = paid, ["Open AR"] = Math.Max(0, sales - paid) });

            var table = new DataTable();
            table.Columns.Add("Area"); table.Columns.Add("Metric"); table.Columns.Add("Value"); table.Columns.Add("Comment");
            table.Rows.Add("Sales", "Total order value", $"HK$ {sales:N2}", "Sum of Sales Orders TotalAmount");
            table.Rows.Add("Purchasing", "Estimated PO cost", $"HK$ {purchasing:N2}", "Quantity multiplied by UnitCost");
            table.Rows.Add("Finance", "Payments received", $"HK$ {paid:N2}", "Sum of Payment Records");
            table.Rows.Add("Inventory", "Low stock items", lowStock.ToString(), "Quantity at or below reorder point");
            table.Rows.Add("Customer Service", "Open complaints", OpenComplaints().ToString(), "Complaints still open");
            grid.DataSource = table;
            status.Text = " Analysis refreshed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch (Exception ex) { MessageBox.Show("Analysis failed: " + ex.Message); }
    }

    private static int Count(string table) { try { return Database.GetTable(table).Rows.Count; } catch { return 0; } }
    private static decimal Sum(string table, string column) { try { decimal total = 0; foreach (DataRow row in Database.GetTable(table).Rows) if (decimal.TryParse(Convert.ToString(row[column]), out var v)) total += v; return total; } catch { return 0; } }
    private static decimal SumExpression(DataTable table, string quantityColumn, string unitColumn) { decimal total = 0; foreach (DataRow row in table.Rows) { decimal q = decimal.TryParse(Convert.ToString(row[quantityColumn]), out var qv) ? qv : 0; decimal u = decimal.TryParse(Convert.ToString(row[unitColumn]), out var uv) ? uv : 0; total += q * u; } return total; }
    private static int LowStock() { try { int count = 0; foreach (DataRow row in Database.GetTable("Stock").Rows) { int q = int.TryParse(Convert.ToString(row["Quantity"]), out var qv) ? qv : 0; int r = int.TryParse(Convert.ToString(row["ReorderPoint"]), out var rv) ? rv : 0; if (q <= r) count++; } return count; } catch { return 0; } }
    private static int OpenComplaints() { try { return Database.GetTable("Complaints").Rows.Cast<DataRow>().Count(row => string.Equals(Convert.ToString(row["Status"]), "Open", StringComparison.OrdinalIgnoreCase)); } catch { return 0; } }
}

public class BarChartPanel : Panel
{
    private Dictionary<string, decimal> data = new();
    public string Title { get; set; } = "Chart";
    public BarChartPanel() { DoubleBuffered = true; BackColor = Color.White; }
    public void SetData(Dictionary<string, decimal> values) { data = values; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var titleFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 8);
        using var valueFont = new Font("Segoe UI", 8, FontStyle.Bold);
        using var textBrush = new SolidBrush(Theme.Text);
        using var mutedBrush = new SolidBrush(Theme.Muted);
        using var barBrush = new SolidBrush(Theme.Primary);
        using var softBrush = new SolidBrush(Theme.PrimarySoft);
        g.DrawString(Title, titleFont, textBrush, 4, 2);
        if (data.Count == 0) { g.DrawString("No data available", labelFont, mutedBrush, 8, 42); return; }
        decimal max = data.Values.Max(); if (max <= 0) max = 1;
        int top = 40, left = 120, right = 18;
        int rowHeight = Math.Max(24, (Height - top - 18) / Math.Max(1, data.Count));
        int barMax = Math.Max(60, Width - left - right - 70);
        int y = top;
        foreach (var item in data)
        {
            string label = item.Key.Length > 18 ? item.Key.Substring(0, 18) + "..." : item.Key;
            int barWidth = (int)(barMax * (item.Value / max));
            var bg = new Rectangle(left, y + 5, barMax, Math.Max(10, rowHeight - 12));
            var fg = new Rectangle(left, y + 5, Math.Max(2, barWidth), Math.Max(10, rowHeight - 12));
            g.DrawString(label, labelFont, mutedBrush, 4, y + 4);
            g.FillRectangle(softBrush, bg);
            g.FillRectangle(barBrush, fg);
            g.DrawString(item.Value.ToString("N0"), valueFont, textBrush, left + barMax + 8, y + 3);
            y += rowHeight;
        }
    }
}
