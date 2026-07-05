using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;

public class LoginForm : Form
{
    readonly TextBox user = new();
    readonly TextBox pass = new();
    readonly Label roleValue = new();
    readonly Button eye = new();
    readonly System.Windows.Forms.Timer roleLookupTimer = new() { Interval = 350 };
    bool passwordVisible = false;

    public LoginForm()
    {
        Text = "Premium Living Furniture ERP - Sign In";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(980, 600);
        BackColor = Theme.AppBg;
        Font = Theme.DefaultFont;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(38), BackColor = Theme.AppBg };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));

        var hero = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Primary, Padding = new Padding(30) };
        hero.Controls.Add(new Label { Text = "Premium Living" + Environment.NewLine + "Furniture ERP", Dock = DockStyle.Top, Height = 145, ForeColor = Color.White, Font = new Font("Segoe UI", 26, FontStyle.Bold), BackColor = Theme.Primary });
        hero.Controls.Add(new Label { Text = "Sales | Purchasing | Inventory | Delivery | Finance", Dock = DockStyle.Top, Height = 70, ForeColor = Color.FromArgb(219, 234, 254), Font = new Font("Segoe UI", 12), BackColor = Theme.Primary });

        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(40), Margin = new Padding(24, 0, 0, 0) };
        card.Controls.Add(Content());

        root.Controls.Add(hero, 0, 0);
        root.Controls.Add(card, 1, 0);
        Controls.Add(root);
    }

    Control Content()
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, BackColor = Color.White };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

        page.Controls.Add(new Label { Text = "Welcome back", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Theme.Text, BackColor = Color.White }, 0, 0);

        var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = Color.White };

        user.Text = "";
        user.PlaceholderText = "Enter username";
        user.TextChanged += (_, _) => QueueRoleLookup();

        pass.Text = "";
        pass.PlaceholderText = "Enter password";
        pass.UseSystemPasswordChar = true;

        roleValue.Dock = DockStyle.Fill;
        roleValue.Height = 36;
        roleValue.TextAlign = ContentAlignment.MiddleLeft;
        roleValue.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        roleValue.BackColor = Theme.PrimarySoft;
        roleValue.ForeColor = Theme.Muted;
        roleValue.Padding = new Padding(10, 0, 10, 0);
        roleValue.Text = "Role: enter username";

        roleLookupTimer.Tick += (_, _) => { roleLookupTimer.Stop(); DetectRole(); };

        Add(form, "Username", user);
        Add(form, "Password", PasswordBox());
        AddRoleDisplay(form);
        page.Controls.Add(form, 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White };
        var login = Theme.PrimaryButton("Login", 125);
        login.Click += (_, _) => Login();
        buttons.Controls.Add(login);
        page.Controls.Add(buttons, 0, 2);

        return page;
    }

    Control PasswordBox()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        pass.Dock = DockStyle.Fill;
        pass.Font = new Font("Segoe UI", 11);

        eye.Text = "👁";
        eye.Dock = DockStyle.Right;
        eye.Width = 46;
        eye.FlatStyle = FlatStyle.Flat;
        eye.BackColor = Color.White;
        eye.ForeColor = Theme.Primary;
        eye.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        eye.Cursor = Cursors.Hand;
        eye.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        eye.Click += (_, _) => TogglePasswordVisibility();

        panel.Controls.Add(pass);
        panel.Controls.Add(eye);
        return panel;
    }

    void TogglePasswordVisibility()
    {
        passwordVisible = !passwordVisible;
        pass.UseSystemPasswordChar = !passwordVisible;
        eye.Text = passwordVisible ? "🙈" : "👁";
        pass.Focus();
        pass.SelectionStart = pass.TextLength;
    }

    void QueueRoleLookup()
    {
        roleValue.Text = string.IsNullOrWhiteSpace(user.Text) ? "Role: enter username" : "Role: Detecting...";
        roleValue.ForeColor = Theme.Muted;
        roleLookupTimer.Stop();
        roleLookupTimer.Start();
    }

    void DetectRole()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Text))
            {
                roleValue.Text = "Role: enter username";
                roleValue.ForeColor = Theme.Muted;
                return;
            }

            string role = AuthService.GetRoleForUsername(user.Text);
            roleValue.Text = string.IsNullOrWhiteSpace(role) ? "Role: Unknown / inactive account" : "Role: " + role;
            roleValue.ForeColor = string.IsNullOrWhiteSpace(role) ? Color.FromArgb(220, 38, 38) : Theme.Primary;
        }
        catch
        {
            roleValue.Text = "Role: Unable to detect role";
            roleValue.ForeColor = Color.FromArgb(220, 38, 38);
        }
    }

    static void Add(TableLayoutPanel f, string label, Control c)
    {
        f.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        f.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        f.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.White });
        c.Dock = DockStyle.Fill;
        c.Margin = new Padding(0, 4, 0, 12);
        f.Controls.Add(c);
    }

    void AddRoleDisplay(TableLayoutPanel f)
    {
        f.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        f.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        f.Controls.Add(new Label { Text = "Role", Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.White });
        roleValue.Margin = new Padding(0, 4, 0, 12);
        f.Controls.Add(roleValue);
    }


    void Login()
    {
        try
        {
            if (!AuthService.TryLogin(user.Text, pass.Text, out var role))
            {
                DetectRole();
                MessageBox.Show("Login failed. Please check username, password and account status.");
                return;
            }

            roleValue.Text = "Role: " + role;
            roleValue.ForeColor = Theme.Primary;

            Hide();
            var main = new MainForm(user.Text.Trim(), role);
            main.FormClosed += (_, _) => Close();
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Login/database failed: " + ex.Message);
        }
    }
}
