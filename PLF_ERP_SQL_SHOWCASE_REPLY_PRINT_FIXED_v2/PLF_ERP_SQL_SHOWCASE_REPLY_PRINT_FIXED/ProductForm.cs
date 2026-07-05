using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace PremiumLivingFurnitureWinForms;

public class ProductForm : Form
{
    readonly TextBox productCode = new();
    readonly TextBox productName = new();
    readonly TextBox category = new();
    readonly NumericUpDown unitPrice = new() { Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, Increment = 10 };
    readonly ComboBox status = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly TextBox imagePath = new() { ReadOnly = true };
    readonly PictureBox preview = new()
    {
        Width = 180,
        Height = 130,
        SizeMode = PictureBoxSizeMode.Zoom,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.White
    };

    string selectedImageFile = "";

    public ProductForm()
    {
        Text = "Add Product";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 650);
        MinimumSize = new Size(820, 610);
        Font = Theme.DefaultFont;
        BackColor = Theme.AppBg;

        productCode.ReadOnly = true;
        productCode.Text = Database.GenerateNextCode("Products", "ProductCode", "P", "", 3);
        productCode.BackColor = Theme.PrimarySoft;

        status.Items.AddRange(new object[] { "Active", "Inactive" });
        status.SelectedIndex = 0;

        Controls.Add(Build());
    }

    Control Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(26),
            AutoScroll = true,
            BackColor = Theme.AppBg
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(root, "Product Code", productCode);
        AddRow(root, "Product Name *", productName);
        AddRow(root, "Category", category);
        AddRow(root, "Unit Price", unitPrice);
        AddRow(root, "Status", status);
        AddImageRow(root);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.AppBg };
        var save = Theme.PrimaryButton("Save Product", 135);
        var cancel = Theme.SecondaryButton("Cancel", 110);
        save.Click += (_, _) => SaveProduct();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        int buttonRow = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.SetColumnSpan(buttons, 2);
        root.Controls.Add(buttons, 0, buttonRow);

        return root;
    }

    static void AddRow(TableLayoutPanel root, string label, Control input)
    {
        int row = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        }, 0, row);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 8, 0, 8);
        root.Controls.Add(input, 1, row);
    }

    void AddImageRow(TableLayoutPanel root)
    {
        int row = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        root.Controls.Add(new Label
        {
            Text = "Product Image",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        }, 0, row);

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Theme.AppBg };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Theme.AppBg };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var choose = Theme.SecondaryButton("Choose Image", 140);
        var clear = Theme.SecondaryButton("Clear Image", 130);
        choose.Click += (_, _) => ChooseImage();
        clear.Click += (_, _) => ClearImage();

        imagePath.Dock = DockStyle.Fill;
        imagePath.Margin = new Padding(0, 8, 0, 8);

        var buttonLine = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Theme.AppBg };
        buttonLine.Controls.Add(choose);
        buttonLine.Controls.Add(clear);

        right.Controls.Add(buttonLine, 0, 0);
        right.Controls.Add(imagePath, 0, 1);
        right.Controls.Add(new Label
        {
            Text = "Supported: JPG, JPEG, PNG, BMP, GIF. Image will be copied to ProductImages folder.",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 8),
            BackColor = Theme.AppBg
        }, 0, 2);

        panel.Controls.Add(preview, 0, 0);
        panel.Controls.Add(right, 1, 0);
        root.Controls.Add(panel, 1, row);
    }

    void ChooseImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose product image",
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        selectedImageFile = dialog.FileName;
        imagePath.Text = selectedImageFile;

        using var img = Image.FromFile(selectedImageFile);
        preview.Image?.Dispose();
        preview.Image = new Bitmap(img);
    }

    void ClearImage()
    {
        selectedImageFile = "";
        imagePath.Clear();
        preview.Image?.Dispose();
        preview.Image = null;
    }

    void SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(productName.Text))
        {
            MessageBox.Show("Please enter Product Name.");
            return;
        }

        try
        {
            EnsureImagePathColumn();
            string savedImagePath = SaveImageToProductFolder();

            using var c = Database.GetConnection();
            c.Open();
            using var cmd = new MySqlCommand(@"INSERT INTO Products
                (ProductCode, ProductName, Category, UnitPrice, ImagePath, Status)
                VALUES (@code, @name, @category, @price, @imagePath, @status)", c);
            cmd.Parameters.AddWithValue("@code", productCode.Text.Trim());
            cmd.Parameters.AddWithValue("@name", productName.Text.Trim());
            cmd.Parameters.AddWithValue("@category", string.IsNullOrWhiteSpace(category.Text) ? DBNull.Value : category.Text.Trim());
            cmd.Parameters.AddWithValue("@price", unitPrice.Value);
            cmd.Parameters.AddWithValue("@imagePath", string.IsNullOrWhiteSpace(savedImagePath) ? DBNull.Value : savedImagePath);
            cmd.Parameters.AddWithValue("@status", status.Text);
            cmd.ExecuteNonQuery();

            MessageBox.Show("Product added successfully.");
            DialogResult = DialogResult.OK;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            MessageBox.Show("Product Code already exists. Please try again.", "Duplicate Product Code");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to add product: " + ex.Message);
        }
    }

    static void EnsureImagePathColumn()
    {
        using var c = Database.GetConnection();
        c.Open();
        using var cmd = new MySqlCommand("ALTER TABLE Products ADD COLUMN ImagePath VARCHAR(500) NULL AFTER UnitPrice", c);
        try { cmd.ExecuteNonQuery(); }
        catch (MySqlException ex) when (ex.Number == 1060) { }
    }

    string SaveImageToProductFolder()
    {
        if (string.IsNullOrWhiteSpace(selectedImageFile)) return "";

        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages");
        Directory.CreateDirectory(folder);

        string ext = Path.GetExtension(selectedImageFile).ToLowerInvariant();
        string safeCode = SafeFileName(productCode.Text.Trim());
        string fileName = safeCode + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ext;
        string destination = Path.Combine(folder, fileName);

        File.Copy(selectedImageFile, destination, overwrite: true);
        return Path.Combine("ProductImages", fileName).Replace("\\", "/");
    }

    static string SafeFileName(string text)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) text = text.Replace(c, '_');
        return string.IsNullOrWhiteSpace(text) ? "product" : text;
    }
}
