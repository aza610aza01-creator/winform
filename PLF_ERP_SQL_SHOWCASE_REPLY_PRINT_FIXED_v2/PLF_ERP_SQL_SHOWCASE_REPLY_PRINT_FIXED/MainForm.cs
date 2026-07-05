using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace PremiumLivingFurnitureWinForms;

public class MainForm : Form
{
    readonly Panel content = new() { Dock = DockStyle.Fill };
    readonly NaturalSidebarMenuPanel menu = new();
    readonly string currentUser, currentRole;
    readonly Dictionary<string, ModuleDefinition> modules;

    public MainForm(string user, string role)
    {
        currentUser = user;
        currentRole = role;
        modules = BuildModules();

        Text = "PLF ERP - Analysis Dashboard";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1320, 860);
        Font = Theme.DefaultFont;
        BackColor = Theme.AppBg;

        Controls.Add(content);
        Controls.Add(Header());
        Controls.Add(Sidebar());

        Dashboard();
    }

    Control Header()
    {
        var h = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = Color.White,
            Padding = new Padding(24, 14, 24, 14)
        };

        var chip = Theme.Chip(currentRole);
        chip.Dock = DockStyle.Right;
        h.Controls.Add(chip);

        h.Controls.Add(new Label
        {
            Text = currentUser,
            Dock = DockStyle.Right,
            Width = 170,
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.White,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        });

        h.Controls.Add(new Label
        {
            Text = "Premium Living Furniture ERP",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            BackColor = Color.White,
            ForeColor = Theme.Text
        });

        return h;
    }

    Control Sidebar()
    {
        var side = new Panel { Dock = DockStyle.Left, Width = 318, BackColor = Theme.Side };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Theme.Side };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        var header = new Label { Text = "  PLF System", Dock = DockStyle.Fill, ForeColor = Color.White, BackColor = Theme.Side, Font = new Font("Segoe UI", 17, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        menu.Controls.Clear(); menu.Dock = DockStyle.Fill;
        AddMenu("Dashboard", Dashboard);
        AddMenu("Business Analysis", () => SetContent(new AnalysisForm()));
        AddMenu("Data Relationships", Relations);
        if (Security.CanShowcaseDatabase(currentRole)) AddMenu("SQL Database", FixedShowcaseSqlDatabase);
        AddMenu("Update Profile", Profile);
        menu.Controls.Add(new Label { Height = 12, Width = 280, BackColor = Theme.Side });
        foreach (var m in modules.Values) if (Security.CanAccess(currentRole, m.TableName)) AddMenu(m.Title, () => Module(m));
        layout.Controls.Add(header, 0, 0); layout.Controls.Add(menu, 0, 1); layout.Controls.Add(FixedUtilityPanel(), 0, 2);
        side.Controls.Add(layout); return side;
    }


    Control FixedUtilityPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Side, Padding = new Padding(18, 12, 18, 12) };
        var box = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, BackColor = Theme.Side };
        box.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); box.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); box.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); box.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); box.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); box.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        box.Controls.Add(new Label { Text = "Utilities", Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), BackColor = Theme.Side }, 0, 0);
        var check = Theme.SecondaryButton("Check SQL Connection", 210); check.Click += (_, _) => FixedCheckSqlConnection(); box.Controls.Add(check, 0, 1);
        var showcase = Theme.SecondaryButton("Showcase SQL Database", 220); showcase.Visible = Security.CanShowcaseDatabase(currentRole); showcase.Click += (_, _) => FixedShowcaseSqlDatabase(); box.Controls.Add(showcase, 0, 2);
        var debug = Theme.SecondaryButton("Debug Check", 150); debug.Visible = Security.CanDebugCheck(currentRole); debug.Click += (_, _) => FixedDebugCheck(); box.Controls.Add(debug, 0, 3);
        var logout = Theme.SecondaryButton("Logout", 110); logout.Click += (_, _) => Logout(); box.Controls.Add(logout, 0, 4);
        panel.Controls.Add(box); return panel;
    }

    void FixedCheckSqlConnection()
    {
        try { MessageBox.Show(Database.TestConnectionDetailed(), "SQL Connection"); }
        catch (Exception ex) { MessageBox.Show("SQL connection failed:\n\n" + ex.Message, "SQL Connection Failed"); }
    }

    void FixedDebugCheck()
    {
        if (!Security.CanDebugCheck(currentRole)) { MessageBox.Show("You do not have permission to run debug check."); return; }
        try { MessageBox.Show("Debug Check Completed\n\nCurrent User: " + currentUser + "\nCurrent Role: " + currentRole + "\n\n" + Database.TestConnectionDetailed(), "Debug Check"); }
        catch (Exception ex) { MessageBox.Show("Debug check failed:\n\n" + ex, "Debug Check Failed"); }
    }

    void FixedShowcaseSqlDatabase()
    {
        if (!Security.CanShowcaseDatabase(currentRole)) { MessageBox.Show("You do not have permission to showcase the SQL database."); return; }
        var page = Page("SQL Database Showcase", "Read-only SQL database view. Click Show Details to view table structure and rows.");
        page.RowStyles[0].Height = 112;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Theme.AppBg };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        var tablesGrid = Grid(); tablesGrid.AutoGenerateColumns = false; tablesGrid.Columns.Clear();
        tablesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "Table Name", DataPropertyName = "TableName", ReadOnly = true });
        tablesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rows", HeaderText = "Rows", DataPropertyName = "Rows", ReadOnly = true });
        tablesGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Action", HeaderText = "Action", Text = "Show Details", UseColumnTextForButtonValue = true });
        var details = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = Theme.AppBg };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        try
        {
            var summary = FixedLoadSqlTableSummary(); tablesGrid.DataSource = summary;
            tablesGrid.CellContentClick += (_, e) => { if (e.RowIndex < 0 || tablesGrid.Columns[e.ColumnIndex].Name != "Action") return; string tableName = Convert.ToString(tablesGrid.Rows[e.RowIndex].Cells["TableName"].Value) ?? ""; FixedShowSqlTableDetails(details, tableName); };
            if (summary.Rows.Count > 0) FixedShowSqlTableDetails(details, Convert.ToString(summary.Rows[0]["TableName"]) ?? "");
        }
        catch (Exception ex)
        {
            root.Controls.Add(Card(new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = Theme.Danger, Text = "Unable to load SQL database showcase:\r\n\r\n" + ex }), 0, 0);
            page.Controls.Add(root, 0, 1); SetContent(page); return;
        }
        root.Controls.Add(FixedCardWithTitle("Database Tables", tablesGrid), 0, 0); root.Controls.Add(details, 0, 1); page.Controls.Add(root, 0, 1); SetContent(page);
    }

    DataTable FixedLoadSqlTableSummary()
    {
        var result = new DataTable(); result.Columns.Add("TableName"); result.Columns.Add("Rows", typeof(int));
        using var c = Database.GetConnection(); c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SHOW TABLES"; using var reader = cmd.ExecuteReader();
        var tables = new List<string>(); while (reader.Read()) tables.Add(Convert.ToString(reader[0]) ?? ""); reader.Close();
        foreach (string table in tables.Where(FixedIsSafeSqlIdentifier).OrderBy(x => x)) { using var countCmd = c.CreateCommand(); countCmd.CommandText = $"SELECT COUNT(*) FROM `{table}`"; result.Rows.Add(table, Convert.ToInt32(countCmd.ExecuteScalar() ?? 0)); }
        return result;
    }

    void FixedShowSqlTableDetails(TableLayoutPanel host, string tableName)
    {
        host.Controls.Clear(); if (!FixedIsSafeSqlIdentifier(tableName)) return;
        host.Controls.Add(FixedCardWithTitle("Table Structure: " + tableName, FixedSqlGrid($"DESCRIBE `{tableName}`")), 0, 0);
        host.Controls.Add(FixedCardWithTitle("Rows: " + tableName + " (first 200)", FixedSqlGrid($"SELECT * FROM `{tableName}` LIMIT 200")), 1, 0);
    }

    DataGridView FixedSqlGrid(string sql)
    {
        var g = Grid(); using var c = Database.GetConnection(); c.Open(); using var da = new MySqlDataAdapter(sql, c); var dt = new DataTable(); da.Fill(dt); g.DataSource = dt; return g;
    }

    static bool FixedIsSafeSqlIdentifier(string value) => !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[A-Za-z0-9_]+$");

    Panel FixedCardWithTitle(string title, Control inner)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(30, 0, 30, 18) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.White };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, ForeColor = Theme.Text, BackColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        inner.Dock = DockStyle.Fill; layout.Controls.Add(inner, 0, 1); panel.Controls.Add(layout); return panel;
    }

    void AddMenu(string text, Action action)
    {
        var b = new Button
        {
            Text = "   " + text,
            Width = 280,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Theme.Side,
            ForeColor = Color.FromArgb(226, 232, 240),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(0, 2, 0, 2),
            Cursor = Cursors.Hand
        };

        b.FlatAppearance.BorderSize = 0;
        b.MouseEnter += (_, _) => b.BackColor = Theme.Side2;
        b.MouseLeave += (_, _) => b.BackColor = Theme.Side;
        b.Click += (_, _) => action();

        menu.Controls.Add(b);
    }

    void SetContent(Control control)
    {
        content.Controls.Clear();
        control.Dock = DockStyle.Fill;
        content.Controls.Add(control);
    }

    TableLayoutPanel Page(string title, string subtitle)
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Theme.AppBg
        };

        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        p.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        p.Controls.Add(new Label
        {
            Text = title + Environment.NewLine + subtitle,
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 20, 30, 0),
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.AppBg
        }, 0, 0);

        return p;
    }

    void Dashboard()
    {
        var page = Page("Dashboard", "Full ERP modules with Business Analysis charts.");

        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 12),
            ScrollBars = ScrollBars.Vertical
        };

        box.Lines = new[]
        {
            "Included:",
            "",
            "- New Business Analysis form with KPI cards and bar charts.",
            "- Purchase Orders Add New has Supplier and Supplier Item dropdowns.",
            "- Supplier Item list filters by selected supplier and auto-fills Unit Cost.",
            "- Suppliers, Purchase Orders, Complaints, Stock and Stock Movements are included with sample data.",
            "- Sales Order Items hides Add New because items are created from Sales Orders.",
            "- Stock > Add New opens a dedicated StockForm with Item Type first rule.",
            "- Stock Movements update stock quantity and low-stock status.",
            "- Sales Orders support Cancel Order.",
            "- Customer Reply Slips print a large signature PNG."
        };

        page.Controls.Add(Card(box), 0, 1);
        SetContent(page);
    }

    Panel Card(Control inner)
    {
        var p = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18),
            Margin = new Padding(30, 0, 30, 30)
        };

        p.Controls.Add(inner);
        return p;
    }

    void Health()
    {
        var p = Page("Database Health", "Checks MySQL connection.");

        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            Font = new Font("Consolas", 11),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White
        };

        try
        {
            box.Text = Database.TestConnectionDetailed();
        }
        catch (Exception ex)
        {
            box.Text = ex.ToString();
        }

        p.Controls.Add(Card(box), 0, 1);
        SetContent(p);
    }

    void Relations()
    {
        var p = Page("Data Relationships", "Foreign keys between the forms.");
        var g = Grid();

        try
        {
            g.DataSource = Database.GetRelationships();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

        p.Controls.Add(Wrap(g), 0, 1);
        SetContent(p);
    }

    void Module(ModuleDefinition module)
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Theme.AppBg
        };

        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        p.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        p.Controls.Add(new Label
        {
            Text = module.Title + Environment.NewLine + module.Subtitle,
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 20, 30, 0),
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Theme.AppBg
        }, 0, 0);

        var toolbarCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18, 11, 18, 11),
            Margin = new Padding(30, 0, 30, 12)
        };

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            WrapContents = false
        };

        var search = new TextBox
        {
            Width = 350,
            Height = 34,
            PlaceholderText = "Search " + module.Title.ToLower(),
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 6, 16, 6)
        };

        var add = Theme.PrimaryButton("Add New");
        var delete = Theme.SecondaryButton("Delete");
        var refresh = Theme.SecondaryButton("Refresh");
        var cancelOrder = Theme.SecondaryButton("Cancel Order", 130);
        var print = Theme.SecondaryButton("Print Content", 125);
        var printSelected = Theme.SecondaryButton("Print Selected Rows", 170);

        bool isOrderItems = module.TableName == "OrderItems";

        add.Visible = !isOrderItems;
        add.Enabled = !isOrderItems && Security.CanAdd(currentRole, module.TableName);
        delete.Enabled = Security.CanDelete(currentRole, module.TableName);

        cancelOrder.Visible = module.TableName == "Orders";
        cancelOrder.Enabled = module.TableName == "Orders" && Security.CanAdd(currentRole, module.TableName);

        toolbar.Controls.Add(search);
        toolbar.Controls.Add(add);
        toolbar.Controls.Add(delete);
        toolbar.Controls.Add(refresh);

        if (module.TableName == "Orders")
            toolbar.Controls.Add(cancelOrder);

        toolbar.Controls.Add(print);
        toolbar.Controls.Add(printSelected);

        if (isOrderItems)
        {
            toolbar.Controls.Add(new Label
            {
                Text = "  Order items are created from Sales Orders",
                AutoSize = true,
                Height = 38,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.Muted,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(8, 11, 0, 0),
                BackColor = Color.White
            });
        }

        toolbarCard.Controls.Add(toolbar);
        p.Controls.Add(toolbarCard, 0, 1);

        var grid = Grid();

        void Load()
        {
            try
            {
                grid.DataSource = Database.GetTable(module.TableName, search.Text);
                HideTechnicalColumns(grid);

                if (module.TableName == "Products")
                {
                    AddProductImageColumn(grid);
                }

                if (module.TableName == "Orders")
                {
                    if (grid.Columns.Contains("CustomerName"))
                    {
                        grid.Columns["CustomerName"].HeaderText = "Customer Name";
                        grid.Columns["CustomerName"].DisplayIndex = 2;
                    }

                    if (grid.Columns.Contains("SalesUser"))
                        grid.Columns["SalesUser"].HeaderText = "Sales User";
                }

                if (module.TableName == "OrderItems")
                {
                    if (grid.Columns.Contains("Id"))
                        grid.Columns["Id"].HeaderText = "Sales Order Item ID";

                    if (grid.Columns.Contains("SalesOrderId"))
                    {
                        grid.Columns["SalesOrderId"].Visible = true;
                        grid.Columns["SalesOrderId"].HeaderText = "Sales Order ID";
                        grid.Columns["SalesOrderId"].DisplayIndex = 0;
                    }

                    if (grid.Columns.Contains("OrderNo"))
                        grid.Columns["OrderNo"].DisplayIndex = 1;

                    if (grid.Columns.Contains("CustomerName"))
                    {
                        grid.Columns["CustomerName"].HeaderText = "Customer Name";
                        grid.Columns["CustomerName"].DisplayIndex = 2;
                    }
                    AddProductImageColumn(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        add.Click += (_, _) => Add(module, Load);
        delete.Click += (_, _) => Delete(module, grid, Load);
        refresh.Click += (_, _) => Load();
        cancelOrder.Click += (_, _) => CancelOrder(grid, Load);

        print.Click += (_, _) =>
        {
            if (module.TableName == "ReplySlips")
                SelectedRecordPrintFormHelper.PrintSelectedRecordForms(this, module.Title, module.TableName, grid, false);
            else
                PagePrintHelper.PrintGridContent(this, module.Title, grid);
        };
        printSelected.Click += (_, _) =>
        {
            if (module.TableName == "ReplySlips")
                SelectedRecordPrintFormHelper.PrintSelectedRecordForms(this, module.Title, module.TableName, grid, true);
            else
                PagePrintHelper.PrintSelectedRows(this, module.Title, grid);
        };

        search.TextChanged += (_, _) => Load();

        p.Controls.Add(Wrap(grid), 0, 2);
        SetContent(p);
        Load();
    }


    static void AddProductImageColumn(DataGridView grid)
    {
        if (!grid.Columns.Contains("ImagePath")) return;
        if (!grid.Columns.Contains("ProductPicture")) grid.Columns.Insert(Math.Min(4, grid.Columns.Count), new DataGridViewImageColumn { Name = "ProductPicture", HeaderText = "Picture", ImageLayout = DataGridViewImageCellLayout.Zoom, Width = 110 });
        grid.RowTemplate.Height = 72;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            row.Cells["ProductPicture"].Value = SalesOrderForm.LoadProductThumbnail(Convert.ToString(row.Cells["ImagePath"].Value) ?? "", 96, 56);
            row.Height = 72;
        }
        grid.Columns["ImagePath"].Visible = false;
        if (grid.Columns.Contains("ProductPicture")) grid.Columns["ProductPicture"].DisplayIndex = Math.Min(3, grid.Columns.Count - 1);
    }

    static void HideTechnicalColumns(DataGridView g)
    {
        foreach (var c in new[]
        {
            "Password",
            "OrderId",
            "ProductId",
            "CustomerId",
            "SupplierId",
            "SupplierItemId",
            "RelatedOrderId",
            "AssignedUserId",
            "DeliveryNoteId",
            "StockId",
            "InvoiceId"
        })
        {
            if (g.Columns.Contains(c))
                g.Columns[c].Visible = false;
        }

        g.ClearSelection();
    }

    DataGridView Grid()
    {
        var g = new DataGridView();
        Theme.Grid(g);
        return g;
    }

    Control Wrap(DataGridView g)
    {
        var p = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18),
            Margin = new Padding(30, 0, 30, 30)
        };

        p.Controls.Add(g);
        return p;
    }

    void Add(ModuleDefinition module, Action reload)
    {
        if (module.TableName == "Users")
        {
            using var ef = new EmployeeAccountForm();

            if (ef.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                Database.AddRecord(module.TableName, ef.Values);
                reload();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show(
                    "This username already exists. Please use another username. No new account was created.",
                    "Duplicate Username"
                );
            }

            return;
        }

        if (module.TableName == "Products")
        {
            using var pf = new ProductForm();

            if (pf.ShowDialog(this) != DialogResult.OK)
                return;

            reload();
            return;
        }

        if (module.TableName == "Orders")
        {
            using var f = new SalesOrderForm();

            if (f.ShowDialog(this) != DialogResult.OK)
                return;

            Database.AddSalesOrderWithItems(
                f.OrderNo,
                f.CustomerId,
                f.Status,
                f.OrderDate,
                f.Priority,
                f.SalesUserId,
                f.Items
            );

            reload();
            return;
        }

        if (module.TableName == "PurchaseOrders")
        {
            using var pf = new PurchaseOrderForm();

            if (pf.ShowDialog(this) != DialogResult.OK)
                return;

            Database.AddRecord(module.TableName, pf.Values);
            reload();
            return;
        }

        if (module.TableName == "Stock")
        {
            using var sf = new StockForm();

            if (sf.ShowDialog(this) != DialogResult.OK)
                return;

            Database.AddRecord(module.TableName, sf.Values);
            reload();
            return;
        }

        using var rf = new RecordForm("Add " + module.Title, module.Fields);

        if (rf.ShowDialog(this) != DialogResult.OK)
            return;

        Database.AddRecord(module.TableName, rf.Values);
        reload();
    }

    void CancelOrder(DataGridView grid, Action reload)
    {
        if (grid.CurrentRow == null || !grid.Columns.Contains("Id"))
        {
            MessageBox.Show("Please select one sales order first.", "Cancel Order");
            return;
        }

        int orderId = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);

        string orderNo = grid.Columns.Contains("OrderNo")
            ? Convert.ToString(grid.CurrentRow.Cells["OrderNo"].Value) ?? ""
            : "";

        string currentStatus = grid.Columns.Contains("Status")
            ? Convert.ToString(grid.CurrentRow.Cells["Status"].Value) ?? ""
            : "";

        if (currentStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("This order has already been cancelled.", "Cancel Order");
            return;
        }

        if (currentStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
            currentStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "Delivered or closed orders cannot be cancelled.",
                "Cancel Order",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        string message = string.IsNullOrWhiteSpace(orderNo)
            ? "Cancel selected sales order?"
            : $"Cancel sales order {orderNo}?";

        if (MessageBox.Show(
                message,
                "Confirm Cancel Order",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            ) != DialogResult.Yes)
            return;

        try
        {
            using var c = Database.GetConnection();
            c.Open();

            using var cmd = new MySqlCommand(
                "UPDATE Orders SET Status = 'Cancelled' WHERE Id = @id",
                c
            );

            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.ExecuteNonQuery();

            MessageBox.Show("Sales order has been cancelled.", "Cancel Order");
            reload();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to cancel order: " + ex.Message, "Cancel Order");
        }
    }

    void Delete(ModuleDefinition module, DataGridView grid, Action reload)
    {
        if (grid.CurrentRow == null || !grid.Columns.Contains("Id"))
            return;

        int id = Convert.ToInt32(grid.CurrentRow.Cells["Id"].Value);

        if (MessageBox.Show("Delete selected record?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;

        Database.DeleteRecord(module.TableName, id);
        reload();
    }

    void Profile()
    {
        using var f = new ProfileForm(currentUser);
        f.ShowDialog(this);
    }

    void Logout()
    {
        Hide();

        var login = new LoginForm();
        login.FormClosed += (_, _) => Close();
        login.Show();
    }

    static Dictionary<string, ModuleDefinition> BuildModules()
    {
        return new()
        {
            ["Users"] = new(
                "Employee Accounts",
                "Users",
                "Manage staff login accounts and assigned roles",
                new()
                {
                    F("Username"),
                    P("Password"),
                    L("RoleId", "Role", "UserRoles"),
                    F("FullName"),
                    F("Email", false),
                    C("Status", new[] { "Active", "Inactive" })
                }
            ),

            ["Suppliers"] = new(
                "Suppliers",
                "Suppliers",
                "Supplier master data",
                new()
                {
                    F("SupplierCode", true, Code("Suppliers", "SupplierCode", "SUP", "-", 3), true),
                    F("SupplierName"),
                    F("ContactPerson", false),
                    F("Phone", false),
                    F("Email", false),
                    F("Country", false),
                    C("Status", new[] { "Active", "Inactive" })
                }
            ),

            ["SupplierItems"] = new(
                "Supplier Items",
                "SupplierItems",
                "Items supplied by each supplier",
                new()
                {
                    L("SupplierId", "Supplier", "Suppliers"),
                    L("ProductId", "Linked Product", "Products"),
                    C("ItemType", new[] { "Raw Material", "Accessory", "Fabric", "Finished Product", "Service" }),
                    F("SupplyDescription"),
                    N("DefaultUnitCost", FieldKind.Decimal),
                    C("Status", new[] { "Active", "Inactive" })
                }
            ),

            ["Customers"] = new(
                "Customers",
                "Customers",
                "Customer master data",
                new()
                {
                    F("CustomerCode", true, Code("Customers", "CustomerCode", "C", "", 4), true),
                    F("CustomerName"),
                    C("CustomerType", new[] { "B2B", "B2C", "Internal" }),
                    F("Phone", false),
                    F("Email", false),
                    F("Address", false),
                    C("Status", new[] { "Active", "Inactive" })
                }
            ),

            ["Products"] = new(
                "Products",
                "Products",
                "Furniture product catalogue",
                new()
                {
                    F("ProductCode", true, Code("Products", "ProductCode", "P", "", 3), true),
                    F("ProductName"),
                    F("Category", false),
                    N("UnitPrice", FieldKind.Decimal),
                    C("Status", new[] { "Active", "Inactive" })
                }
            ),

            ["Orders"] = new(
                "Sales Orders",
                "Orders",
                "Add New supports multiple products",
                new()
                {
                    F("OrderNo", true, Code("Orders", "OrderNo", "SO-2026", "-", 4), true),
                    L("CustomerId", "Customer", "Customers"),
                    L("ProductId", "Main Product", "Products"),
                    N("Quantity"),
                    C("Status", new[] { "Draft", "Confirmed", "Processing", "Delivered", "Closed", "Cancelled" }),
                    D("OrderDate"),
                    C("Priority", new[] { "Normal", "High", "Urgent", "VIP" }),
                    L("SalesUserId", "Sales User", "Users"),
                    N("TotalAmount", FieldKind.Decimal)
                }
            ),

            ["OrderItems"] = new(
                "Sales Order Items",
                "OrderItems",
                "Read-only list generated from Sales Orders",
                new()
                {
                    L("OrderId", "Order", "Orders"),
                    L("ProductId", "Product", "Products"),
                    F("Description", false),
                    N("Quantity"),
                    N("UnitPrice", FieldKind.Decimal),
                    N("Discount", FieldKind.Decimal),
                    N("LineTotal", FieldKind.Decimal)
                }
            ),

            ["PurchaseOrders"] = new(
                "Purchase Orders",
                "PurchaseOrders",
                "Supplier item selection connected to suppliers",
                new()
                {
                    F("PONo", true, Code("PurchaseOrders", "PONo", "PO", "-", 4), true),
                    L("SupplierId", "Supplier", "Suppliers"),
                    L("SupplierItemId", "Supplier Item", "SupplierItems"),
                    L("RelatedOrderId", "Related Sales Order", "Orders"),
                    F("Item", false),
                    N("Quantity"),
                    N("UnitCost", FieldKind.Decimal),
                    C("Status", new[] { "Request", "Approved", "Supplier Confirmed", "Received", "Cancelled" }),
                    D("OrderDate"),
                    D("ExpectedDate")
                }
            ),

            ["Complaints"] = new(
                "Complaints",
                "Complaints",
                "Customer service cases",
                new()
                {
                    F("ComplaintNo", true, Code("Complaints", "ComplaintNo", "FB-2026", "-", 4), true),
                    L("CustomerId", "Customer", "Customers"),
                    L("RelatedOrderId", "Related Order", "Orders"),
                    C("IssueType", new[]
                    {
                        "Late delivery",
                        "Damage",
                        "Wrong item",
                        "Missing item",
                        "Quality issue",
                        "Return Request",
                        "Replacement Request",
                        "Refund Request",
                        "Other"
                    }),
                    C("Priority", new[] { "Low", "Medium", "High", "Urgent" }),
                    C("Status", new[] { "Open", "In Progress", "Resolved", "Closed" }),
                    L("AssignedUserId", "Assigned User", "Users"),
                    D("CreatedDate"),
                    F("Notes", false)
                }
            ),

            ["Stock"] = new(
                "Stock",
                "Stock",
                "Inventory balances",
                new()
                {
                    F("ItemCode", true, Code("Stock", "ItemCode", "STK", "-", 4), true),
                    L("ProductId", "Product", "Products"),
                    F("ItemName", false),
                    C("ItemType", new[] { "Finished Product", "Raw Material", "Accessory" }),
                    C("Warehouse", new[] { "WH-HK-01", "WH-CN-01", "WH-VN-01" }),
                    N("Quantity"),
                    N("ReorderPoint"),
                    C("Status", new[] { "Normal", "LOW STOCK", "Overstock" }),
                    D("LastUpdated")
                }
            ),

            ["StockMovements"] = new(
                "Stock Movements",
                "StockMovements",
                "Transactions that update stock",
                new()
                {
                    F("MovementNo", true, Code("StockMovements", "MovementNo", "SM", "-", 4), true),
                    L("StockId", "Stock Item", "Stock"),
                    C("MovementType", new[] { "Receipt", "Issue", "Transfer", "Adjustment" }),
                    N("Quantity"),
                    C("Warehouse", new[] { "WH-HK-01", "WH-CN-01", "WH-VN-01" }),
                    D("MovementDate"),
                    F("Reason", false)
                }
            ),

            ["DeliveryNotes"] = new(
                "Shipping / Delivery",
                "DeliveryNotes",
                "Delivery notes with from/to addresses",
                new()
                {
                    F("DeliveryNoteNo", true, Code("DeliveryNotes", "DeliveryNoteNo", "DN", "-", 4), true),
                    L("OrderId", "Order", "Orders"),
                    C("Warehouse", new[] { "WH-HK-01", "WH-CN-01", "WH-VN-01" }),
                    F("FromAddress", false),
                    F("ToAddress", false),
                    C("DeliveryMethod", new[] { "Company Truck", "Courier", "Customer Pickup" }),
                    F("DriverOrCourier", false),
                    C("Status", new[] { "Draft", "Dispatched", "Returned", "Closed" }),
                    D("DispatchDate"),
                    F("RouteNotes", false)
                }
            ),

            ["ReplySlips"] = new(
                "Customer Reply Slips",
                "ReplySlips",
                "Print Form renders large SignatureRef PNG",
                new()
                {
                    F("ReplySlipNo", true, Code("ReplySlips", "ReplySlipNo", "RS", "-", 4), true),
                    L("DeliveryNoteId", "Delivery Note", "DeliveryNotes"),
                    L("CustomerId", "Customer", "Customers"),
                    F("ContactPerson", false),
                    C("ResponseType", new[]
                    {
                        "Delivery Acknowledgement",
                        "Customer Feedback",
                        "Damage Report",
                        "Missing Item",
                        "Wrong Item",
                        "Return Request",
                        "Other"
                    }),
                    C("SatisfactionRating", new[]
                    {
                        "5 - Very Satisfied",
                        "4 - Satisfied",
                        "3 - Neutral",
                        "2 - Dissatisfied",
                        "1 - Very Dissatisfied",
                        "N/A"
                    }),
                    C("FollowUpRequired", new[] { "No", "Yes" }),
                    F("ReceivedBy", false),
                    F("SignatureRef", false, "Signatures/sig_rs_0001.png"),
                    C("Status", new[] { "Pending Customer Reply", "Received", "Follow-up Required", "Resolved", "Closed" }),
                    D("ReturnedDate"),
                    F("Remarks", false)
                }
            ),

            ["Invoices"] = new(
                "Customer Invoices",
                "Invoices",
                "Invoices linked to orders",
                new()
                {
                    F("InvoiceNo", true, Code("Invoices", "InvoiceNo", "INV", "-", 4), true),
                    L("OrderId", "Order", "Orders"),
                    L("CustomerId", "Customer", "Customers"),
                    N("Amount", FieldKind.Decimal),
                    C("Currency", new[] { "HKD", "CNY", "VND", "USD" }),
                    C("PaymentStatus", new[] { "Unpaid", "Partially Paid", "Paid", "Overdue" }),
                    D("InvoiceDate"),
                    D("DueDate")
                }
            ),

            ["Payments"] = new(
                "Payment Records",
                "Payments",
                "Payments linked to invoices",
                new()
                {
                    F("PaymentNo", true, Code("Payments", "PaymentNo", "PAY", "-", 4), true),
                    L("InvoiceId", "Invoice", "Invoices"),
                    N("Amount", FieldKind.Decimal),
                    C("PaymentMethod", new[] { "FPS", "Bank Transfer", "Credit Card", "Cash", "Cheque" }),
                    D("PaymentDate"),
                    F("ReferenceNo", false)
                }
            )
        };
    }

    static FieldDefinition F(string col, bool req = true, object? def = null, bool ro = false) =>
        new()
        {
            Column = col,
            Label = Split(col),
            Kind = FieldKind.Text,
            Required = req,
            DefaultValue = def,
            ReadOnly = ro
        };

    static FieldDefinition P(string col, bool req = true) =>
        new()
        {
            Column = col,
            Label = Split(col),
            Kind = FieldKind.Password,
            Required = req
        };

    static FieldDefinition C(string col, string[] opts, object? def = null) =>
        new()
        {
            Column = col,
            Label = Split(col),
            Kind = FieldKind.Combo,
            Options = opts,
            DefaultValue = def ?? opts[0]
        };

    static FieldDefinition N(string col, FieldKind kind = FieldKind.Integer) =>
        new()
        {
            Column = col,
            Label = Split(col),
            Kind = kind,
            DefaultValue = 0
        };

    static FieldDefinition D(string col) =>
        new()
        {
            Column = col,
            Label = Split(col),
            Kind = FieldKind.Date,
            DefaultValue = DateTime.Today
        };

    static FieldDefinition L(string col, string label, string table) =>
        new()
        {
            Column = col,
            Label = label,
            Kind = FieldKind.Lookup,
            LookupTable = table
        };

    static AutoCode Code(string table, string col, string prefix, string sep, int width) =>
        new(table, col, prefix, sep, width);

    static string Split(string text) =>
        Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
}

public class NaturalSidebarMenuPanel : FlowLayoutPanel
{
    readonly int normalTopPadding = 14;
    int scrollOffset = 0;

    public NaturalSidebarMenuPanel()
    {
        Dock = DockStyle.Fill;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoScroll = false;
        BackColor = Theme.Side;
        Padding = new Padding(14, normalTopPadding, 10, 10);
        DoubleBuffered = true;
        TabStop = true;
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);

        if (e.Control != null)
            HookMouseWheel(e.Control);

        UpdateScrollLimit();
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        base.OnControlRemoved(e);
        UpdateScrollLimit();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ScrollBy(e.Delta < 0 ? 54 : -54);
    }

    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        UpdateScrollLimit();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        UpdateScrollLimit(false);
    }

    void HookMouseWheel(Control control)
    {
        control.MouseEnter += (_, _) => Focus();
        control.MouseWheel += (_, e) => ScrollBy(e.Delta < 0 ? 54 : -54);

        foreach (Control child in control.Controls)
            HookMouseWheel(child);
    }

    void ScrollBy(int delta)
    {
        int max = GetMaxScrollOffset();
        int next = Math.Max(0, Math.Min(max, scrollOffset + delta));

        if (next == scrollOffset)
            return;

        scrollOffset = next;
        ApplyScrollOffset();
    }

    void UpdateScrollLimit(bool apply = true)
    {
        int max = GetMaxScrollOffset();

        if (scrollOffset > max)
            scrollOffset = max;

        if (apply)
            ApplyScrollOffset();
    }

    int GetMaxScrollOffset()
    {
        if (Controls.Count == 0)
            return 0;

        int bottom = Controls.Cast<Control>()
            .Where(c => c.Visible)
            .Select(c => c.Bottom + c.Margin.Bottom)
            .DefaultIfEmpty(0)
            .Max();

        int visibleHeight = Math.Max(1, ClientSize.Height - 10);

        return Math.Max(0, bottom - visibleHeight + normalTopPadding);
    }

    void ApplyScrollOffset()
    {
        Padding = new Padding(14, normalTopPadding - scrollOffset, 10, 10);
        PerformLayout();
        Invalidate();
    }
}