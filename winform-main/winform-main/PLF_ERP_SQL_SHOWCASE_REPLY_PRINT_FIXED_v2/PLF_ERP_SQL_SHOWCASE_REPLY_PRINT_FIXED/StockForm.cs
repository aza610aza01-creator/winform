using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;

public class StockForm : Form
{
    readonly TextBox itemCode = new();
    readonly ComboBox itemType = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox product = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly TextBox itemName = new();
    readonly ComboBox warehouse = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly NumericUpDown quantity = new() { Minimum = 0, Maximum = 999999, Value = 0 };
    readonly NumericUpDown reorderPoint = new() { Minimum = 0, Maximum = 999999, Value = 0 };
    readonly DateTimePicker lastUpdated = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
    readonly Label hint = new();

    public Dictionary<string, object?> Values { get; } = new();

    public StockForm()
    {
        Text = "Add Stock Item";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(850, 650);
        MinimumSize = new Size(780, 610);
        Font = Theme.DefaultFont;
        BackColor = Theme.AppBg;

        itemCode.ReadOnly = true;
        itemCode.Text = Database.GenerateNextCode("Stock", "ItemCode", "STK", "-", 4);
        itemCode.BackColor = Theme.PrimarySoft;

        itemType.Items.AddRange(new object[] { "Finished Product", "Raw Material", "Accessory" });

        product.Enabled = false;
        product.BackColor = Color.FromArgb(241, 245, 249);
        product.Items.Add(new LookupItem(0, "0 - No linked product"));
        foreach (var p in Database.GetLookup("Products")) product.Items.Add(p);
        product.SelectedIndex = 0;

        warehouse.Items.AddRange(new object[] { "WH-HK-01", "WH-CN-01", "WH-VN-01" });
        if (warehouse.Items.Count > 0) warehouse.SelectedIndex = 0;

        hint.Text = "Please select Item Type first. Product can only be selected for Finished Product.";
        hint.ForeColor = Theme.Muted;
        hint.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        hint.Dock = DockStyle.Fill;

        itemType.SelectedIndexChanged += (_, _) => ApplyItemTypeRule();
        product.SelectedIndexChanged += (_, _) => AutoFillItemNameFromProduct();

        Controls.Add(Build());
    }

    Control Build()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(26), AutoScroll = true, BackColor = Theme.AppBg };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(root, "Item Code", itemCode);
        AddRow(root, "Item Type *", itemType);
        AddRow(root, "Product", product);
        AddRow(root, "Item Name *", itemName);
        AddRow(root, "Warehouse", warehouse);
        AddRow(root, "Quantity", quantity);
        AddRow(root, "Reorder Point", reorderPoint);
        AddRow(root, "Last Updated", lastUpdated);

        int hintRow = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.SetColumnSpan(hint, 2);
        root.Controls.Add(hint, 0, hintRow);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.AppBg };
        var save = Theme.PrimaryButton("Save Stock", 125);
        var cancel = Theme.SecondaryButton("Cancel", 110);
        save.Click += (_, _) => Save();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        int buttonRow = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.SetColumnSpan(buttons, 2);
        root.Controls.Add(buttons, 0, buttonRow);

        return root;
    }

    static void AddRow(TableLayoutPanel root, string label, Control input)
    {
        int row = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9, FontStyle.Bold) }, 0, row);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 8, 0, 8);
        root.Controls.Add(input, 1, row);
    }

    void ApplyItemTypeRule()
    {
        bool isFinishedProduct = itemType.Text == "Finished Product";
        product.Enabled = isFinishedProduct;
        product.BackColor = isFinishedProduct ? Color.White : Color.FromArgb(241, 245, 249);

        if (!isFinishedProduct)
        {
            product.SelectedIndex = 0;
            if (itemType.Text == "Raw Material")
                hint.Text = "Raw Material cannot select Product. Please type Item Name manually, e.g. Oak Timber Board.";
            else if (itemType.Text == "Accessory")
                hint.Text = "Accessory cannot select Product. Please type Item Name manually, e.g. Chair hardware set.";
            else
                hint.Text = "Please select Item Type first. Product can only be selected for Finished Product.";
        }
        else
        {
            hint.Text = "Finished Product selected. Product dropdown is enabled.";
            AutoFillItemNameFromProduct();
        }
    }

    void AutoFillItemNameFromProduct()
    {
        if (!product.Enabled) return;
        if (product.SelectedItem is LookupItem selectedLookup && selectedLookup.Id > 0)
        {
            string display = selectedLookup.Display;
            int dash = display.IndexOf(" - ", StringComparison.Ordinal);
            itemName.Text = dash >= 0 ? display[(dash + 3)..] : display;
        }
    }

    void Save()
    {
        if (itemType.SelectedIndex < 0) { MessageBox.Show("Please select Item Type before saving."); return; }
        if (warehouse.SelectedIndex < 0) { MessageBox.Show("Please select Warehouse."); return; }

        bool isFinishedProduct = itemType.Text == "Finished Product";
        if (!isFinishedProduct && string.IsNullOrWhiteSpace(itemName.Text)) { MessageBox.Show("Please enter Item Name for Raw Material or Accessory."); return; }
        if (isFinishedProduct && string.IsNullOrWhiteSpace(itemName.Text)) { MessageBox.Show("Please select Product or enter Item Name for Finished Product."); return; }

        Values.Clear();
        Values["ItemCode"] = itemCode.Text.Trim();
        Values["ItemType"] = itemType.Text;
        Values["ItemName"] = itemName.Text.Trim();
        Values["Warehouse"] = warehouse.Text;
        Values["Quantity"] = (int)quantity.Value;
        Values["ReorderPoint"] = (int)reorderPoint.Value;
        Values["LastUpdated"] = lastUpdated.Value.ToString("yyyy-MM-dd");

        if (isFinishedProduct && product.SelectedItem is LookupItem finishedProductLookup && finishedProductLookup.Id > 0)
            Values["ProductId"] = finishedProductLookup.Id;

        DialogResult = DialogResult.OK;
    }
}
